using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class WorldServer : MonoBehaviour
{
    private readonly WorldGenerator _generator = new();
    private readonly WebSocketServer _wss;

    public int ServerPort = 3000;

    public WorldServer()
    {
        var url = string.Format("ws://localhost:{0}", ServerPort);
        _wss = new WebSocketServer(url);
        _wss.AddWebSocketService<Terrain>("/terrain");
        Debug.LogFormat("Server created at {0}", url);
    }

    // Start is called before the first frame update
    void Start()
    {
        _wss.Start();
        Debug.LogFormat("Server started");
    }

    void OnDestroy()
    {
        _wss.Stop();
        Debug.LogFormat("Server stopped");
    }

    public class Terrain : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            Debug.LogFormat("Received from client: {0}", e.Data);
            Send("Echo: " + e.Data);
        }
    }
}
