using UnityEngine;

namespace Clotzbergh.Client.MeshGeneration
{
    /// <summary>
    /// Helper class to stitch multiple world chunks together.
    /// Always operates from the perspective of the chunk given to the constructor. 
    /// </summary>
    public class WorldReader
    {
        private readonly WorldChunk _worldChunk;
        private readonly WorldChunk _neighborWorldXM1;
        private readonly WorldChunk _neighborWorldXP1;
        private readonly WorldChunk _neighborWorldYM1;
        private readonly WorldChunk _neighborWorldYP1;
        private readonly WorldChunk _neighborWorldZM1;
        private readonly WorldChunk _neighborWorldZP1;
        private readonly int _lodSkip = 1;
        private readonly KlotzRegion _cutoutRegion;

        private int _x, _y, _z;
        private SubKlotz _subKlotz;
        private int _exposed = 0;

        private bool GetExposed(int i) { return (_exposed & (1 << i)) != 0; }
        private void SetExposed(int i) { _exposed |= 1 << i; }

        /// <summary>
        /// Gets a value indicating whether any side of the current <see cref="SubKlotz"/> is exposed.
        /// A side is considered exposed if it is adjacent to a non-opaque block or the edge of the world.
        /// </summary>
        public bool IsExposed { get { return _exposed != 0; } }
        public bool IsExposedXM1 { get { return GetExposed(0); } }
        public bool IsExposedXP1 { get { return GetExposed(1); } }
        public bool IsExposedYM1 { get { return GetExposed(2); } }
        public bool IsExposedYP1 { get { return GetExposed(3); } }
        public bool IsExposedZM1 { get { return GetExposed(4); } }
        public bool IsExposedZP1 { get { return GetExposed(5); } }

        public WorldReader(ClientChunk chunk, int lodSkip = 1, KlotzRegion cutout = null)
        {
            _worldChunk = chunk.World;
            _neighborWorldXM1 = chunk.NeighborXM1?.World;
            _neighborWorldXP1 = chunk.NeighborXP1?.World;
            _neighborWorldYM1 = chunk.NeighborYM1?.World;
            _neighborWorldYP1 = chunk.NeighborYP1?.World;
            _neighborWorldZM1 = chunk.NeighborZM1?.World;
            _neighborWorldZP1 = chunk.NeighborZP1?.World;
            _lodSkip = lodSkip;
            _cutoutRegion = cutout;
        }

        public void MoveTo(int x, int y, int z)
        {
            _x = x;
            _y = y;
            _z = z;
            _subKlotz = _worldChunk.Get(x, y, z);
            _exposed = 0;

            bool isOpaqueAndNotCut = _subKlotz.IsOpaque &&
                (_cutoutRegion == null || !_cutoutRegion.Contains(new RelKlotzCoords(x, y, z)));

            if (isOpaqueAndNotCut)
            {
                if (IsSideExposedXM1()) SetExposed(0);
                if (IsSideExposedXP1()) SetExposed(1);
                if (IsSideExposedYM1()) SetExposed(2);
                if (IsSideExposedYP1()) SetExposed(3);
                if (IsSideExposedZM1()) SetExposed(4);
                if (IsSideExposedZP1()) SetExposed(5);
            }
        }

        public void MoveTo(Vector3Int coords)
        {
            MoveTo(coords.x, coords.y, coords.z);
        }

        private bool IsOpaqueAndNotCut(WorldChunk chunk, int x, int y, int z)
        {
            return chunk.Get(x, y, z).IsOpaque;
        }

        private bool IsSideExposedXM1()
        {
            if (_x > 0) // if within current chunk: regular case
                return !IsOpaqueAndNotCut(_worldChunk, _x - _lodSkip, _y, _z);

            if (_neighborWorldXM1 == null)
                return false; // neighbor chunk not known

            if (!IsOpaqueAndNotCut(_neighborWorldXM1, WorldDef.ChunkSubDivsX - _lodSkip, _y, _z))
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !IsOpaqueAndNotCut(_neighborWorldXM1, WorldDef.ChunkSubDivsX - _lodSkip / 2, _y, _z) ||
                !IsOpaqueAndNotCut(_neighborWorldXM1, WorldDef.ChunkSubDivsX - _lodSkip / 2, _y, _z + _lodSkip / 2) ||
                !IsOpaqueAndNotCut(_neighborWorldXM1, WorldDef.ChunkSubDivsX - _lodSkip / 2, _y + _lodSkip / 2, _z) ||
                !IsOpaqueAndNotCut(_neighborWorldXM1, WorldDef.ChunkSubDivsX - _lodSkip / 2, _y + _lodSkip / 2, _z + _lodSkip / 2);
        }

        private bool IsSideExposedXP1()
        {
            if (_x < WorldDef.ChunkSubDivsX - _lodSkip) // if within current chunk: regular case
                return !IsOpaqueAndNotCut(_worldChunk, _x + _lodSkip, _y, _z);

            if (_neighborWorldXP1 == null)
                return false; // neighbor chunk not known

            if (!IsOpaqueAndNotCut(_neighborWorldXP1, 0, _y, _z))
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !IsOpaqueAndNotCut(_neighborWorldXP1, 0, _y, _z + _lodSkip / 2) ||
                !IsOpaqueAndNotCut(_neighborWorldXP1, 0, _y + _lodSkip / 2, _z) ||
                !IsOpaqueAndNotCut(_neighborWorldXP1, 0, _y + _lodSkip / 2, _z + _lodSkip / 2);
        }

