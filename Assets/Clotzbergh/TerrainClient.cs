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

    private Thread newThread;
    private volatile bool _requestToStop = false;

    void Start()
    {
        // Create a new thread and start it
        newThread = new Thread(DoWork);
        newThread.Start();
    }

    void DoWork()
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
        string request = string.Format("getch {0},{1},{2}", 0, 0, 0);
        ws.Send(request);

        while (!_requestToStop)
        {
            Thread.Sleep(100);
        }
    }

    void OnDataReceived(object sender, MessageEventArgs e)
    {
        Debug.LogFormat("Received: {0}", e.Data);
    }

    void OnDestroy()
    {
        _requestToStop = true;

        if (newThread != null && newThread.IsAlive)
        {
            if (!newThread.Join(TimeSpan.FromSeconds(3)))
            {
                Debug.LogFormat("Failed to join WS thread");
                newThread.Abort();
            }
        }
    }
}
