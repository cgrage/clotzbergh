using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public interface IServerSideOps
{
    WorldChunk GetChunk(Vector3Int coords);
}

public class RequestQueue : BlockingCollection<Action<IServerSideOps>> { }

public class GameServer : MonoBehaviour, IServerSideOps
{
    private readonly WorldGenerator _generator = new();

    private WebSocketServer _wss;

    public int ServerPort = 3000;

    public bool ShowPreview = false;

    public int ThreadCount = 4;

    private readonly CancellationTokenSource _runCancelTS = new();
    private readonly List<Thread> _threads = new();
    private readonly RequestQueue _requestQueue = new();

    // Start is called before the first frame update
    void Start()
    {
        string url = string.Format("ws://localhost:{0}", ServerPort);

        try
        {
            _wss = new WebSocketServer(url);
            _wss.AddWebSocketService<ClientHandler>("/intercom", (handler) => { handler.RQ = _requestQueue; });
            _wss.Start();
            Debug.LogFormat("Server started at {0}", url);
        }
        catch (Exception ex)
        {
            _wss = null;
            Debug.LogException(ex);
            Debug.LogFormat("Server start at {0} failed with exception (see above)", url);
        }

        StartThreads();
    }

    void OnDestroy()
    {
        if (_wss != null)
        {
            _wss.Stop();
            Debug.LogFormat("Server stopped");
        }

        StopThreads();
    }

    void OnDrawGizmos()
    {
        if (ShowPreview)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawMesh(_generator.GeneratePreviewMesh(128));
        }
    }

    private void StartThreads()
    {
        Debug.LogFormat("Server starting threads...");

        for (int i = 0; i < ThreadCount; i++)
        {
            var thread = new Thread(TerrainThreadMain) { Name = $"TerrainThread{i}" };
            _threads.Add(thread);
            thread.Start();
        }

        Debug.LogFormat("Server thread started");
    }

    private void StopThreads()
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

    WorldChunk IServerSideOps.GetChunk(Vector3Int coords)
    {
        return _generator.GetChunk(coords);
    }

    private class ClientHandler : WebSocketBehavior
    {
        public RequestQueue RQ;

        protected override void OnOpen()
        {
            // 
        }

        protected override void OnClose(CloseEventArgs e)
        {
            // 
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            RQ.Add((ops) =>
            {
                var cmd = IntercomProtocol.Command.FromBytes(e.RawData);
                // Debug.LogFormat("Server received command '{0}'", cmd.Code);

                if (cmd is IntercomProtocol.PlayerPosUpdateCommand)
                {
                    var posCmd = cmd as IntercomProtocol.PlayerPosUpdateCommand;
                    var chunk = ops.GetChunk(posCmd.Coord);
                    var resp = new IntercomProtocol.ChunkDataCommand(posCmd.Coord, chunk);
                    Send(resp.ToBytes());
                }
            });
        }
    }
}
