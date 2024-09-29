using System;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class WorldServer : MonoBehaviour
{
    private readonly WorldGenerator _generator = new();

    private WebSocketServer _wss;

    public int ServerPort = 3000;

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
        Gizmos.color = Color.green;
        Gizmos.DrawMesh(_generator.GeneratePreviewMesh(128));
    }

    public class Terrain : WebSocketBehavior
    {
        public WorldGenerator Gen { get; set; }

        protected override void OnMessage(MessageEventArgs e)
        {
            var cmd = TerrainProto.Command.FromBytes(e.RawData);
            Debug.LogFormat("Server received command '{0}'", cmd.Code);

            if (cmd is TerrainProto.GetChunkCommand)
            {
                var getch = cmd as TerrainProto.GetChunkCommand;
                var chunk = Gen.GetChunk(getch.Coord);
                var resp = new TerrainProto.ChunkDataCommand(getch.Coord, chunk);
                Send(resp.ToBytes());
            }
        }
    }
}
