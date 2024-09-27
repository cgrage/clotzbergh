using System.Linq;
using System.Threading;
using UnityEngine;

public class TerrainChunk
{
    private readonly int _ownerThreadId;
    private readonly GameObject _gameObject;
    private readonly IAsyncTerrainOps _asyncOps;
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

    public TerrainChunk(Vector3Int coords, Transform parent, IAsyncTerrainOps asyncOps, Material material)
    {
        _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        _asyncOps = asyncOps;

        _position.x = coords.x * WorldChunk.Size.x;
        _position.y = coords.y * WorldChunk.Size.y;
        _position.z = coords.z * WorldChunk.Size.z;
        _bounds = new Bounds(_position, WorldChunk.Size);

        _gameObject = new GameObject("Terrain Chunk");
        _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
        _meshFilter = _gameObject.AddComponent<MeshFilter>();
        _meshRenderer.material = material;

        _gameObject.transform.position = _position;
        _gameObject.transform.parent = parent;
        IsActive = false;

        for (int i = 0; i < DetailLevels.Length; i++)
        {
            _lodSpecificData[i] = new LevelOfDetailSpecificData();
        }

        _asyncOps.RequestWorldData(coords);
    }

    internal void OnWorldChunkReceived(WorldChunk chunk, Vector3 viewerPos)
    {
        _world = chunk;

        // Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
        // _meshRenderer.material.mainTexture = texture;

        UpdateLevelOfDetail(viewerPos);
    }

    internal void OnMeshDataReceived(MeshBuilder meshData, int lodIndex, Vector3 viewerPos)
    {
        // print("Mesh data received");

        var lodData = _lodSpecificData[lodIndex];
        lodData.Mesh = meshData.ToMesh();

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

        int lodValue = DetailLevels[lodIndex.Value].SetLevelOfDetail;
        if (lodIndex != _currendLodIndex)
        {
            var lodData = _lodSpecificData[lodIndex.Value];
            if (lodData.HasMesh)
            {
                // print(string.Format("SetMesh {0}", this.position));
                _meshFilter.mesh = lodData.Mesh;
                _currendLodIndex = lodIndex.Value;
            }
            else if (!lodData.IsMeshRequested)
            {
                _asyncOps.RequestMeshCalc(this, _world, lodValue, lodIndex.Value);
                lodData.IsMeshRequested = true;
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
        public Mesh Mesh { get; set; }
        public bool IsMeshRequested { get; set; }

        public bool HasMesh { get { return Mesh != null; } }
    }
}
