using System.Linq;
using System.Threading;
using UnityEngine;

public class TerrainChunk
{
    private readonly string _id;
    private readonly Vector3Int _coords;
    private readonly int _ownerThreadId;
    private readonly GameObject _gameObject;
    private readonly IAsyncTerrainOps _asyncOps;

    private readonly MeshRenderer _meshRenderer;
    private readonly MeshFilter _meshFilter;
    private readonly MeshCollider _meshCollider;

    private readonly LevelOfDetailSpecificData[] _lodSpecificData = new LevelOfDetailSpecificData[WorldDef.MaxLodValue + 1];

    private bool _isCleanedUp = false;
    private WorldChunk _currentWorld;
    /// <summary>
    /// The _currentWorldVersion is a purely local counter. 
    /// It even goes up when the neighbors world version changed.
    /// </summary>
    private ulong _currentWorldVersion = 0;
    private bool _currentWorldRequested = false;
    private int _currentLevelOfDetail = -1;
    private int _loadPriority = 1000; // less is higher priority

    public WorldChunk World => _currentWorld;
    public string Id => _id;
    public Vector3Int Coords => _coords;
    public int LoadPriority => _loadPriority;

    public TerrainChunk NeighborXM1 { get; set; }
    public TerrainChunk NeighborXP1 { get; set; }
    public TerrainChunk NeighborYM1 { get; set; }
    public TerrainChunk NeighborYP1 { get; set; }
    public TerrainChunk NeighborZM1 { get; set; }
    public TerrainChunk NeighborZP1 { get; set; }

    public TerrainChunk(Vector3Int coords, Transform parent, IAsyncTerrainOps asyncOps, Material material)
    {
        _id = $"Terrain Chunk ({coords.x},{coords.y},{coords.z})";
        _coords = coords;
        _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        _asyncOps = asyncOps;

        _gameObject = new GameObject(_id);
        _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
        _meshFilter = _gameObject.AddComponent<MeshFilter>();
        _meshCollider = _gameObject.AddComponent<MeshCollider>();

        _gameObject.transform.position = WorldChunk.ChunkCoordsToPosition(coords);
        _gameObject.transform.parent = parent;
        _meshRenderer.material = material;
        IsActive = false;

        for (int i = 0; i < _lodSpecificData.Length; i++)
        {
            _lodSpecificData[i] = new LevelOfDetailSpecificData();
        }
    }

    public bool RequestWorldIfNeeded()
    {
        if (_currentWorld != null)
            return false;

        if (_currentWorldRequested)
            return false;

        // Debug.Log($"Request world for ${_coords} (Prio: {_loadPriority})");
        _asyncOps?.RequestWorldData(_coords);
        _currentWorldRequested = true;
        return true;
    }

    public void CleanUp()
    {
        ExpectRunningOnOwnerThread();

        GameObject.Destroy(_gameObject);
        _isCleanedUp = true;
    }

    public void OnWorldUpdate(WorldChunk world)
    {
        if (_isCleanedUp)
            return;

        // store the data
        _currentWorld = world;
        IncWorldVersion();

        // update neighbors (TODO: Only if required?)
        NeighborXM1?.IncWorldVersion();
        NeighborXP1?.IncWorldVersion();
        NeighborYM1?.IncWorldVersion();
        NeighborYP1?.IncWorldVersion();
        NeighborZM1?.IncWorldVersion();
        NeighborZP1?.IncWorldVersion();
    }

    public void OnMeshUpdate(MeshBuilder meshData, int levelOfDetail, ulong worldVersion)
    {
        if (_isCleanedUp)
            return;

        var lodData = _lodSpecificData[levelOfDetail];

        // is update really an update?
        if (worldVersion < lodData.worldVersion)
            return;

        lodData.mesh = meshData.ToMesh();
        lodData.worldVersion = worldVersion;

        SetCurrentMeshIfAvailable();
    }

    public static int? GetLodFromDistance(int chunkDistance)
    {
        foreach (var entry in WorldDef.DetailLevels)
        {
            if (chunkDistance <= entry.MaxThreshold)
                return entry.LevelOfDetail;
        }

        // nothing found..
        return null;
    }

    public bool RequestMeshUpdatesIfNeeded()
    {
        if (_currentLevelOfDetail == -1 || _currentWorld == null)
            return false;

        var lodData = _lodSpecificData[_currentLevelOfDetail];

        // are we up to date?
        if (lodData.worldVersion == _currentWorldVersion)
            return false;

        // we are not up to date. have we at least requested the data?
        if (lodData.requestedWorldVersion >= _currentWorldVersion)
            return false;

        _asyncOps?.RequestMeshCalc(this, _currentWorld, _currentLevelOfDetail, _currentWorldVersion);
        lodData.requestedWorldVersion = _currentWorldVersion;
        return true;
    }

    /// <summary>
    /// This method is expected to be run on main thread.
    /// </summary>
    public void OnViewerMoved(int viewerChunkDist)
    {
        ExpectRunningOnOwnerThread();

        if (_isCleanedUp)
            return;

        // find new level of detail for this distance
        int? levelOfDetail = GetLodFromDistance(viewerChunkDist);
        if (!levelOfDetail.HasValue || levelOfDetail.Value == -1)
        {
            _currentLevelOfDetail = -1;
            IsActive = false;
            return;
        }

        _currentLevelOfDetail = levelOfDetail.Value;
        _loadPriority = viewerChunkDist;

        SetCurrentMeshIfAvailable();
        IsActive = true;
    }

    private void SetCurrentMeshIfAvailable()
    {
        if (_currentLevelOfDetail == -1)
            return;

        var lodData = _lodSpecificData[_currentLevelOfDetail];

        // we simply show the newest mesh we have, even if is it a bit outdated.
        _meshFilter.mesh = lodData.mesh;
        _meshCollider.sharedMesh = lodData.mesh;
    }

    private void IncWorldVersion()
    {
        _currentWorldVersion++;
    }

    public bool IsActive
    {
        get { return _gameObject.activeSelf; }
        private set { _gameObject.SetActive(value); }
    }

    private void ExpectRunningOnOwnerThread()
    {
        if (Thread.CurrentThread.ManagedThreadId == _ownerThreadId)
            return;

        throw new System.Exception("TerrainChunk updated by wrong thread!");
    }

    class LevelOfDetailSpecificData
    {
        public Mesh mesh = null;
        public ulong worldVersion = 0;
        public ulong requestedWorldVersion = 0;
    }
}
