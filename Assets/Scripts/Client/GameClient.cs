using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

public interface IClientSideOps
{
    void RequestMeshCalc(ClientChunk owner, WorldChunk world, int lod, ulong worldLocalVersion);
    void TakeKlotz(Vector3Int chunkCoords, Vector3Int innerChunkCoords);
}

public class GameClient : MonoBehaviour, IClientSideOps
{
    public string Hostname = "localhost";
    public int Port = 3000;
    public float MoveThreshold = 2;
    public int MeshThreadCount = 4;
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
    private readonly ClientChunkStore _chunkStore = new();
    private readonly BlockingCollection<Action<WebSocket>> _connectionThreadActionQueue = new();
    private readonly ConcurrentQueue<Action> _mainThreadActionQueue = new();
    private readonly MeshGenerator _meshGenerator = new();

    private ulong _receivedBytes = 0;
    private ulong _receivedBytesLastSec = 0;
    private ulong _receivedBytesPerSecLastSec;
    private ulong _receivedChunks = 0;
    private ulong _receivedChunksLastSec = 0;
    private ulong _receivedChunksPerSecLastSec;


    /// <summary>
    /// Called by Unity
    /// </summary>
    void Start()
    {
        _chunkStore.ParentObject = transform;
        _chunkStore.AsyncTerrainOps = this;
        _chunkStore.KlotzMat = Material;

        _connectionThread = new Thread(ConnectionThreadMain) { Name = "ConnectionThread" };
        _connectionThread.Start();

        for (int i = 0; i < MeshThreadCount; i++)
        {
            var thread = new Thread(MeshThreadMain) { Name = $"MeshTread{i}" };
            _meshThreads.Add(thread);
            thread.Start();
        }

        StartCoroutine(TimerCoroutine());
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
                _chunkStore.OnViewerMoved(newCoords);
                SendPayerPosUpdate(newCoords);
            }

            // update after "on moved" so that world can be requested on first frame
            _chunkStore.OnUpdate();
        }

        _wasConnected = _isConnected;
    }

    IEnumerator TimerCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);

            _receivedBytesPerSecLastSec = _receivedBytes - _receivedBytesLastSec;
            _receivedChunksPerSecLastSec = _receivedChunks - _receivedChunksLastSec;

            _receivedBytesLastSec = _receivedBytes;
            _receivedChunksLastSec = _receivedChunks;
        }
    }

    void OnGUI()
    {
        GUIStyle style = new() { fontSize = 16 };
        style.normal.textColor = Color.black;

        Vector3Int viewerChunkCoords = WorldChunk.PositionToChunkCoords(Viewer.position);
        GUI.Label(new Rect(5, 5, 500, 200),
            $"Pos: {Viewer.position}\n" +
            $"Coord: {viewerChunkCoords}\n" +
            $"Chk Count: {_chunkStore.ChunkCount}\n" +
            $"Act Count: {_chunkStore.ActiveChunkCount}\n" +
            $"Rec.Chunks (total): {_receivedChunks}\n" +
            $"Rec.Chunks (1/s): {_receivedChunksPerSecLastSec}\n" +
            $"Rec.MByte (total): {_receivedBytes / 1024 / 1024}\n" +
            $"Rec.KByte (1/s): {_receivedBytesPerSecLastSec / 1024}",
            style);
    }

    /// <summary>
    /// Started on the connection thread
    /// </summary>
    void ConnectionThreadMain()
    {
        string url = string.Format("ws://{0}:{1}/intercom", Hostname, Port);
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
                var action = _connectionThreadActionQueue.Take(_runCancelTS.Token);
                action(ws);
            }
        }
        catch (Exception ex)
        {
            _isConnected = false;

            if (_runCancelTS.Token.IsCancellationRequested || ex is ThreadAbortException)
            {
                Debug.LogFormat($"ConnectionThread stopped with exception ({ex.GetType().Name}).");
            }
            else
            {
                Debug.LogException(ex);
                Debug.LogFormat("ConnectionThread stopped on exception (see above).");
            }
        }
    }

    void MeshThreadMain()
    {
        try
        {
            while (!_runCancelTS.Token.IsCancellationRequested)
            {
                MeshRequest request = _meshRequestQueue.Take(_runCancelTS.Token);
                VoxelMeshBuilder meshData = _meshGenerator.GenerateTerrainMesh(request.Owner, request.Lod);

                ToMainThread(() => { request.Owner.OnMeshUpdate(meshData, request.Lod, request.WorldLocalVersion); });
            }
        }
        catch (Exception ex)
        {
            if (_runCancelTS.Token.IsCancellationRequested || ex is ThreadAbortException)
            {
                Debug.LogFormat($"MeshThread stopped with exception ({ex.GetType().Name}).");
            }
            else
            {
                Debug.LogException(ex);
                Debug.LogFormat("MeshThread stopped on exception (see above).");
            }
        }
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
        _receivedBytes += (ulong)e.RawData.Length;
        var cmd = IntercomProtocol.Command.FromBytes(e.RawData);
        // Debug.LogFormat("Client received command '{0}'", cmd.Code);

        switch (cmd.Code)
        {
            case IntercomProtocol.Command.CodeValue.ChuckData:
                var realCmd = (IntercomProtocol.ChunkDataCommand)cmd;
                ToMainThread(() =>
                {
                    _receivedChunks++;
                    _chunkStore.OnWorldChunkReceived(realCmd.Coord, realCmd.Version, realCmd.Chunk);
                });
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

    private void SendPayerPosUpdate(Vector3Int coords)
    {
        _connectionThreadActionQueue.Add((ws) =>
        {
            IntercomProtocol.PlayerPosUpdateCommand cmd = new(coords);
            ws.Send(cmd.ToBytes());
        });
    }

    private readonly struct MeshRequest
    {
        private readonly ClientChunk owner;
        private readonly int lod;
        private readonly ulong worldLocalVersion;

        public MeshRequest(ClientChunk owner, int lod, ulong worldLocalVersion)
        {
            this.owner = owner;
            this.lod = lod;
            this.worldLocalVersion = worldLocalVersion;
        }

        public readonly ClientChunk Owner => owner;
        public readonly int Lod => lod;
        public readonly ulong WorldLocalVersion => worldLocalVersion;
    }

    void IClientSideOps.RequestMeshCalc(ClientChunk owner, WorldChunk world, int lod, ulong worldLocalVersion)
    {
        _meshRequestQueue.Add(new MeshRequest(owner, lod, worldLocalVersion));
    }

    void IClientSideOps.TakeKlotz(Vector3Int chunkCoords, Vector3Int innerChunkCoords)
    {
        _connectionThreadActionQueue.Add((ws) =>
        {
            // Debug.Log($"Client: TakeKlotz {chunkCoords}.{innerChunkCoords}");
            IntercomProtocol.TakeKlotzCommand cmd = new(chunkCoords, innerChunkCoords);
            ws.Send(cmd.ToBytes());
        });
    }
}
