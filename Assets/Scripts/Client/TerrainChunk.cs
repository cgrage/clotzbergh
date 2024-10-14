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
    /// The _worldLocalVersion is a purely local counter. 
    /// It even goes up when the neighbors _worldLocalVersion changes.
    /// </summary>
    private ulong _worldLocalVersion = 0;
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

    public class OwnerRef : MonoBehaviour
    {
        public TerrainChunk owner;
    }

    class LevelOfDetailSpecificData
    {
        public Mesh mesh = null;
        public Vector3Int[] voxelCoords = null;
        public ulong worldLocalVersion = 0;
        public ulong requestedWorldLocalVersion = 0;
    }

    public TerrainChunk(Vector3Int coords, Transform parent, IAsyncTerrainOps asyncOps, Material material)
    {
        _id = $"Terrain Chunk ({coords.x},{coords.y},{coords.z})";
        _coords = coords;
        _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        _asyncOps = asyncOps;

        _gameObject = new GameObject(_id);
        _gameObject.AddComponent<OwnerRef>().owner = this;
        _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
        _meshFilter = _gameObject.AddComponent<MeshFilter>();
        _meshCollider = _gameObject.AddComponent<MeshCollider>();
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
        IncWorldLocalVersion();

        // update neighbors (TODO: Only if required?)
        NeighborXM1?.IncWorldLocalVersion();
        NeighborXP1?.IncWorldLocalVersion();
        NeighborYM1?.IncWorldLocalVersion();
        NeighborYP1?.IncWorldLocalVersion();
        NeighborZM1?.IncWorldLocalVersion();
        NeighborZP1?.IncWorldLocalVersion();
    }

    public void OnMeshUpdate(VoxelMeshBuilder meshData, int levelOfDetail, ulong worldLocalVersion)
    {
        if (_isCleanedUp)
            return;

        var lodData = _lodSpecificData[levelOfDetail];

        // is update really an update?
        if (worldLocalVersion < lodData.worldLocalVersion)
            return;

        lodData.mesh = meshData.ToMesh();
        lodData.voxelCoords = meshData.VoxelCoords.ToArray();
        lodData.worldLocalVersion = worldLocalVersion;

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
        if (lodData.worldLocalVersion == _worldLocalVersion)
            return false;

        // we are not up to date. have we at least requested the data?
        if (lodData.requestedWorldLocalVersion >= _worldLocalVersion)
            return false;

        _asyncOps?.RequestMeshCalc(this, _currentWorld, _currentLevelOfDetail, _worldLocalVersion);
        lodData.requestedWorldLocalVersion = _worldLocalVersion;
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

    private void IncWorldLocalVersion()
    {
        _worldLocalVersion++;
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

    private Klotz GetKlotzAt(Vector3Int subKlotzCoords)
    {
        if (_currentWorld == null)
            return null;

        SubKlotz k = _currentWorld.Get(subKlotzCoords);
        Vector3Int rootPos = k.RootPos(subKlotzCoords);

        Vector3 innerPos = Vector3.Scale(rootPos, WorldDef.SubKlotzSize);
        Vector3 pos = innerPos + WorldChunk.ChunkCoordsToPosition(_coords);
        Vector3 size = Vector3.Scale(KlotzKB.KlotzSize(k.Type, k.Direction), WorldDef.SubKlotzSize);

        return new()
        {
            innerChunkCoords = rootPos,
            worldPosition = pos,
            worldSize = size,
            type = k.Type,
        };
    }

    public Klotz GetKlotzFromTriangleIndex(int triangleIndex)
    {
        if (_currentLevelOfDetail != 0)
            return null;

        if (_lodSpecificData[0].voxelCoords == null)
            return null;

        Vector3Int subKlotzCoords = _lodSpecificData[0].voxelCoords[triangleIndex];
        return GetKlotzAt(subKlotzCoords);
    }

    public void TakeKlotz(Vector3Int innerChunkCoords)
    {
        // _asyncOps?.RequestWorldData
    }
}
