using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainChunk
{
    private readonly GameObject _gameObject;
    private readonly ITerrainDataRequester _terrainReq;
    private readonly Vector3 _position;
    private readonly Bounds _bounds;

    private readonly MeshRenderer _meshRenderer;
    private readonly MeshFilter _meshFilter;

    //LODMesh[] lodMeshes;

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

    public TerrainChunk(Vector3Int coords, Transform parent, ITerrainDataRequester terrainReq /*, Material material*/)
    {
        _terrainReq = terrainReq;

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

        // lodMeshes = new LODMesh[detailLevels.Length];
        //for (int i = 0; i < lodMeshes.Length; i++)
        //{
        //    lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
        //}

        _terrainReq.RequestWorldData(coords);
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
        if (!IsWorldChunkReceived)
            return;

        // print(string.Format("UpdateTerrainChunk {0}", this.position));

        float viewerDistFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPos));
        int? lodIndex = GetLodIndexFromDistance(viewerDistFromNearestEdge);

        if (!lodIndex.HasValue)
        {
            IsActive = false;
            return;
        }

        if (lodIndex != _currendLodIndex)
        {
            // LODMesh lodMesh = lodMeshes[lodIndex];
            //if (lodMesh.hasMesh)
            //{
            //    previousLODIndex = lodIndex;
            //    meshFilter.mesh = lodMesh.mesh;
            // print(string.Format("SetMesh {0}", this.position));
            //}
            //else if (!lodMesh.hasReqMesh)
            //{
            //    lodMesh.RequestMesh(mapData);
            //}
        }

        IsActive = true;
    }

    public bool IsActive
    {
        get { return _gameObject.activeSelf; }
        private set { _gameObject.SetActive(value); }
    }
}

/*
class LODMesh
{
    public Mesh mesh;
    public bool hasReqMesh;
    public bool hasMesh;
    int lod;
    Action updateCallback;

    public LODMesh(int lod, Action updateCallback)
    {
        this.lod = lod;
        this.updateCallback = updateCallback;
    }

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