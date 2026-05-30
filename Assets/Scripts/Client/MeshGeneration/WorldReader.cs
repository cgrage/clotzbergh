using System;
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
        private readonly ChunkCoords _worldChunkCoords;
        private readonly WorldChunk _neighborWorldXM1;
        private readonly WorldChunk _neighborWorldXP1;
        private readonly WorldChunk _neighborWorldYM1;
        private readonly WorldChunk _neighborWorldYP1;
        private readonly WorldChunk _neighborWorldZM1;
        private readonly WorldChunk _neighborWorldZP1;
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

        public WorldReader(ClientChunk chunk, KlotzRegion cutout = null)
        {
            _worldChunk = chunk.World;
            _worldChunkCoords = chunk.Coords;
            _neighborWorldXM1 = chunk.NeighborXM1?.World;
            _neighborWorldXP1 = chunk.NeighborXP1?.World;
            _neighborWorldYM1 = chunk.NeighborYM1?.World;
            _neighborWorldYP1 = chunk.NeighborYP1?.World;
            _neighborWorldZM1 = chunk.NeighborZM1?.World;
            _neighborWorldZP1 = chunk.NeighborZP1?.World;
            _cutoutRegion = cutout ?? KlotzRegion.Empty;
        }

        public void MoveTo(int x, int y, int z)
        {
            _x = x;
            _y = y;
            _z = z;
            _subKlotz = _worldChunk.Get(x, y, z);
            _exposed = 0;

            bool isOpaqueAndNotCut = _subKlotz.IsOpaque &&
               !_cutoutRegion.Contains(_worldChunkCoords, x, y, z);

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

        private bool IsSideExposedXM1()
        {
            if (_cutoutRegion.Contains(_worldChunkCoords, _x - 1, _y, _z)) // is cut out?
                return true;

            if (_x > 0) // if within current chunk: regular case
                return !_worldChunk.Get(_x - 1, _y, _z).IsOpaque;

            if (_neighborWorldXM1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldXM1.Get(WorldDef.ChunkSubDivsX - 1, _y, _z).IsOpaque)
                return true; // same LOD neighbor block missing

            return false;
        }

        private bool IsSideExposedXP1()
        {
            if (_cutoutRegion.Contains(_worldChunkCoords, _x + 1, _y, _z)) // is cut out?
                return true;

            SubKlotz neighborBlock;

            if (_x < WorldDef.ChunkSubDivsX - 1)
            {
                // if within current chunk: regular case
                neighborBlock = _worldChunk.Get(_x + 1, _y, _z);
            }
            else
            {
                if (_neighborWorldXP1 == null)
                    return false; // neighbor chunk not known

                neighborBlock = _neighborWorldXP1.Get(0, _y, _z);
            }

            if (!neighborBlock.IsOpaque)
                return true; // same LOD neighbor block missing

            return false;
        }

        private bool IsSideExposedYM1()
        {
            if (_cutoutRegion.Contains(_worldChunkCoords, _x, _y - 1, _z)) // is cut out?
                return true;

            if (_y > 0) // if within current chunk: regular case
                return !_worldChunk.Get(_x, _y - 1, _z).IsOpaque;

            if (_neighborWorldYM1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldYM1.Get(_x, WorldDef.ChunkSubDivsY - 1, _z).IsOpaque)
                return true; // same LOD neighbor block missing

            return false;
        }

        private bool IsSideExposedYP1()
        {
            if (_cutoutRegion.Contains(_worldChunkCoords, _x, _y + 1, _z)) // is cut out?
                return true;

            if (_y < WorldDef.ChunkSubDivsY - 1) // if within current chunk: regular case
                return !_worldChunk.Get(_x, _y + 1, _z).IsOpaque;

            if (_neighborWorldYP1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldYP1.Get(_x, 0, _z).IsOpaque)
                return true; // same LOD neighbor block missing

            return false;
        }

        private bool IsSideExposedZM1()
        {
            if (_cutoutRegion.Contains(_worldChunkCoords, _x, _y, _z - 1)) // is cut out?
                return true;

            if (_z > 0) // if within current chunk: regular case
                return !_worldChunk.Get(_x, _y, _z - 1).IsOpaque;

            if (_neighborWorldZM1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldZM1.Get(_x, _y, WorldDef.ChunkSubDivsZ - 1).IsOpaque)
                return true; // same LOD neighbor block missing

            return false;
        }

        private bool IsSideExposedZP1()
        {
            if (_cutoutRegion.Contains(_worldChunkCoords, _x, _y, _z + 1)) // is cut out?
                return true;

            if (_z < WorldDef.ChunkSubDivsZ - 1) // if within current chunk: regular case
                return !_worldChunk.Get(_x, _y, _z + 1).IsOpaque;

            if (_neighborWorldZP1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldZP1.Get(_x, _y, 0).IsOpaque)
                return true; // same LOD neighbor block missing

            return false;
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
