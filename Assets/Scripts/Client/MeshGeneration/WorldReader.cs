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
        private RelKlotzCoords _rootPos;
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
            _rootPos = _subKlotz.RootPos(new(x, y, z));
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
            SubKlotz neighbor;
            if (_x > 0)
                neighbor = _worldChunk.Get(_x - 1, _y, _z);
            else if (_neighborWorldXM1 != null)
                neighbor = _neighborWorldXM1.Get(WorldDef.ChunkSubDivsX - 1, _y, _z);
            else
                return false;

            if (_cutoutRegion.Contains(_worldChunkCoords, _x - 1, _y, _z))
                return _rootPos != neighbor.RootPos(new(_x - 1, _y, _z));

            return !neighbor.IsOpaque;
        }

        private bool IsSideExposedXP1()
        {
            SubKlotz neighbor;
            if (_x < WorldDef.ChunkSubDivsX - 1)
                neighbor = _worldChunk.Get(_x + 1, _y, _z);
            else if (_neighborWorldXP1 != null)
                neighbor = _neighborWorldXP1.Get(0, _y, _z);
            else
                return false;

            if (_cutoutRegion.Contains(_worldChunkCoords, _x + 1, _y, _z))
                return _rootPos != neighbor.RootPos(new(_x + 1, _y, _z));

            return !neighbor.IsOpaque;
        }

        private bool IsSideExposedYM1()
        {
            SubKlotz neighbor;
            if (_y > 0)
                neighbor = _worldChunk.Get(_x, _y - 1, _z);
            else if (_neighborWorldYM1 != null)
                neighbor = _neighborWorldYM1.Get(_x, WorldDef.ChunkSubDivsY - 1, _z);
            else
                return false;

            if (_cutoutRegion.Contains(_worldChunkCoords, _x, _y - 1, _z))
                return _rootPos != neighbor.RootPos(new(_x, _y - 1, _z));

            return !neighbor.IsOpaque;
        }

        private bool IsSideExposedYP1()
        {
            SubKlotz neighbor;
            if (_y < WorldDef.ChunkSubDivsY - 1)
                neighbor = _worldChunk.Get(_x, _y + 1, _z);
            else if (_neighborWorldYP1 != null)
                neighbor = _neighborWorldYP1.Get(_x, 0, _z);
            else
                return false;

            if (_cutoutRegion.Contains(_worldChunkCoords, _x, _y + 1, _z))
                return _rootPos != neighbor.RootPos(new(_x, _y + 1, _z));

            return !neighbor.IsOpaque;
        }

        private bool IsSideExposedZM1()
        {
            SubKlotz neighbor;
            if (_z > 0)
                neighbor = _worldChunk.Get(_x, _y, _z - 1);
            else if (_neighborWorldZM1 != null)
                neighbor = _neighborWorldZM1.Get(_x, _y, WorldDef.ChunkSubDivsZ - 1);
            else
                return false;

            if (_cutoutRegion.Contains(_worldChunkCoords, _x, _y, _z - 1))
                return _rootPos != neighbor.RootPos(new(_x, _y, _z - 1));

            return !neighbor.IsOpaque;
        }

        private bool IsSideExposedZP1()
        {
            SubKlotz neighbor;
            if (_z < WorldDef.ChunkSubDivsZ - 1)
                neighbor = _worldChunk.Get(_x, _y, _z + 1);
            else if (_neighborWorldZP1 != null)
                neighbor = _neighborWorldZP1.Get(_x, _y, 0);
            else
                return false;

            if (_cutoutRegion.Contains(_worldChunkCoords, _x, _y, _z + 1))
                return _rootPos != neighbor.RootPos(new(_x, _y, _z + 1));

            return !neighbor.IsOpaque;
        }

        public bool IsRoot { get { return _subKlotz.IsRoot; } }

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
