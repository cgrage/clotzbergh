using System;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    /// <summary>
    /// Stupid little helper
    /// </summary>
    private static Color32 ColorFromHash(int hash)
    {
        System.Random random = new(hash);
        byte r = (byte)random.Next(256);
        byte g = (byte)random.Next(256);
        byte b = (byte)random.Next(256);
        return new Color32(r, g, b, 255);
    }

    public MeshGenerator()
    {
        // 
    }

    /// <summary>
    /// 
    /// </summary>
    public MeshBuilder GenerateTerrainMesh(TerrainChunk terrainChunk, int lod)
    {
        WorldChunk worldChunk = terrainChunk.World;
        if (worldChunk == null)
            return null;

        CellWallBuilder builder = new(WorldChunk.Size, WorldChunk.KlotzCount);
        builder.MeshColor = ColorFromHash(terrainChunk.Id.GetHashCode());

        OpaquenessChecker checker = new(terrainChunk);

        for (int z = 0; z < WorldChunk.KlotzCountZ; z++)
        {
            for (int y = 0; y < WorldChunk.KlotzCountY; y++)
            {
                for (int x = 0; x < WorldChunk.KlotzCountX; x++)
                {
                    bool opaque = checker.IsOpaqueAt(x, y, z).GetValueOrDefault();
                    if (!opaque)
                        continue; // later this will be more complex, I guess.

                    builder.MoveTo(x, y, z);
                    if (!checker.IsOpaqueAt(x - 1, y, z).GetValueOrDefault()) builder.AddFaceXM1();
                    if (!checker.IsOpaqueAt(x + 1, y, z).GetValueOrDefault()) builder.AddFaceXP1();
                    if (!checker.IsOpaqueAt(x, y - 1, z).GetValueOrDefault()) builder.AddFaceYM1();
                    if (!checker.IsOpaqueAt(x, y + 1, z).GetValueOrDefault()) builder.AddFaceYP1();
                    if (!checker.IsOpaqueAt(x, y, z - 1).GetValueOrDefault()) builder.AddFaceZM1();
                    if (!checker.IsOpaqueAt(x, y, z + 1).GetValueOrDefault()) builder.AddFaceZP1();
                }
            }
        }

        return builder;
    }
}

public class OpaquenessChecker
{
    private readonly WorldChunk _worldChunk;
    private readonly WorldChunk _neighborXM1;
    private readonly WorldChunk _neighborXP1;
    private readonly WorldChunk _neighborYM1;
    private readonly WorldChunk _neighborYP1;
    private readonly WorldChunk _neighborZM1;
    private readonly WorldChunk _neighborZP1;

    public OpaquenessChecker(TerrainChunk chunk)
    {
        _worldChunk = chunk.World;
        _neighborXM1 = chunk.NeighborXM1?.World;
        _neighborXP1 = chunk.NeighborXP1?.World;
        _neighborYM1 = chunk.NeighborYM1?.World;
        _neighborYP1 = chunk.NeighborYP1?.World;
        _neighborZM1 = chunk.NeighborZM1?.World;
        _neighborZP1 = chunk.NeighborZP1?.World;
    }

    // shorthand notation
    const int MAX_X = WorldChunk.KlotzCountX;
    const int MAX_Y = WorldChunk.KlotzCountY;
    const int MAX_Z = WorldChunk.KlotzCountZ;

    public bool? IsOpaqueAt(int x, int y, int z)
    {
        if (x < 0) { return _neighborXM1?.Get(x + MAX_X, y, z).IsOpaque; }
        else if (x >= MAX_X) { return _neighborXP1?.Get(x - MAX_X, y, z).IsOpaque; }
        else if (y < 0) { return _neighborYM1?.Get(x, y + MAX_Y, z).IsOpaque; }
        else if (y >= MAX_Y) { return _neighborYP1?.Get(x, y - MAX_Y, z).IsOpaque; }
        else if (z < 0) { return _neighborZM1?.Get(x, y, z + MAX_Z).IsOpaque; }
        else if (z >= MAX_Z) { return _neighborZP1?.Get(x, y, z - MAX_Z).IsOpaque; }
        else { return _worldChunk.Get(x, y, z).IsOpaque; }
    }
}

public class MeshBuilder
{
    public List<Vector3> Vertices { get; private set; }
    public List<Color32> Colors { get; private set; }
    public List<Vector3> Normals { get; private set; }
    public List<int> Triangles { get; private set; }

