using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Client
{
    /// <summary>
    /// Generates the meshes. When called on <c>GenerateTerrainMesh</c> it generates 
    /// the mesh for a <c>ClientChunk</c> and its inner <c>WorldChunk</c>.
    /// Uses the neighbors of the <c>ClientChunk</c> to find adjacent world 
    /// information to draw the mesh correctly.
    /// For overlapping Klotzes the general rule is that the chunk with the root
    /// <c>SubKlotz</c> owns the Klotz (that is the <c>SubKlotz</c> with the sub-
    /// coords {0,0,0}).
    /// </summary>
    public static class MeshGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        public static VoxelMeshBuilder GenerateTerrainMesh(ClientChunk chunk, int lod, KlotzRegion? cutout = null)
        {
            if (lod < 0 || lod > 4)
                throw new ArgumentOutOfRangeException("lod", "lod must be 0 to 4");

            WorldChunk worldChunk = chunk.World;
            if (worldChunk == null)
                return null;

            int lodSkip = 1 << lod; // 1, 2, 4, 8, or 16
            WorldReader reader = new(chunk, lodSkip, cutout);
            VoxelMeshBuilder builder = new(WorldDef.ChunkSize, WorldDef.ChunkSubDivs / lodSkip);

            for (int z = 0, zi = 0; z < WorldDef.ChunkSubDivsZ; z += lodSkip, zi++)
            {
                for (int y = 0, yi = 0; y < WorldDef.ChunkSubDivsY; y += lodSkip, yi++)
                {
                    for (int x = 0, xi = 0; x < WorldDef.ChunkSubDivsX; x += lodSkip, xi++)
                    {
                        reader.MoveTo(x, y, z);
                        if (!reader.IsExposed)
                            continue;

                        SubKlotz? kRoot = reader.RootSubKlotz;
                        if (!kRoot.HasValue)
                            continue; // can't access the root sub-klotz

                        KlotzType type = kRoot.Value.Type;
                        builder.MoveTo(xi, yi, zi);
                        builder.SetColor(kRoot.Value.Color);
                        builder.SetVariant(kRoot.Value.Variant);

                        if (reader.IsExposedXM1) builder.AddLeftFace();
                        if (reader.IsExposedXP1) builder.AddRightFace();
                        if (reader.IsExposedYM1) builder.AddBottomFace(lod == 0 && KlotzKB.TypeHasBottomHoles(type) ? KlotzSideFlags.HasHoles : 0);
                        if (reader.IsExposedYP1) builder.AddTopFace(lod == 0 && KlotzKB.TypeHasTopStuds(type) ? KlotzSideFlags.HasStuds : 0);
                        if (reader.IsExposedZM1) builder.AddBackFace();
                        if (reader.IsExposedZP1) builder.AddFrontFace();
                    }
                }
            }

            return builder;
        }
    }

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
        private readonly KlotzRegion? _cutout;

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

        public WorldReader(ClientChunk chunk, int lodSkip = 1, KlotzRegion? cutout = null)
        {
            _worldChunk = chunk.World;
            _neighborWorldXM1 = chunk.NeighborXM1?.World;
            _neighborWorldXP1 = chunk.NeighborXP1?.World;
            _neighborWorldYM1 = chunk.NeighborYM1?.World;
            _neighborWorldYP1 = chunk.NeighborYP1?.World;
            _neighborWorldZM1 = chunk.NeighborZM1?.World;
            _neighborWorldZP1 = chunk.NeighborZP1?.World;
            _lodSkip = lodSkip;
            _cutout = cutout;
        }

        public void MoveTo(int x, int y, int z)
        {
            _x = x;
            _y = y;
            _z = z;
            _subKlotz = _worldChunk.Get(x, y, z);
            _exposed = 0;

            if (_subKlotz.IsOpaque)
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
            if (_x > 0) // if within current chunk: regular case
                return !_worldChunk.Get(_x - _lodSkip, _y, _z).IsOpaque;

            if (_neighborWorldXM1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldXM1.Get(WorldDef.ChunkSubDivsX - _lodSkip, _y, _z).IsOpaque)
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !_neighborWorldXM1.Get(WorldDef.ChunkSubDivsX - _lodSkip / 2, _y, _z).IsOpaque ||
                !_neighborWorldXM1.Get(WorldDef.ChunkSubDivsX - _lodSkip / 2, _y, _z + _lodSkip / 2).IsOpaque ||
                !_neighborWorldXM1.Get(WorldDef.ChunkSubDivsX - _lodSkip / 2, _y + _lodSkip / 2, _z).IsOpaque ||
                !_neighborWorldXM1.Get(WorldDef.ChunkSubDivsX - _lodSkip / 2, _y + _lodSkip / 2, _z + _lodSkip / 2).IsOpaque;
        }

        private bool IsSideExposedXP1()
        {
            if (_x < WorldDef.ChunkSubDivsX - _lodSkip) // if within current chunk: regular case
                return !_worldChunk.Get(_x + _lodSkip, _y, _z).IsOpaque;

            if (_neighborWorldXP1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldXP1.Get(0, _y, _z).IsOpaque)
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !_neighborWorldXP1.Get(0, _y, _z + _lodSkip / 2).IsOpaque ||
                !_neighborWorldXP1.Get(0, _y + _lodSkip / 2, _z).IsOpaque ||
                !_neighborWorldXP1.Get(0, _y + _lodSkip / 2, _z + _lodSkip / 2).IsOpaque;
        }

        private bool IsSideExposedYM1()
        {
            if (_y > 0) // if within current chunk: regular case
                return !_worldChunk.Get(_x, _y - _lodSkip, _z).IsOpaque;

            if (_neighborWorldYM1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldYM1.Get(_x, WorldDef.ChunkSubDivsY - _lodSkip, _z).IsOpaque)
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !_neighborWorldYM1.Get(_x, WorldDef.ChunkSubDivsY - _lodSkip / 2, _z).IsOpaque ||
                !_neighborWorldYM1.Get(_x, WorldDef.ChunkSubDivsY - _lodSkip / 2, _z + _lodSkip / 2).IsOpaque ||
                !_neighborWorldYM1.Get(_x + _lodSkip / 2, WorldDef.ChunkSubDivsY - _lodSkip / 2, _z).IsOpaque ||
                !_neighborWorldYM1.Get(_x + _lodSkip / 2, WorldDef.ChunkSubDivsY - _lodSkip / 2, _z + _lodSkip / 2).IsOpaque;
        }

        private bool IsSideExposedYP1()
        {
            if (_y < WorldDef.ChunkSubDivsY - _lodSkip) // if within current chunk: regular case
                return !_worldChunk.Get(_x, _y + _lodSkip, _z).IsOpaque;

            if (_neighborWorldYP1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldYP1.Get(_x, 0, _z).IsOpaque)
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !_neighborWorldYP1.Get(_x, 0, _z + _lodSkip / 2).IsOpaque ||
                !_neighborWorldYP1.Get(_x + _lodSkip / 2, 0, _z).IsOpaque ||
                !_neighborWorldYP1.Get(_x + _lodSkip / 2, 0, _z + _lodSkip / 2).IsOpaque;
        }

        private bool IsSideExposedZM1()
        {
            if (_z > 0) // if within current chunk: regular case
                return !_worldChunk.Get(_x, _y, _z - _lodSkip).IsOpaque;

            if (_neighborWorldZM1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldZM1.Get(_x, _y, WorldDef.ChunkSubDivsZ - _lodSkip).IsOpaque)
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !_neighborWorldZM1.Get(_x, _y, WorldDef.ChunkSubDivsX - _lodSkip / 2).IsOpaque ||
                !_neighborWorldZM1.Get(_x, _y + _lodSkip / 2, WorldDef.ChunkSubDivsX - _lodSkip / 2).IsOpaque ||
                !_neighborWorldZM1.Get(_x + _lodSkip / 2, _y, WorldDef.ChunkSubDivsX - _lodSkip / 2).IsOpaque ||
                !_neighborWorldZM1.Get(_x + _lodSkip / 2, _y + _lodSkip / 2, WorldDef.ChunkSubDivsX - _lodSkip / 2).IsOpaque;
        }

        private bool IsSideExposedZP1()
        {
            if (_z < WorldDef.ChunkSubDivsX - _lodSkip) // if within current chunk: regular case
                return !_worldChunk.Get(_x, _y, _z + _lodSkip).IsOpaque;

            if (_neighborWorldZP1 == null)
                return false; // neighbor chunk not known

            if (!_neighborWorldZP1.Get(_x, _y, 0).IsOpaque)
                return true; // same LOD neighbor block missing

            // check one LOD below the current one
            if (_lodSkip == 1)
                return false;

            return
                !_neighborWorldZP1.Get(_x, _y + _lodSkip / 2, 0).IsOpaque ||
                !_neighborWorldZP1.Get(_x + _lodSkip / 2, _y, 0).IsOpaque ||
                !_neighborWorldZP1.Get(_x + _lodSkip / 2, _y + _lodSkip / 2, 0).IsOpaque;
        }

        public Vector3Int RootPos { get { return _subKlotz.RootPos(new(_x, _y, _z)); } }

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

        private SubKlotz? At(Vector3Int coords)
        {
            return At(coords.x, coords.y, coords.z);
        }
    }

    public class MeshBuilder
    {
        public List<Vector3> Vertices { get; private set; }
        public List<int> Triangles { get; private set; }
        public List<Vector2> UvData { get; private set; }

        public MeshBuilder(int estimatedVertexCount = 0, int estimatedTriangleCount = 0)
        {
            Vertices = new(estimatedVertexCount);
            Triangles = new List<int>(capacity: estimatedTriangleCount * 3);
            UvData = new List<Vector2>(estimatedVertexCount);
        }

        public MeshBuilder(Vector3[] vertices, int[] triangles, Vector2[] uvData)
        {
            Vertices = new(vertices);
            Triangles = new(triangles);
            UvData = new(uvData);
        }

        public static Vector2 BuildVertexUvData(KlotzSide side, KlotzVertexFlags flags, KlotzColor color, KlotzVariant variant)
        {
            float x = (((uint)color) << 3) | ((uint)side);
            float y = (((uint)flags) << 7) | (uint)variant;
            return new Vector2(x, y);
        }

        public void AddTriangle(int a, int b, int c)
        {
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new()
            {
                vertices = Vertices.ToArray(),
                triangles = Triangles.ToArray(),
                uv = UvData.ToArray(),

            };

            return mesh;
        }
    }

    public class VoxelMeshBuilder : MeshBuilder
    {
        private readonly Vector3 _segmentSize;

        private KlotzColor _color;

        private KlotzVariant _variant;

        private float _x1, _x2, _y1, _y2, _z1, _z2;

        private Vector3Int _currentCoords;

        /// <summary>
        /// Can be used to look-up the voxel coords once you know the triangle index.
        /// </summary>
        public List<Vector3Int> VoxelCoords { get; private set; }

        public VoxelMeshBuilder(Vector3 size, Vector3Int subDivs)
        {
            _segmentSize = new(size.x / subDivs.x, size.y / subDivs.y, size.z / subDivs.z);
            _color = KlotzColor.White;
            _variant = KlotzVariant.Zero;

            VoxelCoords = new();
        }

        public void MoveTo(int x, int y, int z)
        {
            _currentCoords = new(x, y, z);
            _x1 = x * _segmentSize.x;
            _x2 = _x1 + _segmentSize.x;
            _y1 = y * _segmentSize.y;
            _y2 = _y1 + _segmentSize.y;
            _z1 = z * _segmentSize.z;
            _z2 = _z1 + _segmentSize.z;
        }

        public void SetColor(KlotzColor color)
        {
            _color = color;
        }

        public void SetVariant(KlotzVariant variant)
        {
            _variant = variant;
        }

        /// <summary>
        /// A.K.A. the left face
        /// </summary>
        public void AddLeftFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x1, _y1, _z2), new(_x1, _y2, _z2), new(_x1, _y2, _z1), new(_x1, _y1, _z1),
                KlotzSide.Left, flags);
        }

        /// <summary>
        /// A.K.A. the right face
        /// </summary>
        public void AddRightFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x2, _y1, _z1), new(_x2, _y2, _z1), new(_x2, _y2, _z2), new(_x2, _y1, _z2),
                KlotzSide.Right, flags);
        }

        /// <summary>
        /// A.K.A. the bottom face
        /// </summary>
        public void AddBottomFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x2, _y1, _z1), new(_x2, _y1, _z2), new(_x1, _y1, _z2), new(_x1, _y1, _z1),
                KlotzSide.Bottom, flags);
        }

        /// <summary>
        /// A.K.A. the top face
        /// </summary>
        public void AddTopFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x1, _y2, _z1), new(_x1, _y2, _z2), new(_x2, _y2, _z2), new(_x2, _y2, _z1),
                KlotzSide.Top, flags);
        }

        /// <summary>
        /// A.K.A. the back face
        /// </summary>
        public void AddBackFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x1, _y2, _z1), new(_x2, _y2, _z1), new(_x2, _y1, _z1), new(_x1, _y1, _z1),
                KlotzSide.Back, flags);
        }

        /// <summary>
        /// A.K.A. the front face
        /// </summary>
        public void AddFrontFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x1, _y1, _z2), new(_x2, _y1, _z2), new(_x2, _y2, _z2), new(_x1, _y2, _z2),
                KlotzSide.Front, flags);
        }

        /// <summary>
        /// Adds a face to the current mesh (-builder)
        /// </summary>
        private void AddFace(Vector3 corner1, Vector3 corner2, Vector3 corner3, Vector3 corner4, KlotzSide side, KlotzSideFlags sideFlags)
        {
            int v0 = Vertices.Count;

            Vertices.Add(corner1);
            Vertices.Add(corner2);
            Vertices.Add(corner3);
            Vertices.Add(corner4);

            KlotzVertexFlags flags = 0;
            if (sideFlags.HasFlag(KlotzSideFlags.HasStuds)) flags |= KlotzVertexFlags.SideHasStuds;
            if (sideFlags.HasFlag(KlotzSideFlags.HasHoles)) flags |= KlotzVertexFlags.SideHasHoles;

            Vector2 vertexData = BuildVertexUvData(side, flags, _color, _variant);
            UvData.Add(vertexData); UvData.Add(vertexData); UvData.Add(vertexData); UvData.Add(vertexData);

            Triangles.Add(v0 + 0); Triangles.Add(v0 + 1); Triangles.Add(v0 + 2);
            Triangles.Add(v0 + 0); Triangles.Add(v0 + 2); Triangles.Add(v0 + 3);

            VoxelCoords.Add(_currentCoords); VoxelCoords.Add(_currentCoords);
        }
    }
}