        private bool IsSideExposedYM1()
        {
            if (_y > 0) // if within current chunk: regular case
                return !IsOpaqueAndNotCut(_worldChunk, _x, _y - _lodSkip, _z);

            if (_neighborWorldYM1 == null)
                return false; // neighbor chunk not known

            if (!IsOpaqueAndNotCut(_neighborWorldYM1, _x, WorldDef.ChunkSubDivsY - _lodSkip, _z))
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !IsOpaqueAndNotCut(_neighborWorldYM1, _x, WorldDef.ChunkSubDivsY - _lodSkip / 2, _z) ||
                !IsOpaqueAndNotCut(_neighborWorldYM1, _x, WorldDef.ChunkSubDivsY - _lodSkip / 2, _z + _lodSkip / 2) ||
                !IsOpaqueAndNotCut(_neighborWorldYM1, _x + _lodSkip / 2, WorldDef.ChunkSubDivsY - _lodSkip / 2, _z) ||
                !IsOpaqueAndNotCut(_neighborWorldYM1, _x + _lodSkip / 2, WorldDef.ChunkSubDivsY - _lodSkip / 2, _z + _lodSkip / 2);
        }

        private bool IsSideExposedYP1()
        {
            if (_y < WorldDef.ChunkSubDivsY - _lodSkip) // if within current chunk: regular case
                return !IsOpaqueAndNotCut(_worldChunk, _x, _y + _lodSkip, _z);

            if (_neighborWorldYP1 == null)
                return false; // neighbor chunk not known

            if (!IsOpaqueAndNotCut(_neighborWorldYP1, _x, 0, _z))
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !IsOpaqueAndNotCut(_neighborWorldYP1, _x, 0, _z + _lodSkip / 2) ||
                !IsOpaqueAndNotCut(_neighborWorldYP1, _x + _lodSkip / 2, 0, _z) ||
                !IsOpaqueAndNotCut(_neighborWorldYP1, _x + _lodSkip / 2, 0, _z + _lodSkip / 2);
        }

        private bool IsSideExposedZM1()
        {
            if (_z > 0) // if within current chunk: regular case
                return !IsOpaqueAndNotCut(_worldChunk, _x, _y, _z - _lodSkip);

            if (_neighborWorldZM1 == null)
                return false; // neighbor chunk not known

            if (!IsOpaqueAndNotCut(_neighborWorldZM1, _x, _y, WorldDef.ChunkSubDivsZ - _lodSkip))
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !IsOpaqueAndNotCut(_neighborWorldZM1, _x, _y, WorldDef.ChunkSubDivsX - _lodSkip / 2) ||
                !IsOpaqueAndNotCut(_neighborWorldZM1, _x, _y + _lodSkip / 2, WorldDef.ChunkSubDivsX - _lodSkip / 2) ||
                !IsOpaqueAndNotCut(_neighborWorldZM1, _x + _lodSkip / 2, _y, WorldDef.ChunkSubDivsX - _lodSkip / 2) ||
                !IsOpaqueAndNotCut(_neighborWorldZM1, _x + _lodSkip / 2, _y + _lodSkip / 2, WorldDef.ChunkSubDivsX - _lodSkip / 2);
        }

        private bool IsSideExposedZP1()
        {
            if (_z < WorldDef.ChunkSubDivsX - _lodSkip) // if within current chunk: regular case
                return !IsOpaqueAndNotCut(_worldChunk, _x, _y, _z + _lodSkip);

            if (_neighborWorldZP1 == null)
                return false; // neighbor chunk not known

            if (!IsOpaqueAndNotCut(_neighborWorldZP1, _x, _y, 0))
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !IsOpaqueAndNotCut(_neighborWorldZP1, _x, _y + _lodSkip / 2, 0) ||
                !IsOpaqueAndNotCut(_neighborWorldZP1, _x + _lodSkip / 2, _y, 0) ||
                !IsOpaqueAndNotCut(_neighborWorldZP1, _x + _lodSkip / 2, _y + _lodSkip / 2, 0);
        }

        public RelKlotzCoords RootPos { get { return _subKlotz.RootPos(new(_x, _y, _z)); } }

        public SubKlotz? RootSubKlotz
        {
            get
            {
                if (_subKlotz.IsRoot)
                    return _subKlotz;

                return At(RootPos);
            }
        }

        private SubKlotz? At(int x, int y, int z)
        {
            if (x < 0)
                return _neighborWorldXM1?.Get(x + WorldDef.ChunkSubDivsX, y, z);

            if (x >= WorldDef.ChunkSubDivsX)
                return _neighborWorldXP1?.Get(x - WorldDef.ChunkSubDivsX, y, z);

            if (y < 0)
                return _neighborWorldYM1?.Get(x, y + WorldDef.ChunkSubDivsY, z);

            if (y >= WorldDef.ChunkSubDivsY)
                return _neighborWorldYP1?.Get(x, y - WorldDef.ChunkSubDivsY, z);

            if (z < 0)
                return _neighborWorldZM1?.Get(x, y, z + WorldDef.ChunkSubDivsZ);

            if (z >= WorldDef.ChunkSubDivsZ)
                return _neighborWorldZP1?.Get(x, y, z - WorldDef.ChunkSubDivsZ);

            return _worldChunk.Get(x, y, z);
        }

        private SubKlotz? At(RelKlotzCoords coords)
        {
            return At(coords.X, coords.Y, coords.Z);
        }
    }

}
