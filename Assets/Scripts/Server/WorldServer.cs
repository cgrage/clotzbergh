using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class WorldServer : MonoBehaviour
{
    private readonly WorldGenerator _generator = new();

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
            _wss.AddWebSocketService<Terrain>("/terrain", (terrain) => { terrain.Gen = _generator; });
            _wss.Start();
            Debug.LogFormat("Server started at {0}", url);
        }
        catch (Exception ex)
        {
            _wss = null;
            Debug.LogException(ex);
            Debug.LogFormat("Server start at {0} failed with exception (see above)", url);
        }
    }

    void OnDestroy()
    {
        if (_wss != null)
        {
            _wss.Stop();
            Debug.LogFormat("Server stopped");
        }
    }

    void OnDrawGizmos()
    {
        if (ShowPreview)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawMesh(_generator.GeneratePreviewMesh(128));
        }
    }

    public class Terrain : WebSocketBehavior
    {
        private readonly BlockingCollection<Action> _requestQueue = new();

        private readonly CancellationTokenSource _runCancelTS = new();

        private readonly List<Thread> _terrainThreads = new();

        private const int ThreadCount = 4;

        public WorldGenerator Gen { get; set; }

        protected override void OnOpen()
        {
            Debug.LogFormat("Terrain server starting...");

            for (int i = 0; i < ThreadCount; i++)
            {
                var thread = new Thread(TerrainThreadMain) { Name = $"TerrainThread{i}" };
                _terrainThreads.Add(thread);
                thread.Start();
            }

            Debug.LogFormat("Terrain server started");
        }

        void TerrainThreadMain()
        {
            try
            {
                while (!_runCancelTS.Token.IsCancellationRequested)
                {
                    Action action = _requestQueue.Take(_runCancelTS.Token);
                    action();
                }
            }
            catch (OperationCanceledException) { /* see also: Expection anti-pattern */ }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.LogFormat("Terrain server closing...");
            _runCancelTS.Cancel();

            foreach (var thread in _terrainThreads)
            {
                if (!thread.Join(TimeSpan.FromSeconds(1)))
                    thread.Abort();
            }

            Debug.LogFormat("Terrain server closed");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            _requestQueue.Add(() =>
            {
                var cmd = TerrainProto.Command.FromBytes(e.RawData);
                // Debug.LogFormat("Server received command '{0}'", cmd.Code);

                if (cmd is TerrainProto.GetChunkCommand)
                {
                    var getch = cmd as TerrainProto.GetChunkCommand;
                    var chunk = Gen.GetChunk(getch.Coord);
                    var resp = new TerrainProto.ChunkDataCommand(getch.Coord, chunk);
                    Send(resp.ToBytes());
                }
            });
        }
    }
}
