using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public interface IServerSideOps
{
    PlayerId AddPlayer();

    void PlayerMoved(PlayerId id, Vector3Int newCoords);

    void RemovePlayer(PlayerId id);

    WorldChunkUpdate GetNextChunkUpdate(PlayerId id);

    void PlayerTakeKlotz(PlayerId id, Vector3Int chunkCoords, Vector3Int innerChunkCoords);
}

public class GameServer : MonoBehaviour, IServerSideOps
{
    private static int _nextPlayerId = 1;
    private readonly WorldMap _worldMap = new();
    private readonly ConcurrentDictionary<PlayerId, ConnectionData> _playerData = new();

    private WebSocketServer _wss;

    public int ServerPort = 3000;

    public bool ShowPreview = false;

    // Start is called before the first frame update
    void Start()
    {
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

        _worldMap.StartGeneratorThreads();
    }

    void OnDestroy()
    {
        if (_wss != null)
        {
            _wss.Stop();
            Debug.LogFormat("Server stopped");
        }

        _worldMap.StopMainThreads();
    }

    void OnDrawGizmos()
    {
        if (ShowPreview)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawMesh(_worldMap.GeneratePreviewMesh(128));
        }
    }

    PlayerId IServerSideOps.AddPlayer()
    {
        int playerIdValue = 0;
        Interlocked.Increment(ref _nextPlayerId);

        PlayerId playerId = new(playerIdValue++);
        ConnectionData data = new() { };

        if (!_playerData.TryAdd(playerId, data))
        {
            throw new Exception("Failed to add player.");
        }

        _worldMap.AddPlayer(playerId);
        return playerId;
    }

    /// <summary>
    /// Called by Thread Pool Worker
    /// </summary>
    void IServerSideOps.PlayerMoved(PlayerId id, Vector3Int newCoords)
    {
        _worldMap.PlayerMoved(id, newCoords);
    }

    void IServerSideOps.RemovePlayer(PlayerId id)
    {
        _worldMap.RemovePlayer(id);
        _playerData.TryRemove(id, out _);
    }

    /// <summary>
    /// Called by ClientUpdaterThread
    /// </summary>
    WorldChunkUpdate IServerSideOps.GetNextChunkUpdate(PlayerId id)
    {
        return _worldMap.GetNextChunkUpdate(id);
    }

    /// <summary>
    /// Called by Thread Pool Worker
    /// </summary>
    void IServerSideOps.PlayerTakeKlotz(PlayerId id, Vector3Int chunkCoords, Vector3Int innerChunkCoords)
    {
        _worldMap.PlayerTakeKlotz(id, chunkCoords, innerChunkCoords);
    }

    private class ClientHandler : WebSocketBehavior
    {
        public IServerSideOps ops;

        private volatile bool _isClosed = false;
        private bool _initialCoordsReceived = false;
        private PlayerId _playerId;
        private Thread _clientUpdaterThread;

        protected override void OnOpen()
        {
            _playerId = ops.AddPlayer();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _isClosed = true;
            ops.RemovePlayer(_playerId);

            if (_clientUpdaterThread != null)
            {
                if (!_clientUpdaterThread.Join(TimeSpan.FromSeconds(1)))
                    _clientUpdaterThread.Abort();
            }
        }

        private void OnInitialCoordsReceived()
        {
            _clientUpdaterThread = new Thread(ClientUpdaterThreadMain) { Name = $"ClientUpdaterThreadMain ID={_playerId}" };
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
            if (!_initialCoordsReceived && cmd is not IntercomProtocol.PlayerPosUpdateCommand)
                throw new Exception("First command must be player pos update command");

            if (cmd is IntercomProtocol.PlayerPosUpdateCommand)
            {
                var posCmd = cmd as IntercomProtocol.PlayerPosUpdateCommand;

                if (!_initialCoordsReceived)
                {
                    OnInitialCoordsReceived();
                    _initialCoordsReceived = true;
                }

                ops.PlayerMoved(_playerId, posCmd.Coord);
            }
            else if (cmd is IntercomProtocol.TakeKlotzCommand)
            {
                var takeCmd = cmd as IntercomProtocol.TakeKlotzCommand;
                ops.PlayerTakeKlotz(_playerId,
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
            PlayerId id = _playerId;

            try
            {
                while (!_isClosed)
                {
                    WorldChunkUpdate update = ops.GetNextChunkUpdate(id);

                    if (update == null)
                    {
                        // Waste some time
                        Thread.Sleep(10);
                    }
                    else
                    {
                        var resp = new IntercomProtocol.ChunkDataCommand(update.Coords, update.Version, update.Chunk);
                        Send(resp.ToBytes());
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
