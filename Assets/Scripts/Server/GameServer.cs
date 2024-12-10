using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Clotzbergh.Server
{
    public interface IServerSideOps
    {
        ClientId AddClient();

        void PlayerMoved(ClientId id, Vector3 newPosition);

        void RemoveClient(ClientId id);

        ServerStatusUpdate GetServerStatus(ClientId id);

        WorldChunkUpdate GetNextChunkUpdate(ClientId id);

        void PlayerTakeKlotz(ClientId id, Vector3Int chunkCoords, Vector3Int innerChunkCoords);
    }

    public class GameServer : MonoBehaviour, IServerSideOps
    {
        private static int _idCounter = 0;

        private WorldMap _worldMap;
        private ConcurrentDictionary<ClientId, ConnectionData> _clientData;

        private WebSocketServer _wss;

        public int ServerPort = 3000;

        public bool ShowPreview = false;

        // Start is called before the first frame update
        void Start()
        {
            _worldMap = new(0);
            _clientData = new();

            string url = string.Format("ws://localhost:{0}", ServerPort);

            try
            {
                _wss = new WebSocketServer(url);
                _wss.AddWebSocketService<ClientHandler>("/intercom", (handler) => { handler.ops = this; });
                _wss.Start();
                Debug.LogFormat("Server started at {0}", url);
            }
            catch (Exception ex)
            {
                _wss = null;
                Debug.LogException(ex);
                Debug.LogFormat("Server start at {0} failed with exception (see above)", url);
            }

            _worldMap.StartThreads();
        }

        void OnDestroy()
        {
            _wss?.Stop();
            _worldMap?.StopThreads();

            Debug.LogFormat("Server stopped");
        }

        void OnDrawGizmos()
        {
            if (ShowPreview)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawMesh(_worldMap.GeneratePreviewMesh(128));
            }
        }

        ClientId IServerSideOps.AddClient()
        {
            ClientId clientId = new(Interlocked.Increment(ref _idCounter));
            ConnectionData data = new() { };

            if (!_clientData.TryAdd(clientId, data))
            {
                throw new Exception("Failed to add client.");
            }

            _worldMap.AddClient(clientId);
            return clientId;
        }

        /// <summary>
        /// Called by Thread Pool Worker
        /// </summary>
        void IServerSideOps.PlayerMoved(ClientId id, Vector3 newPosition)
        {
            _worldMap.PlayerMoved(id, newPosition);
        }

        void IServerSideOps.RemoveClient(ClientId id)
        {
            _worldMap.RemoveClient(id);
            _clientData.TryRemove(id, out _);
        }

        /// <summary>
        /// Called by ClientUpdaterThread
        /// </summary>
        /// <returns></returns>
        ServerStatusUpdate IServerSideOps.GetServerStatus(ClientId id)
        {
            return _worldMap.GetNextServerStatus(id);
        }

        /// <summary>
        /// Called by ClientUpdaterThread
        /// </summary>
        WorldChunkUpdate IServerSideOps.GetNextChunkUpdate(ClientId id)
        {
            return _worldMap.GetNextChunkUpdate(id);
        }

        /// <summary>
        /// Called by Thread Pool Worker
        /// </summary>
        void IServerSideOps.PlayerTakeKlotz(ClientId id, Vector3Int chunkCoords, Vector3Int innerChunkCoords)
        {
            _worldMap.PlayerTakeKlotz(id, chunkCoords, innerChunkCoords);
        }

        private class ClientHandler : WebSocketBehavior
        {
            public IServerSideOps ops;

            private volatile bool _isClosed = false;
            private bool _initialStatusReceived = false;
            private ClientId _clientId;
            private Thread _clientUpdaterThread;

            protected override void OnOpen()
            {
                _clientId = ops.AddClient();
            }

            protected override void OnClose(CloseEventArgs e)
            {
                _isClosed = true;
                ops.RemoveClient(_clientId);

                if (_clientUpdaterThread != null)
                {
                    if (!_clientUpdaterThread.Join(TimeSpan.FromSeconds(1)))
                        _clientUpdaterThread.Abort();
                }
            }

            private void OnInitialCoordsReceived()
            {
                _clientUpdaterThread = new Thread(ClientUpdaterThreadMain) { Name = $"ClientUpdaterThreadMain ID={_clientId}" };
                _clientUpdaterThread.Start();
            }

            /// <summary>
            /// Called by Thread Pool Worker
            /// </summary>
            protected override void OnMessage(MessageEventArgs e)
            {
                try
                {
                    var cmd = IntercomProtocol.Command.FromBytes(e.RawData);
                    // Debug.LogFormat("Server received command '{0}'", cmd.Code);

                    HandleCommand(cmd);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            /// <summary>
            /// Called by Thread Pool Worker
            /// </summary>
            private void HandleCommand(IntercomProtocol.Command cmd)
            {
                if (!_initialStatusReceived && cmd is not IntercomProtocol.ClientStatusCommand)
                    throw new Exception("First command must be client status command");

                if (cmd is IntercomProtocol.ClientStatusCommand)
                {
                    var posCmd = cmd as IntercomProtocol.ClientStatusCommand;

                    if (!_initialStatusReceived)
                    {
                        OnInitialCoordsReceived();
                        _initialStatusReceived = true;
                    }

                    ops.PlayerMoved(_clientId, posCmd.Position);
                }
                else if (cmd is IntercomProtocol.TakeKlotzCommand)
                {
                    var takeCmd = cmd as IntercomProtocol.TakeKlotzCommand;
                    ops.PlayerTakeKlotz(_clientId,
                        takeCmd.ChunkCoord, takeCmd.InnerChunkCoord);
                }
                else
                {
                    // ?
                }
            }

            /// <summary>
            /// Main thread function of the ClientUpdaterThread
            /// </summary>
            private void ClientUpdaterThreadMain()
            {
                ClientId id = _clientId;
                int timeOfLastStatus = Environment.TickCount;

                try
                {
                    while (!_isClosed)
                    {
                        int currentTime = Environment.TickCount;
                        int elapsedMilliseconds = currentTime - timeOfLastStatus;

                        if (elapsedMilliseconds >= 100)
                        {
                            ServerStatusUpdate statusUpdate = ops.GetServerStatus(id);
                            Send(new IntercomProtocol.ServerStatusCommand(statusUpdate).ToBytes());

                            // Reset the start time
                            timeOfLastStatus = currentTime;
                        }

                        WorldChunkUpdate chunkUpdate = ops.GetNextChunkUpdate(id);
                        if (chunkUpdate == null)
                        {
                            // Waste some time
                            Thread.Sleep(10);
                        }
                        else
                        {
                            Send(new IntercomProtocol.ChunkDataCommand(chunkUpdate.Coords,
                                chunkUpdate.Version, chunkUpdate.Chunk).ToBytes());
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_isClosed || ex is ThreadAbortException)
                    {
                        Debug.LogFormat($"ClientUpdaterThread stopped with exception ({ex.GetType().Name}).");
                    }
                    else
                    {
                        Debug.LogException(ex);
                        Debug.LogFormat("ClientUpdaterThread stopped on exception (see above).");
                    }
                }
            }
        }
    }
}
