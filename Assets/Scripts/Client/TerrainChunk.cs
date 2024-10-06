using System.Linq;
using System.Threading;
using UnityEngine;

public class TerrainChunk
{
    private readonly string _id;
    private readonly int _ownerThreadId;
    private readonly GameObject _gameObject;
    private readonly IAsyncTerrainOps _asyncOps;
    private readonly Vector3 _position;
    private readonly Bounds _bounds;

    private readonly MeshRenderer _meshRenderer;
    private readonly MeshFilter _meshFilter;
    private readonly MeshCollider _meshCollider;

    private readonly LevelOfDetailSpecificData[] _lodSpecificData = new LevelOfDetailSpecificData[DetailLevels.Length];

    private WorldChunk _world;
    private int _currentLodIndex = -1;

    public struct LevelOfDetailSetting
    {
        public int SetLevelOfDetail;
        public float UsedBelowThisThreshold;
    }

    public static readonly LevelOfDetailSetting[] DetailLevels =
    {
         //new() { SetLevelOfDetail = 0, UsedBelowThisThreshold = 4, },
         //new() { SetLevelOfDetail = 1, UsedBelowThisThreshold = 8, },
         //new() { SetLevelOfDetail = 2, UsedBelowThisThreshold = 12, },
         new() { SetLevelOfDetail = 1, UsedBelowThisThreshold = 20, },
    };

    public static float MaxViewDist { get { return DetailLevels.Last().UsedBelowThisThreshold; } }

    public WorldChunk World { get { return _world; } }
    public string Id { get { return _id; } }

    private TerrainChunk _neighborXM1;
    private TerrainChunk _neighborXP1;
    private TerrainChunk _neighborYM1;
    private TerrainChunk _neighborYP1;
    private TerrainChunk _neighborZM1;
    private TerrainChunk _neighborZP1;

    public TerrainChunk(Vector3Int coords, Transform parent, IAsyncTerrainOps asyncOps, Material material)
    {
        _id = $"Terrain Chunk ({coords.x},{coords.y},{coords.z})";
        _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        _asyncOps = asyncOps;

        _position.x = coords.x * WorldChunk.Size.x;
        _position.y = coords.y * WorldChunk.Size.y;
        _position.z = coords.z * WorldChunk.Size.z;
        _bounds = new Bounds(_position, WorldChunk.Size);

        _gameObject = new GameObject(_id);
        _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
        _meshFilter = _gameObject.AddComponent<MeshFilter>();
        _meshCollider = _gameObject.AddComponent<MeshCollider>();

        _gameObject.transform.position = _position;
        _gameObject.transform.parent = parent;
        _meshRenderer.material = material;
        IsActive = false;

        for (int i = 0; i < DetailLevels.Length; i++)
        {
            _lodSpecificData[i] = new LevelOfDetailSpecificData();
        }

        _asyncOps?.RequestWorldData(coords);
    }

    public void OnWorldChunkReceived(WorldChunk worldChunk, Vector3 viewerPos)
    {
        _world = worldChunk;

        // Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
        // _meshRenderer.material.mainTexture = texture;

        UpdateLevelOfDetail(viewerPos);
    }

    public void OnMeshDataReceived(MeshBuilder meshData, int lodIndex, Vector3 viewerPos)
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

    /// <summary>
    /// This method is expected to be run on main thread.
    /// </summary>
    public void UpdateLevelOfDetail(Vector3 viewerPos)
    {
        ExpectRunningOnOwnerThread();

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
        if (lodIndex != _currentLodIndex)
        {
            var lodData = _lodSpecificData[lodIndex.Value];
            if (lodData.HasMesh)
            {
                // print(string.Format("SetMesh {0}", this.position));
                _meshFilter.mesh = lodData.Mesh;
                _meshCollider.sharedMesh = lodData.Mesh;
                _currentLodIndex = lodIndex.Value;
            }
            else if (!lodData.IsMeshRequested)
            {
                _asyncOps?.RequestMeshCalc(this, _world, lodValue, lodIndex.Value);
                lodData.IsMeshRequested = true;
            }
        }

        IsActive = true;
    }

    /// <summary>
    /// This method is expected to be run on main thread.
    /// </summary>
    private void OnNeighborChanged()
    {
        ExpectRunningOnOwnerThread();

        // TODO: Here we need to request a new mesh.
        // We need a mechanism for that which is more robust than the current one.
    }

    public bool IsActive
    {
        get { return _gameObject.activeSelf; }
        private set { _gameObject.SetActive(value); }
    }

    public TerrainChunk NeighborXM1
    {
        get { return _neighborXM1; }
        set
        {
            if (_neighborXM1 != value)
            {
                _neighborXM1 = value;
                OnNeighborChanged();
            }
        }
    }

    public TerrainChunk NeighborXP1
    {
        get { return _neighborXP1; }
        set
        {
            if (_neighborXP1 != value)
            {
                _neighborXP1 = value;
                OnNeighborChanged();
            }
        }
    }

    public TerrainChunk NeighborYM1
    {
        get { return _neighborYM1; }
        set
        {
            if (_neighborYM1 != value)
            {
                _neighborYM1 = value;
                OnNeighborChanged();
            }
        }
    }

    public TerrainChunk NeighborYP1
    {
        get { return _neighborYP1; }
        set
        {
            if (_neighborYP1 != value)
            {
                _neighborYP1 = value;
                OnNeighborChanged();
            }
        }
    }

    public TerrainChunk NeighborZM1
    {
        get { return _neighborZM1; }
        set
        {
            if (_neighborZM1 != value)
            {
                _neighborZM1 = value;
                OnNeighborChanged();
            }
        }
    }

    public TerrainChunk NeighborZP1
    {
        get { return _neighborZP1; }
        set
        {
            if (_neighborZP1 != value)
            {
                _neighborZP1 = value;
                OnNeighborChanged();
            }
        }
    }

    private void ExpectRunningOnOwnerThread()
    {
        if (Thread.CurrentThread.ManagedThreadId == _ownerThreadId)
            return;

        throw new System.Exception("TerrainChunk updated by wrong thread!");
    }

    class LevelOfDetailSpecificData
    {
        public Mesh Mesh { get; set; }
        public bool IsMeshRequested { get; set; }

        public bool HasMesh { get { return Mesh != null; } }
    }
}
