using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    private readonly CancellationTokenSource _runCancelTS = new();
    private readonly List<Thread> _threads = new();
    private readonly BlockingCollection<Action<IServerSideOps>> _requestQueue = new();
    private readonly WorldMap _worldMap = new();
    private readonly ConcurrentDictionary<PlayerId, ConnectionData> _playerData = new();

    private WebSocketServer _wss;

    public int ServerPort = 3000;

    public bool ShowPreview = false;

    public int MainThreadCount = 1;

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

        StartMainThreads();
    }

    void OnDestroy()
    {
        if (_wss != null)
        {
            _wss.Stop();
            Debug.LogFormat("Server stopped");
        }

        StopMainThreads();
    }

    void OnDrawGizmos()
    {
        if (ShowPreview)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawMesh(_worldMap.GeneratePreviewMesh(128));
        }
    }

    private void StartMainThreads()
    {
        Debug.LogFormat("Server starting threads...");

        for (int i = 0; i < MainThreadCount; i++)
        {
            var thread = new Thread(TerrainThreadMain) { Name = $"ServerMainThread{i}" };
            _threads.Add(thread);
            thread.Start();
        }

        Debug.LogFormat("Server thread started");
    }

    private void StopMainThreads()
    {
        Debug.LogFormat("Server threads closing...");
        _runCancelTS.Cancel();

        foreach (var thread in _threads)
        {
            if (!thread.Join(TimeSpan.FromSeconds(1)))
                thread.Abort();
        }

        Debug.LogFormat("Server thread stopped");
    }

    void TerrainThreadMain()
    {
        try
        {
            while (!_runCancelTS.Token.IsCancellationRequested)
            {
                Action<IServerSideOps> action = _requestQueue.Take(_runCancelTS.Token);
                action(this);
            }
        }
        catch (OperationCanceledException) { /* see also: Expection anti-pattern */ }
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

    void IServerSideOps.PlayerMoved(PlayerId id, Vector3Int newCoords)
    {
        _worldMap.PlayerMoved(id, newCoords);
    }

    void IServerSideOps.RemovePlayer(PlayerId id)
    {
        _worldMap.RemovePlayer(id);
        _playerData.TryRemove(id, out _);
    }

    WorldChunkUpdate IServerSideOps.GetNextChunkUpdate(PlayerId id)
    {
        return _worldMap.GetNextChunkUpdate(id);
    }

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
            catch (InvalidOperationException) { /* this is expected */ }
        }
    }
}
