using System.Threading;
using UnityEngine;

namespace Clotzbergh.Client
{
    public class ClientChunk
    {
        private readonly string _id;
        private readonly Vector3Int _coords;
        private readonly int _ownerThreadId;
        private readonly GameObject _gameObject;
        private readonly IClientSideOps _asyncOps;

        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;

        private readonly LevelOfDetailSpecificData[] _lodSpecificData = new LevelOfDetailSpecificData[WorldDef.MaxLodValue + 1];

        private bool _isCleanedUp = false;
        private WorldChunk _currentWorld;
        private ulong _currentWorldServerVersion = 0;
        /// <summary>
        /// The _worldLocalVersion is a purely local counter. 
        /// It even goes up when the neighbors _worldLocalVersion changes.
        /// </summary>
        private ulong _worldLocalVersion = 0;
        private int _currentLevelOfDetail = -1;
        private int _loadPriority = 1000; // less is higher priority

        public WorldChunk World => _currentWorld;
        public string Id => _id;
        public Vector3Int Coords => _coords;
        public int LoadPriority => _loadPriority;

        public ClientChunk NeighborXM1 { get; set; }
        public ClientChunk NeighborXP1 { get; set; }
        public ClientChunk NeighborYM1 { get; set; }
        public ClientChunk NeighborYP1 { get; set; }
        public ClientChunk NeighborZM1 { get; set; }
        public ClientChunk NeighborZP1 { get; set; }

        public class OwnerRef : MonoBehaviour
        {
            public ClientChunk owner;
        }

        class LevelOfDetailSpecificData
        {
            public Mesh mesh = null;
            public Vector3Int[] voxelCoords = null;
            public ulong worldLocalVersion = 0;
            public ulong requestedWorldLocalVersion = 0;
        }

        public ClientChunk(Vector3Int coords, Transform parent, IClientSideOps asyncOps, Material material)
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

        public void CleanUp()
        {
            ExpectRunningOnOwnerThread();

            GameObject.Destroy(_gameObject);
            _isCleanedUp = true;
        }

        public void OnWorldUpdate(ulong version, WorldChunk world)
        {
            if (_isCleanedUp)
                return;

            // needs to be newer than what we have
            if (version <= _currentWorldServerVersion)
                return;

            // store the data
            _currentWorld = world;
            _currentWorldServerVersion = version;
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

            throw new System.Exception("ClientChunk updated by wrong thread!");
        }

        private KlotzWorldData GetKlotzAt(Vector3Int subKlotzCoords)
        {
            if (_currentWorld == null)
                return null;

            WorldReader reader = new(this);
            reader.MoveTo(subKlotzCoords);
            SubKlotz? root = reader.RootSubKlotz;

            if (!root.HasValue)
                return null;

            KlotzType type = root.Value.Type;
            KlotzDirection dir = root.Value.Direction;

            Vector3 innerPos = SubKlotz.TranslateSubKlotzCoordToWorldLocation(reader.RootPos, dir);
            Vector3 pos = innerPos + WorldChunk.ChunkCoordsToPosition(_coords);
            Vector3 size = Vector3.Scale(KlotzKB.KlotzSize(type), WorldDef.SubKlotzSize);
            Quaternion rotation = SubKlotz.KlotzDirectionToQuaternion(dir);

            return new()
            {
                rootCoords = reader.RootPos,
                worldPosition = pos,
                worldSize = size,
                worldRotation = rotation,
                type = type,
                isFreeToTake = true,
            };
        }

        public KlotzWorldData GetKlotzFromTriangleIndex(int triangleIndex)
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
            _asyncOps?.TakeKlotz(_coords, innerChunkCoords);
        }
    }
}
