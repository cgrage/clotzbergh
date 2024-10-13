using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

public interface IAsyncTerrainOps
{
    void RequestWorldData(Vector3Int coords);
    void RequestMeshCalc(TerrainChunk owner, WorldChunk world, int lod, ulong worldVersion);
}

public class TerrainClient : MonoBehaviour, IAsyncTerrainOps
{
    public string Hostname = "localhost";
    public int Port = 3000;
    public float MoveThreshold = 2;
    public Transform Viewer;
    public Material Material;

    private Thread _connectionThread;

    private readonly List<Thread> _meshThreads = new();
    private readonly BlockingCollection<MeshRequest> _meshRequestQueue = new();
    private Vector3 _viewerPosition = Vector3.positiveInfinity;
    private Vector3Int _viewerChunkCoords = Vector3Int.zero;
    private bool _isConnected = false;

    /// <summary>
    /// Used during <c>Update()</c> do decide if we were connected at last <c>Update()</c>
    /// </summary>
    private bool _wasConnected = false;

    private readonly CancellationTokenSource _runCancelTS = new();
    private readonly TerrainChunkStore _terrainChunkStore = new();
    private readonly BlockingCollection<Action<WebSocket>> _worldRequestQueue = new();
    private readonly ConcurrentQueue<Action> _mainThreadActionQueue = new();
    private readonly MeshGenerator _meshGenerator = new();

    private const int MeshThreadCount = 4;

    /// <summary>
    /// Called by Unity
    /// </summary>
    void Start()
    {
        _terrainChunkStore.ParentObject = transform;
        _terrainChunkStore.AsyncTerrainOps = this;
        _terrainChunkStore.KlotzMat = Material;

        _connectionThread = new Thread(ConnectionThreadMain) { Name = "ConnectionThread" };
        _connectionThread.Start();

        for (int i = 0; i < MeshThreadCount; i++)
        {
            var thread = new Thread(MeshThreadMain) { Name = $"MeshTread{i}" };
            _meshThreads.Add(thread);
            thread.Start();
        }
    }

    void Update()
    {
        if (_isConnected && !_wasConnected)
        {
            _mainThreadActionQueue.Clear();
            OnConnected();
        }

        if (!_isConnected && _wasConnected)
        {
            OnDisconnected();
        }

        if (_isConnected)
        {
            while (_mainThreadActionQueue.TryDequeue(out Action action))
            {
                action();
            }

            float diffOfLastPos = Vector3.Distance(Viewer.position, _viewerPosition);
            Vector3Int newCoords = WorldChunk.PositionToChunkCoords(Viewer.position);

            if (newCoords != _viewerChunkCoords /*&& diffOfLastPos > MoveThreshold*/)
            {
                _viewerChunkCoords = newCoords;
                _viewerPosition = Viewer.position;

                Debug.Log($"Viewer moved to chunk ${newCoords}");
                _terrainChunkStore.OnViewerMoved(newCoords);
            }

            // update after "on moved" so that world can be requested on first frame
            _terrainChunkStore.OnUpdate();
        }

        _wasConnected = _isConnected;
    }

    void OnGUI()
    {
        GUIStyle style = new() { fontSize = 16 };
        style.normal.textColor = Color.black;

        Vector3Int viewerChunkCoords = WorldChunk.PositionToChunkCoords(Viewer.position);
        GUI.Label(new Rect(5, 5, 500, 150),
            $"Pos: {Viewer.position}\n" +
            $"Coord: {viewerChunkCoords}\n" +
            $"Chk Count: {_terrainChunkStore.ChunkCount}\n" +
            $"Act Count: {_terrainChunkStore.ActiveChunkCount}",
            style);
    }

    /// <summary>
    /// Started on the connection thread
    /// </summary>
    void ConnectionThreadMain()
    {
        string url = string.Format("ws://{0}:{1}/terrain", Hostname, Port);
        using var ws = new WebSocket(url);
        ws.OnMessage += OnDataReceivedAsync;

        try
        {
            Debug.LogFormat("Connecting to {0}...", url);
            ws.Connect();

            if (ws.ReadyState != WebSocketState.Open)
                throw new Exception("Connect Failed");

            _isConnected = true;

            while (!_runCancelTS.Token.IsCancellationRequested)
            {
                var action = _worldRequestQueue.Take(_runCancelTS.Token);
                action(ws);
            }
        }
        catch (OperationCanceledException)
        {
            _isConnected = false;
            Debug.LogFormat("ConnectionThread stopped (OperationCanceledException).");
        }
        catch (Exception ex)
        {
            _isConnected = false;
            Debug.LogException(ex);
            Debug.LogFormat("ConnectionThread stopped on exception (see above).");
        }
    }

    void MeshThreadMain()
    {
        try
        {
            while (!_runCancelTS.Token.IsCancellationRequested)
            {
                MeshRequest request = _meshRequestQueue.Take(_runCancelTS.Token);
                MeshBuilder meshData = _meshGenerator.GenerateTerrainMesh(request.Owner, request.Lod);

                ToMainThread(() => { request.Owner.OnMeshUpdate(meshData, request.Lod, request.WorldVersion); });
            }
        }
        catch (OperationCanceledException) { /* see also: Expection anti-pattern */ }
    }

    void OnConnected()
    {
        Debug.LogFormat("TerrainClient connected");
    }

    void OnDisconnected()
    {
        Debug.LogFormat("TerrainClient disconnected");
    }

    private void ToMainThread(Action action)
    {
        _mainThreadActionQueue.Enqueue(action);
    }

    void OnDataReceivedAsync(object sender, MessageEventArgs e)
    {
        var cmd = TerrainProto.Command.FromBytes(e.RawData);
        // Debug.LogFormat("Client received command '{0}'", cmd.Code);

        switch (cmd.Code)
        {
            case TerrainProto.Command.CodeValue.ChuckData:
                var realCmd = (TerrainProto.ChunkDataCommand)cmd;
                ToMainThread(() => { _terrainChunkStore.OnWorldChunkReceived(realCmd.Coord, realCmd.Chunk); });
                break;
        }
    }

    /// <summary>
    /// Called by Unity
    /// </summary>
    void OnDestroy()
    {
        Debug.LogFormat("Closing client...");
        _runCancelTS.Cancel();

        if (_connectionThread != null && _connectionThread.IsAlive)
        {
            if (!_connectionThread.Join(TimeSpan.FromSeconds(3)))
                _connectionThread.Abort();
        }

        foreach (var thread in _meshThreads)
        {
            if (!thread.Join(TimeSpan.FromSeconds(1)))
                thread.Abort();
        }

        Debug.LogFormat("Closed client");
    }

    void IAsyncTerrainOps.RequestWorldData(Vector3Int coords)
    {
        _worldRequestQueue.Add((ws) =>
        {
            TerrainProto.GetChunkCommand cmd = new(coords);
            ws.Send(cmd.ToBytes());
        });
    }

    private readonly struct MeshRequest
    {
        private readonly TerrainChunk owner;
        private readonly int lod;
        private readonly ulong worldVersion;

        public MeshRequest(TerrainChunk owner, int lod, ulong worldVersion)
        {
            this.owner = owner;
            this.lod = lod;
            this.worldVersion = worldVersion;
        }

        public readonly TerrainChunk Owner => owner;
        public readonly int Lod => lod;
        public readonly ulong WorldVersion => worldVersion;
    }

    void IAsyncTerrainOps.RequestMeshCalc(TerrainChunk owner, WorldChunk world, int lod, ulong worldVersion)
    {
        _meshRequestQueue.Add(new MeshRequest(owner, lod, worldVersion));
    }
}
