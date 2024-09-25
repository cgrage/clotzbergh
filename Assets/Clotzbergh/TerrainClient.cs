using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

public class TerrainClient : MonoBehaviour
{
    public string Hostname = "localhost";
    public int Port = 3000;

    private Thread _connectionThread;
    private Thread _meshBuiderThread;

    private volatile bool _requestToStop = false;

    public float MoveThreshold = 25;

    public LevelOfDetailSetting[] DetailLevels =
    {
         new() { SetLevelOfDetail = 0, UsedBelowThisThreshold = 2, },
         new() { SetLevelOfDetail = 1, UsedBelowThisThreshold = 4, },
         new() { SetLevelOfDetail = 2, UsedBelowThisThreshold = 6, },
         new() { SetLevelOfDetail = 3, UsedBelowThisThreshold = 10, },
    };

    public Transform Viewer;
    private Vector3 _viewerPos = Vector3.positiveInfinity;
    public Material MapMaterial;

    private readonly TerrainChunkStore _terrainChunkStore = new();

    /// <summary>
    /// Called by Unity
    /// </summary>
    void Start()
    {
        _terrainChunkStore.ParentObject = transform;

        _connectionThread = new Thread(ConnectionThreadMain);
        _connectionThread.Start();

        _meshBuiderThread = new Thread(MeshBuiderThreadMain);
        _meshBuiderThread.Start();
    }

    void Update()
    {
        const float scale = 1f;
        var newViewerPos = Viewer.position / scale;

        if ((_viewerPos - newViewerPos).sqrMagnitude > MoveThreshold * MoveThreshold)
        {
            _viewerPos = newViewerPos;
            UpdateView();
        }
    }

    /// <summary>
    /// Started on the connection thread
    /// </summary>
    void ConnectionThreadMain()
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
                    RunConnection(ws);
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

    void RunConnection(WebSocket ws)
    {
        TerrainProto.GetChunkCommand cmd = new(new Vector3Int(0, 0, 0));
        ws.Send(cmd.ToBytes());

        while (!_requestToStop)
        {
            Thread.Sleep(500);
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
    }

    /// <summary>
    /// Called by Unity
    /// </summary>
    void OnDestroy()
    {
        _requestToStop = true;

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

    void UpdateView()
    {
        int currentChunkCoordX = (int)(_viewerPos.x / WorldChunk.Size.x);
        int currentChunkCoordY = (int)(_viewerPos.y / WorldChunk.Size.y);
        int currentChunkCoordZ = (int)(_viewerPos.z / WorldChunk.Size.z);

        int chunksVisibleInViewDistX = Mathf.RoundToInt(MaxViewDist / WorldChunk.Size.x);
        int chunksVisibleInViewDistY = Mathf.RoundToInt(MaxViewDist / WorldChunk.Size.y);
        int chunksVisibleInViewDistZ = Mathf.RoundToInt(MaxViewDist / WorldChunk.Size.z);

        for (int zOffset = -chunksVisibleInViewDistZ; zOffset <= chunksVisibleInViewDistY; zOffset++)
        {
            for (int yOffset = -chunksVisibleInViewDistY; yOffset <= chunksVisibleInViewDistY; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDistX; xOffset <= chunksVisibleInViewDistX; xOffset++)
                {
                    Vector3Int viewedChunkCoord = new(
                        currentChunkCoordX + xOffset,
                        currentChunkCoordY + yOffset,
                        currentChunkCoordZ + zOffset);

                    _terrainChunkStore.ConsiderLoading(viewedChunkCoord, _viewerPos);
                }
            }
        }
    }

    private float MaxViewDist
    {
        get { return DetailLevels.Last().UsedBelowThisThreshold; }
    }

    [Serializable]
    public struct LevelOfDetailSetting
    {
        public int SetLevelOfDetail;
        public float UsedBelowThisThreshold;
    }
}
