using System;
using System.Collections.Generic;
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
        private readonly HashSet<long> _cutRoots;
        private readonly int _originX, _originY, _originZ;
        private readonly int _reachMinX, _reachMinY, _reachMinZ;
        private readonly int _reachMaxX, _reachMaxY, _reachMaxZ;

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
            _originX = _worldChunkCoords.X * WorldDef.ChunkSubDivsX;
            _originY = _worldChunkCoords.Y * WorldDef.ChunkSubDivsY;
            _originZ = _worldChunkCoords.Z * WorldDef.ChunkSubDivsZ;

            // Cells this far outside the region can still belong to a klotz reaching into it;
            // anything beyond cannot, and is rejected before touching the lookup below.
            BoundsInt rough = _cutoutRegion.RoughBounds;
            int reachXZ = KlotzKB.MaxExtentXZ;
            int reachY = KlotzKB.MaxExtentY;
            _reachMinX = rough.xMin - reachXZ; _reachMaxX = rough.xMax + reachXZ;
            _reachMinY = rough.yMin - reachY; _reachMaxY = rough.yMax + reachY;
            _reachMinZ = rough.zMin - reachXZ; _reachMaxZ = rough.zMax + reachXZ;

            _cutRoots = BuildCutRoots();
        }

        /// <summary>
        /// Root positions of every klotz reaching into the cutout region, so that testing a cell
        /// during meshing is a lookup rather than a klotz resolve plus an intersection test.
        /// Walking the region once costs about as much as a few hundred cells, against the tens
        /// of thousands the mesh pass would otherwise pay it on.
        /// </summary>
        private HashSet<long> BuildCutRoots()
        {
            if (_cutoutRegion.IsEmpty)
                return null;

            HashSet<long> roots = new();
            BoundsInt bounds = _cutoutRegion.RoughBounds;

            for (int absY = bounds.yMin; absY <= bounds.yMax; absY++)
            {
                for (int absZ = bounds.zMin; absZ <= bounds.zMax; absZ++)
                {
                    for (int absX = bounds.xMin; absX <= bounds.xMax; absX++)
                    {
                        AbsKlotzCoords abs = new(absX, absY, absZ);
                        if (!_cutoutRegion.IntersectsAbs(abs, abs))
                            continue;

                        int x = absX - _originX;
                        int y = absY - _originY;
                        int z = absZ - _originZ;

                        SubKlotz? cell = AtReachable(x, y, z);
                        if (!cell.HasValue || (cell.Value.IsRoot && cell.Value.IsAir))
                            continue;

                        RelKlotzCoords root = cell.Value.RootPos(new(x, y, z));
                        roots.Add(PackAbs(root.X + _originX, root.Y + _originY, root.Z + _originZ));
                    }
                }
            }

            return roots;
        }

        /// <summary>
        /// Absolute coords as one key. World limits keep each axis far inside 21 bits, and
        /// masking maps negatives onto a stable representative.
        /// </summary>
        private static long PackAbs(int x, int y, int z)
        {
            return ((long)(x & 0x1fffff) << 42) | ((long)(y & 0x1fffff) << 21) | (long)(z & 0x1fffff);
        }

        /// <summary>
        /// Whether the klotz owning a cell is cut away by the selection. A klotz goes as a whole
        /// as soon as any part of it falls inside the region, so none is ever left sliced open.
        /// Takes the cell's value because every caller has already read it.
        /// </summary>
        private bool IsKlotzCut(SubKlotz cell, int x, int y, int z)
        {
            if (_cutRoots == null)
                return false;

            int absX = x + _originX;
            int absY = y + _originY;
            int absZ = z + _originZ;

            if (absX < _reachMinX || absX > _reachMaxX ||
                absY < _reachMinY || absY > _reachMaxY ||
                absZ < _reachMinZ || absZ > _reachMaxZ)
                return false;

            // Air is the only cell not belonging to a klotz, and is always stored as a root.
            if (cell.IsRoot && cell.IsAir)
                return false;

            RelKlotzCoords root = cell.RootPos(new(x, y, z));
            return _cutRoots.Contains(PackAbs(root.X + _originX, root.Y + _originY, root.Z + _originZ));
        }

        public void MoveTo(int x, int y, int z)
        {
            _x = x;
            _y = y;
            _z = z;
            _subKlotz = _worldChunk.Get(x, y, z);
            _exposed = 0;

            if (_subKlotz.IsOpaque && !IsKlotzCut(_subKlotz, x, y, z))
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
            int nx = _x - 1;
            SubKlotz neighbor;

            if (nx >= 0)
                neighbor = _worldChunk.Get(nx, _y, _z);
            else if (_neighborWorldXM1 != null)
                neighbor = _neighborWorldXM1.Get(WorldDef.ChunkSubDivsX - 1, _y, _z);
            else
                return false;

            return !neighbor.IsOpaque || IsKlotzCut(neighbor, nx, _y, _z);
        }

        private bool IsSideExposedXP1()
        {
            int nx = _x + 1;
            SubKlotz neighbor;

            if (nx < WorldDef.ChunkSubDivsX)
                neighbor = _worldChunk.Get(nx, _y, _z);
            else if (_neighborWorldXP1 != null)
                neighbor = _neighborWorldXP1.Get(0, _y, _z);
            else
                return false;

            return !neighbor.IsOpaque || IsKlotzCut(neighbor, nx, _y, _z);
        }

        private bool IsSideExposedYM1()
        {
            int ny = _y - 1;
            SubKlotz neighbor;

            if (ny >= 0)
                neighbor = _worldChunk.Get(_x, ny, _z);
            else if (_neighborWorldYM1 != null)
                neighbor = _neighborWorldYM1.Get(_x, WorldDef.ChunkSubDivsY - 1, _z);
            else
                return false;

            return !neighbor.IsOpaque || IsKlotzCut(neighbor, _x, ny, _z);
        }

        private bool IsSideExposedYP1()
        {
            int ny = _y + 1;
            SubKlotz neighbor;

            if (ny < WorldDef.ChunkSubDivsY)
                neighbor = _worldChunk.Get(_x, ny, _z);
            else if (_neighborWorldYP1 != null)
                neighbor = _neighborWorldYP1.Get(_x, 0, _z);
            else
                return false;

            return !neighbor.IsOpaque || IsKlotzCut(neighbor, _x, ny, _z);
        }

        private bool IsSideExposedZM1()
        {
            int nz = _z - 1;
            SubKlotz neighbor;

            if (nz >= 0)
                neighbor = _worldChunk.Get(_x, _y, nz);
            else if (_neighborWorldZM1 != null)
                neighbor = _neighborWorldZM1.Get(_x, _y, WorldDef.ChunkSubDivsZ - 1);
            else
                return false;

            return !neighbor.IsOpaque || IsKlotzCut(neighbor, _x, _y, nz);
        }

        private bool IsSideExposedZP1()
        {
            int nz = _z + 1;
            SubKlotz neighbor;

            if (nz < WorldDef.ChunkSubDivsZ)
                neighbor = _worldChunk.Get(_x, _y, nz);
            else if (_neighborWorldZP1 != null)
                neighbor = _neighborWorldZP1.Get(_x, _y, 0);
            else
                return false;

            return !neighbor.IsOpaque || IsKlotzCut(neighbor, _x, _y, nz);
        }

        public bool IsRoot { get { return _subKlotz.IsRoot; } }

        /// <summary>
        /// Whether the klotz at the current position is cut away by the selection region. For
        /// primitives this already shows up as no side being exposed, but non-primitives are
        /// meshed from their root cell alone and have to ask.
        /// </summary>
        public bool IsCut { get { return IsKlotzCut(_subKlotz, _x, _y, _z); } }

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

        /// <summary>
        /// Like <see cref="At"/>, but yields null instead of reading past a chunk this reader
        /// does not hold. <see cref="At"/> resolves one axis into a face neighbour, so a cell
        /// outside on two axes at once sits in a diagonal chunk that is not available here.
        /// </summary>
        private SubKlotz? AtReachable(int x, int y, int z)
        {
            int outside = 0;
            if (x < 0 || x >= WorldDef.ChunkSubDivsX) outside++;
            if (y < 0 || y >= WorldDef.ChunkSubDivsY) outside++;
            if (z < 0 || z >= WorldDef.ChunkSubDivsZ) outside++;

            return outside > 1 ? null : At(x, y, z);
        }
    }

}
