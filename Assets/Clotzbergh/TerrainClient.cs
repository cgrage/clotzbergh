using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

public class TerrainClient : MonoBehaviour
{
    public string Hostname = "localhost";
    public int Port = 3000;

    private Thread _thread;
    private volatile bool _requestToStop = false;

    /// <summary>
    /// Called by Unity
    /// </summary>
    void Start()
    {
        // Create a new thread and start it
        _thread = new Thread(ThreadMain);
        _thread.Start();
    }

    /// <summary>
    /// Started on the connection thread
    /// </summary>
    void ThreadMain()
    {
        string url = string.Format("ws://{0}:{1}/terrain", Hostname, Port);
        using (var ws = new WebSocket(url))
        {
            ws.OnMessage += OnDataReceived;

            while (!_requestToStop)
            {
                try
                {
                    Debug.LogFormat("Connect to {0}", url);
                    ws.Connect();
                    Debug.LogFormat("Connected");
                    RunConnection(ws, url);
                    Debug.LogFormat("Connection Closed");
                }
                catch (IOException ex)
                {
                    Debug.LogException(ex);
                    // no break here
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    break;
                }
            }
        }
    }

    void RunConnection(WebSocket ws, string url)
    {
        var command = TerrainProto.BuildGetChunkCommand(new Vector3Int(0, 0, 0));
        ws.Send(command);

        while (!_requestToStop)
        {
            Thread.Sleep(100);
        }
    }

    void OnDataReceived(object sender, MessageEventArgs e)
    {
        Debug.LogFormat("Received: {0}", e.Data);
    }

    /// <summary>
    /// Called by Unity
    /// </summary>
    void OnDestroy()
    {
        _requestToStop = true;

        if (_thread != null && _thread.IsAlive)
        {
            if (!_thread.Join(TimeSpan.FromSeconds(3)))
            {
                Debug.LogFormat("Failed to join WS thread");
                _thread.Abort();
            }
        }
    }
}
