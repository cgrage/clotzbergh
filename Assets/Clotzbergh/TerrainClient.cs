using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

public interface ITerrainDataRequester
{
    void RequestWorldData(Vector3Int coords);
}

public class TerrainClient : MonoBehaviour, ITerrainDataRequester
{
    public string Hostname = "localhost";
    public int Port = 3000;

    private Thread _connectionThread;
    private Thread _meshBuiderThread;

    private volatile bool _requestToStop = false;

    public float MoveThreshold = 25;

    public Transform Viewer;
    private Vector3 _viewerPos = Vector3.positiveInfinity;
    public Material MapMaterial;

    private readonly TerrainChunkStore _terrainChunkStore = new();

    private readonly BlockingCollection<Action<WebSocket>> _requestQueue = new();

    /// <summary>
    /// Called by Unity
    /// </summary>
    void Start()
    {
        _terrainChunkStore.ParentObject = transform;
        _terrainChunkStore.TerrainDataRequester = this;

        _connectionThread = new Thread(ConnectionThreadMain) { Name = "ConnectionThread" };
        _connectionThread.Start();

        _meshBuiderThread = new Thread(MeshBuiderThreadMain) { Name = "MeshBuiderThread" };
        _meshBuiderThread.Start();
    }

    void Update()
    {
        const float scale = 1f;
        var newViewerPos = Viewer.position / scale;

        if ((_viewerPos - newViewerPos).sqrMagnitude > MoveThreshold * MoveThreshold)
        {
            _viewerPos = newViewerPos;
            _terrainChunkStore.OnViewerMoved(_viewerPos);
        }
    }

    /// <summary>
    /// Started on the connection thread
    /// </summary>
    void ConnectionThreadMain()
    {
        string url = string.Format("ws://{0}:{1}/terrain", Hostname, Port);

        using var ws = new WebSocket(url);
        ws.OnMessage += OnDataReceived;

        try
        {
            Debug.LogFormat("Connect to {0}", url);
            ws.Connect();
            Debug.LogFormat("Connected");

            while (true)
            {
                var action = _requestQueue.Take();
                action(ws);
            }
        }
        catch (InvalidOperationException)
        {
            Debug.LogFormat("ConnectionThread stopped (InvalidOperationException).");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogFormat("ConnectionThread stopped on exception (see above).");
        }
    }

    void MeshBuiderThreadMain()
    {
        while (!_requestToStop)
        {
            Thread.Sleep(100);
        }
    }

    void OnDataReceived(object sender, MessageEventArgs e)
    {
        var cmd = TerrainProto.Command.FromBytes(e.RawData);
        Debug.LogFormat("Client received command '{0}'", cmd.Code);

        switch (cmd.Code)
        {
            case TerrainProto.Command.CodeValue.ChuckData:
                var realCmd = (TerrainProto.ChunkDataCommand)cmd;
                _terrainChunkStore.OnWorldChunkReceived(realCmd.Coord, realCmd.Chunk, _viewerPos);
                break;
        }
    }

    /// <summary>
    /// Called by Unity
    /// </summary>
    void OnDestroy()
    {
        _requestToStop = true;
        _requestQueue.CompleteAdding();

        if (_connectionThread != null && _connectionThread.IsAlive)
        {
            if (!_connectionThread.Join(TimeSpan.FromSeconds(3)))
            {
                Debug.LogFormat("Failed to join conn thread");
                _connectionThread.Abort();
            }
        }

        if (_meshBuiderThread != null && _meshBuiderThread.IsAlive)
        {
            if (!_meshBuiderThread.Join(TimeSpan.FromSeconds(3)))
            {
                Debug.LogFormat("Failed to join mesh thread");
                _meshBuiderThread.Abort();
            }
        }
    }

    void ITerrainDataRequester.RequestWorldData(Vector3Int coords)
    {
        _requestQueue.Add((ws) =>
        {
            TerrainProto.GetChunkCommand cmd = new(coords);
            ws.Send(cmd.ToBytes());
        });
    }
}