    public MeshBuilder(int estimatedVertexCount = 0, int estimatedTriangleCount = 0)
    {
        Vertices = new(estimatedVertexCount);
        Colors = new List<Color32>(estimatedVertexCount);
        Normals = new List<Vector3>(estimatedVertexCount);
        Triangles = new List<int>(capacity: estimatedTriangleCount * 3);
    }

    public MeshBuilder(Vector3[] vertices, Color32[] colors, Vector3[] normals, int[] triangles)
    {
        Vertices = new(vertices);
        Colors = new(colors);
        Normals = new(normals);
        Triangles = new(triangles);
    }

    public static MeshBuilder FromCuboid(Vector3 pos, Vector3 size, Color32 color)
    {
        float x1 = pos.x, y1 = pos.y, z1 = pos.z;
        float x2 = x1 + size.x, y2 = y1 + size.y, z2 = z1 + size.z;

        Vector3[] vertices = {
            new(x1, y1, z1), new(x1, y2, z1), new(x1, y2, z2), new(x1, y1, z2), // Left face
            new(x2, y1, z1), new(x2, y2, z1), new(x2, y2, z2), new(x2, y1, z2), // Right face
            new(x1, y1, z1), new(x1, y1, z2), new(x2, y1, z2), new(x2, y1, z1), // Bottom face
            new(x1, y2, z1), new(x1, y2, z2), new(x2, y2, z2), new(x2, y2, z1), // Top face
            new(x1, y1, z1), new(x2, y1, z1), new(x2, y2, z1), new(x1, y2, z1), // Back face
            new(x1, y1, z2), new(x2, y1, z2), new(x2, y2, z2), new(x1, y2, z2), // Front face
        };

        Color32[] colors = {
            color, color, color, color,
            color, color, color, color,
            color, color, color, color,
            color, color, color, color,
            color, color, color, color,
            color, color, color, color,
        };

        Vector3[] normals = {
            new(-1,  0,  0), new(-1,  0,  0), new(-1,  0,  0), new(-1,  0,  0), // Left face
            new( 1,  0,  0), new( 1,  0,  0), new( 1,  0,  0), new( 1,  0,  0), // Right face
            new( 0, -1,  0), new( 0, -1,  0), new( 0, -1,  0), new( 0, -1,  0), // Bottom face
            new( 0,  1,  0), new( 0,  1,  0), new( 0,  1,  0), new( 0,  1,  0), // Top face
            new( 0,  0, -1), new( 0,  0, -1), new( 0,  0, -1), new( 0,  0, -1), // Back face
            new( 0,  0,  1), new( 0,  0,  1), new( 0,  0,  1), new( 0,  0,  1), // Front face
        };

        int[] triangles = {
            00+0, 00+2, 00+1,   00+0, 00+3, 00+2, // Left face
            04+0, 04+1, 04+2,   04+0, 04+2, 04+3, // Right face
            08+0, 08+2, 08+1,   08+0, 08+3, 08+2, // Bottom face
            12+0, 12+1, 12+2,   12+0, 12+2, 12+3, // Top face
            16+0, 16+2, 16+1,   16+0, 16+3, 16+2, // Back face
            20+0, 20+1, 20+2,   20+0, 20+2, 20+3, // Front face
        };

        return new MeshBuilder(vertices, colors, normals, triangles);
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
            colors32 = Colors.ToArray(),
            triangles = Triangles.ToArray(),
            normals = Normals.ToArray(),
        };

        return mesh;
    }
}

public class CellWallBuilder : MeshBuilder
{
    private readonly Vector3 _segmentSize;

    public Color32 MeshColor { get; set; }

    float _x1, _x2, _y1, _y2, _z1, _z2;

    public CellWallBuilder(Vector3 size, Vector3Int subDivs)
    {
        _segmentSize = new(size.x / subDivs.x, size.y / subDivs.y, size.z / subDivs.z);
        MeshColor = Color.magenta;
    }

    public void MoveTo(int x, int y, int z)
    {
        _x1 = x * _segmentSize.x;
        _x2 = _x1 + _segmentSize.x;
        _y1 = y * _segmentSize.y;
        _y2 = _y1 + _segmentSize.y;
        _z1 = z * _segmentSize.z;
        _z2 = _z1 + _segmentSize.z;
    }

