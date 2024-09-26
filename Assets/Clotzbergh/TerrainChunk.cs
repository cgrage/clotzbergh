using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class TerrainChunk
{
    private readonly int _ownerThreadId;
    private readonly GameObject _gameObject;
    private readonly IWorldDataRequester _worldReq;
    private readonly Vector3 _position;
    private readonly Bounds _bounds;

    private readonly MeshRenderer _meshRenderer;
    private readonly MeshFilter _meshFilter;

    private readonly LevelOfDetailSpecificData[] _lodSpecificData = new LevelOfDetailSpecificData[DetailLevels.Length];

    private WorldChunk _world;
    private int _currendLodIndex = -1;

    public struct LevelOfDetailSetting
    {
        public int SetLevelOfDetail;
        public float UsedBelowThisThreshold;
    }

    public static readonly LevelOfDetailSetting[] DetailLevels =
    {
         new() { SetLevelOfDetail = 0, UsedBelowThisThreshold = 2, },
         new() { SetLevelOfDetail = 1, UsedBelowThisThreshold = 4, },
         new() { SetLevelOfDetail = 2, UsedBelowThisThreshold = 6, },
         new() { SetLevelOfDetail = 3, UsedBelowThisThreshold = 10, },
    };

    public static float MaxViewDist { get { return DetailLevels.Last().UsedBelowThisThreshold; } }

    public TerrainChunk(Vector3Int coords, Transform parent, IWorldDataRequester worldReq /*, Material material*/)
    {
        // Debug.LogFormat("TerrainChunk {0} created by thread {1}", coords, Thread.CurrentThread.ManagedThreadId);
        _ownerThreadId = Thread.CurrentThread.ManagedThreadId;

        _worldReq = worldReq;

        _position.x = coords.x * WorldChunk.Size.x;
        _position.y = coords.y * WorldChunk.Size.y;
        _position.z = coords.z * WorldChunk.Size.z;
        _bounds = new Bounds(_position, WorldChunk.Size);

        _gameObject = new GameObject("Terrain Chunk");
        _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
        _meshFilter = _gameObject.AddComponent<MeshFilter>();
        // _meshRenderer.material = material;

        _gameObject.transform.position = _position;
        _gameObject.transform.parent = parent;
        IsActive = false;

        for (int i = 0; i < DetailLevels.Length; i++)
        {
            _lodSpecificData[i] = new LevelOfDetailSpecificData();
        }

        _worldReq.RequestWorldData(coords);
    }

    internal void OnWorldChunkReceived(WorldChunk chunk, Vector3 viewerPos)
    {
        _world = chunk;

        // Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
        // _meshRenderer.material.mainTexture = texture;

        UpdateLevelOfDetail(viewerPos);
    }

    public bool IsWorldChunkReceived { get { return _world != null; } }

    public static int? GetLodIndexFromDistance(float distance)
    {
        for (int i = 0; i < DetailLevels.Length; i++)
        {
            if (distance <= DetailLevels[i].UsedBelowThisThreshold)
                return i;
        }

        // nothing found..
        return null;
    }

    public void UpdateLevelOfDetail(Vector3 viewerPos)
    {
        if (!IsOwnerThread())
        {
            Debug.LogErrorFormat("TerrainChunk updated by wrong thread!");
            return;
        }

        if (!IsWorldChunkReceived)
            return;

        float viewerDistFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPos));
        int? lodIndex = GetLodIndexFromDistance(viewerDistFromNearestEdge);

        if (!lodIndex.HasValue)
        {
            IsActive = false;
            return;
        }

        if (lodIndex != _currendLodIndex)
        {
            LevelOfDetailSpecificData lod = _lodSpecificData[lodIndex.Value];
            if (lod.HasMesh)
            {
                //    previousLODIndex = lodIndex;
                //    meshFilter.mesh = lodMesh.mesh;
                // print(string.Format("SetMesh {0}", this.position));
            }
            else if (!lod.HasRequestedMesh)
            {
                // lodMesh.RequestMesh(mapData);
            }
        }

        IsActive = true;
    }

    public bool IsActive
    {
        get { return _gameObject.activeSelf; }
        private set { _gameObject.SetActive(value); }
    }

    private bool IsOwnerThread()
    {
        return Thread.CurrentThread.ManagedThreadId == _ownerThreadId;
    }

    class LevelOfDetailSpecificData
    {
        public Mesh Mesh { get; private set; }
        public bool HasRequestedMesh { get; private set; }

        public bool HasMesh { get { return Mesh != null; } }

        /*
        void OnMeshDataReceived(MeshData meshData)
        {
            // print("Mesh data received");
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasReqMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
        */
    }
}
