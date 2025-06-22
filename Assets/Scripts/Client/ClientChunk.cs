using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Clotzbergh.Client.MeshGeneration;

namespace Clotzbergh.Client
{
    public class ClientChunk
    {
        private readonly string _id;
        private readonly Vector3Int _coords;
        private readonly int _ownerThreadId;
        private readonly GameObject _gameObject;
        private readonly IClientSideOps _asyncOps;
        private readonly PlayerSelection _selection;

        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;

        private readonly Dictionary<int, LevelOfDetailSpecificData> _lodSpecificData = new();

        private bool _isCleanedUp = false;
        private WorldChunk _currentWorld;
        private ulong _currentWorldServerVersion = 0;
        /// <summary>
        /// The _worldLocalVersion is a purely local counter. 
        /// It even goes up when the neighbors _worldLocalVersion changes.
        /// </summary>
        private ulong _worldLocalVersion = 0;
        private int _currentLevelOfDetail = -1;
        private int _viewerChunkDist = -1;
        private int _loadPriority = 1000; // less is higher priority
        private long _lastSeenSelection = -1; // used to detect changes in selection

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
            public Mesh colliderMesh = null;
            /// <summary>
            /// The voxel coordinates of the mesh triangles that can be used to find the klotz at a triangle index.
            /// </summary>
            public Vector3Int[] colliderMeshVoxelCoords = null;
            public Mesh visualMesh = null;
            public ulong worldLocalVersion = 0;
            public ulong requestedWorldLocalVersion = 0;
        }

        public ClientChunk(Vector3Int coords, Transform parent, IClientSideOps asyncOps, PlayerSelection selection, Material material)
        {
            _id = $"Terrain Chunk ({coords.x},{coords.y},{coords.z})";
            _coords = coords;
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
            _asyncOps = asyncOps;
            _selection = selection;

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
        }

        LevelOfDetailSpecificData GetLodData(int levelOfDetail)
        {
            if (!_lodSpecificData.TryGetValue(levelOfDetail, out var lodData))
            {
                lodData = new LevelOfDetailSpecificData();
                _lodSpecificData.Add(levelOfDetail, lodData);
            }

            return lodData;
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

            var lodData = GetLodData(levelOfDetail);

            // is update really an update?
            if (worldLocalVersion < lodData.worldLocalVersion)
                return;

            lodData.colliderMesh = meshData.ToMesh();
            lodData.colliderMeshVoxelCoords = meshData.VoxelCoords.ToArray();
            lodData.visualMesh = lodData.colliderMesh; // we use the same mesh for visualization and collision
            lodData.worldLocalVersion = worldLocalVersion;

            SetCurrentMeshIfAvailable();
        }

        /// <summary>
        /// This method is expected to be run on main thread.
        /// </summary>
        /// <returns>True if a mesh update was requested or if the mesh was updated directly.</returns>
        public bool UpdateMeshOrRequestMeshUpdateIfNeeded()
        {
            if (_currentLevelOfDetail == -1 || _currentWorld == null)
                return false; // no data available

            var lodData = GetLodData(_currentLevelOfDetail);
            bool isNewestVersion = lodData.worldLocalVersion == _worldLocalVersion;
            bool isVeryClose = _viewerChunkDist < 1;

            if (isVeryClose)
            {
                // we are very close to the chunk, so we can generate the mesh directly on the main thread.

                long selectionChangeCount = _selection == null ? -1 : _selection.ChangeCount;
                bool needsSelectionUpdate = _lastSeenSelection != selectionChangeCount || !isNewestVersion;

                if (isNewestVersion && !needsSelectionUpdate)
                    return false;

                if (!isNewestVersion)
                {
                    VoxelMeshBuilder meshData = MeshGenerator.GenerateTerrainMesh(this, _currentLevelOfDetail);
                    lodData.colliderMesh = meshData.ToMesh();
                    lodData.colliderMeshVoxelCoords = meshData.VoxelCoords.ToArray();
                    lodData.worldLocalVersion = _worldLocalVersion;
                    lodData.requestedWorldLocalVersion = _worldLocalVersion;
                }

                if (needsSelectionUpdate)
                {
                    VoxelMeshBuilder meshData = MeshGenerator.GenerateTerrainMesh(this, _currentLevelOfDetail, _selection.Cutout);
                    lodData.visualMesh = meshData.ToMesh();
                    _lastSeenSelection = selectionChangeCount;
                }

                SetCurrentMeshIfAvailable();
                return true;
            }
            else
            {
                // are we up to date?
                if (isNewestVersion)
                    return false;

                // we are not up to date. have we at least requested the data?
                if (lodData.requestedWorldLocalVersion >= _worldLocalVersion)
                    return false;

                _asyncOps?.RequestMeshCalc(this, _currentWorld, _currentLevelOfDetail, _worldLocalVersion);
                lodData.requestedWorldLocalVersion = _worldLocalVersion;
                return true;
            }
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
            int? levelOfDetail = WorldDef.GetLodFromDistance(viewerChunkDist);
            if (!levelOfDetail.HasValue || levelOfDetail.Value == -1)
            {
                _currentLevelOfDetail = -1;
                _viewerChunkDist = -1;
                IsActive = false;
                return;
            }

            _currentLevelOfDetail = levelOfDetail.Value;
            _viewerChunkDist = viewerChunkDist;
            _loadPriority = viewerChunkDist;

            SetCurrentMeshIfAvailable();
            IsActive = true;
        }

        private void SetCurrentMeshIfAvailable()
        {
            if (_currentLevelOfDetail == -1)
                return;

            var lodData = GetLodData(_currentLevelOfDetail);

            // we simply show the newest mesh we have, even if is it a bit outdated.
            _meshFilter.mesh = lodData.visualMesh;
            _meshCollider.sharedMesh = lodData.colliderMesh;
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

            if (!_lodSpecificData.TryGetValue(0, out var lodData))
                return null;

            if (lodData.colliderMeshVoxelCoords == null)
                return null;

            Vector3Int subKlotzCoords = lodData.colliderMeshVoxelCoords[triangleIndex];
            return GetKlotzAt(subKlotzCoords);
        }

        public void TakeKlotz(Vector3Int innerChunkCoords)
        {
            _asyncOps?.TakeKlotz(_coords, innerChunkCoords);
        }
    }
}