    /// <summary>
    /// A.K.A. the left face
    /// </summary>
    public void AddFaceXM1()
    {
        int v0 = Vertices.Count;
        Vertices.AddRange(new Vector3[] { new(_x1, _y1, _z1), new(_x1, _y2, _z1), new(_x1, _y2, _z2), new(_x1, _y1, _z2) });
        Colors.AddRange(new Color32[] { MeshColor, MeshColor, MeshColor, MeshColor });
        Normals.AddRange(new Vector3[] { new(-1, 0, 0), new(-1, 0, 0), new(-1, 0, 0), new(-1, 0, 0) });
        Triangles.AddRange(new int[] { v0 + 0, v0 + 2, v0 + 1, v0 + 0, v0 + 3, v0 + 2 });
    }

    /// <summary>
    /// A.K.A. the right face
    /// </summary>
    public void AddFaceXP1()
    {
        int v0 = Vertices.Count;
        Vertices.AddRange(new Vector3[] { new(_x2, _y1, _z1), new(_x2, _y2, _z1), new(_x2, _y2, _z2), new(_x2, _y1, _z2) });
        Colors.AddRange(new Color32[] { MeshColor, MeshColor, MeshColor, MeshColor });
        Normals.AddRange(new Vector3[] { new(1, 0, 0), new(1, 0, 0), new(1, 0, 0), new(1, 0, 0) });
        Triangles.AddRange(new int[] { v0 + 0, v0 + 1, v0 + 2, v0 + 0, v0 + 2, v0 + 3 });
    }

    /// <summary>
    /// A.K.A. the bottom face
    /// </summary>
    public void AddFaceYM1()
    {
        int v0 = Vertices.Count;
        Vertices.AddRange(new Vector3[] { new(_x1, _y1, _z1), new(_x1, _y1, _z2), new(_x2, _y1, _z2), new(_x2, _y1, _z1) });
        Colors.AddRange(new Color32[] { MeshColor, MeshColor, MeshColor, MeshColor });
        Normals.AddRange(new Vector3[] { new(0, -1, 0), new(0, -1, 0), new(0, -1, 0), new(0, -1, 0) });
        Triangles.AddRange(new int[] { v0 + 0, v0 + 2, v0 + 1, v0 + 0, v0 + 3, v0 + 2 });
    }

    /// <summary>
    /// A.K.A. the top face
    /// </summary>
    public void AddFaceYP1()
    {
        int v0 = Vertices.Count;
        Vertices.AddRange(new Vector3[] { new(_x1, _y2, _z1), new(_x1, _y2, _z2), new(_x2, _y2, _z2), new(_x2, _y2, _z1) });
        Colors.AddRange(new Color32[] { MeshColor, MeshColor, MeshColor, MeshColor });
        Normals.AddRange(new Vector3[] { new(0, 1, 0), new(0, 1, 0), new(0, 1, 0), new(0, 1, 0) });
        Triangles.AddRange(new int[] { v0 + 0, v0 + 1, v0 + 2, v0 + 0, v0 + 2, v0 + 3 });
    }

    /// <summary>
    /// A.K.A. the back face
    /// </summary>
    public void AddFaceZM1()
    {
        int v0 = Vertices.Count;
        Vertices.AddRange(new Vector3[] { new(_x1, _y1, _z1), new(_x2, _y1, _z1), new(_x2, _y2, _z1), new(_x1, _y2, _z1) });
        Colors.AddRange(new Color32[] { MeshColor, MeshColor, MeshColor, MeshColor });
        Normals.AddRange(new Vector3[] { new(0, 0, -1), new(0, 0, -1), new(0, 0, -1), new(0, 0, -1) });
        Triangles.AddRange(new int[] { v0 + 0, v0 + 2, v0 + 1, v0 + 0, v0 + 3, v0 + 2 });
    }

    /// <summary>
    /// A.K.A. the front face
    /// </summary>
    public void AddFaceZP1()
    {
        int v0 = Vertices.Count;
        Vertices.AddRange(new Vector3[] { new(_x1, _y1, _z2), new(_x2, _y1, _z2), new(_x2, _y2, _z2), new(_x1, _y2, _z2) });
        Colors.AddRange(new Color32[] { MeshColor, MeshColor, MeshColor, MeshColor });
        Normals.AddRange(new Vector3[] { new(0, 0, 1), new(0, 0, 1), new(0, 0, 1), new(0, 0, 1) });
        Triangles.AddRange(new int[] { v0 + 0, v0 + 1, v0 + 2, v0 + 0, v0 + 2, v0 + 3 });
    }
}
