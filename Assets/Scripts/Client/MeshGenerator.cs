using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates the meshes. When called on <c>GenerateTerrainMesh</c> it generates 
/// the mesh for a <c>ClientChunk</c> and its inner <c>WorldChunk</c>.
/// Uses the neighbors of the <c>ClientChunk</c> to find adjacent world 
/// information to draw the mesh correctly.
/// For overlapping Klotzes the general rule is that the chunk with the root
/// <c>SubKlotz</c> owns the Klotz (that is the <c>SubKlotz</c> with the sub-
/// coords {0,0,0}).
/// </summary>
public class MeshGenerator
{
    public MeshGenerator()
    {
        // 
    }

    /// <summary>
    /// 
    /// </summary>
    public VoxelMeshBuilder GenerateTerrainMesh(ClientChunk chunk, int lod)
    {
        if (lod < 0 || lod > 4)
            throw new ArgumentOutOfRangeException("lod", "lod must be 0 to 4");

        WorldChunk worldChunk = chunk.World;
        if (worldChunk == null)
            return null;

        int lodSkip = 1 << lod; // 1, 2, 4, 8, or 16
        WorldStitcher stitcher = new(chunk);
        VoxelMeshBuilder builder = new(WorldDef.ChunkSize, WorldDef.ChunkSubDivs / lodSkip);

        for (int z = 0, zi = 0; z < WorldDef.ChunkSubDivsZ; z += lodSkip, zi++)
        {
            for (int y = 0, yi = 0; y < WorldDef.ChunkSubDivsY; y += lodSkip, yi++)
            {
                for (int x = 0, xi = 0; x < WorldDef.ChunkSubDivsX; x += lodSkip, xi++)
                {
                    SubKlotz k = worldChunk.Get(x, y, z);
                    bool opaque = k.IsOpaque;
                    if (!opaque)
                        continue; // later this will be more complex, I guess.

                    bool opaqueXM1 = stitcher.IsOpaqueOrUnknownAt(x - lodSkip, y, z);
                    bool opaqueXP1 = stitcher.IsOpaqueOrUnknownAt(x + lodSkip, y, z);
                    bool opaqueYM1 = stitcher.IsOpaqueOrUnknownAt(x, y - lodSkip, z);
                    bool opaqueYP1 = stitcher.IsOpaqueOrUnknownAt(x, y + lodSkip, z);
                    bool opaqueZM1 = stitcher.IsOpaqueOrUnknownAt(x, y, z - lodSkip);
                    bool opaqueZP1 = stitcher.IsOpaqueOrUnknownAt(x, y, z + lodSkip);

                    if (opaqueXM1 && opaqueXP1 && opaqueYM1 && opaqueYP1 && opaqueZM1 && opaqueZP1)
                        continue;

                    SubKlotz? kRoot = stitcher.At(k.RootPos(new Vector3Int(x, y, z)));
                    if (!kRoot.HasValue)
                        continue; // can't access the root sub-klotz

                    KlotzType type = kRoot.Value.Type;
                    builder.MoveTo(xi, yi, zi);
                    builder.SetColor(kRoot.Value.Color);
                    builder.SetVariant(kRoot.Value.Variant);

                    if (!opaqueXM1) builder.AddLeftFace();
                    if (!opaqueXP1) builder.AddRightFace();
                    if (!opaqueYM1) builder.AddBottomFace(lod == 0 && KlotzKB.TypeHasBottomHoles(type) ? KlotzSideFlags.HasHoles : 0);
                    if (!opaqueYP1) builder.AddTopFace(lod == 0 && KlotzKB.TypeHasTopStuds(type) ? KlotzSideFlags.HasStuds : 0);
                    if (!opaqueZM1) builder.AddBackFace();
                    if (!opaqueZP1) builder.AddFrontFace();
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
public class WorldStitcher
{
    private readonly WorldChunk _worldChunk;
    private readonly WorldChunk _neighborXM1;
    private readonly WorldChunk _neighborXP1;
    private readonly WorldChunk _neighborYM1;
    private readonly WorldChunk _neighborYP1;
    private readonly WorldChunk _neighborZM1;
    private readonly WorldChunk _neighborZP1;

    public WorldStitcher(ClientChunk chunk)
    {
        _worldChunk = chunk.World;
        _neighborXM1 = chunk.NeighborXM1?.World;
        _neighborXP1 = chunk.NeighborXP1?.World;
        _neighborYM1 = chunk.NeighborYM1?.World;
        _neighborYP1 = chunk.NeighborYP1?.World;
        _neighborZM1 = chunk.NeighborZM1?.World;
        _neighborZP1 = chunk.NeighborZP1?.World;
    }

    public SubKlotz? At(int x, int y, int z)
    {
        const int MAX_X = WorldDef.ChunkSubDivsX;
        const int MAX_Y = WorldDef.ChunkSubDivsY;
        const int MAX_Z = WorldDef.ChunkSubDivsZ;

        if (x < 0) { return _neighborXM1?.Get(x + MAX_X, y, z); }
        else if (x >= MAX_X) { return _neighborXP1?.Get(x - MAX_X, y, z); }
        else if (y < 0) { return _neighborYM1?.Get(x, y + MAX_Y, z); }
        else if (y >= MAX_Y) { return _neighborYP1?.Get(x, y - MAX_Y, z); }
        else if (z < 0) { return _neighborZM1?.Get(x, y, z + MAX_Z); }
        else if (z >= MAX_Z) { return _neighborZP1?.Get(x, y, z - MAX_Z); }
        else { return _worldChunk.Get(x, y, z); }
    }

    public SubKlotz? At(Vector3Int coords)
    {
        return At(coords.x, coords.y, coords.z);
    }

    public bool IsOpaqueOrUnknownAt(int x, int y, int z)
    {
        SubKlotz? subKlotz = At(x, y, z);
        if (!subKlotz.HasValue)
            return true;

        return subKlotz.Value.IsOpaque;
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
