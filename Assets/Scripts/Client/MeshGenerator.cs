using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates the meshes. When called on <c>GenerateTerrainMesh</c> it generates 
/// the mesh for a <c>TerrainChunk</c> and its inner <c>WorldChunk</c>.
/// Uses the neighbors of the <c>TerrainChunk</c> to find adjacent world 
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
    public MeshBuilder GenerateTerrainMesh(TerrainChunk terrainChunk, int lod)
    {
        WorldChunk worldChunk = terrainChunk.World;
        if (worldChunk == null)
            return null;

        CellWallBuilder builder = new(WorldChunk.Size, WorldChunk.KlotzCount);
        WorldStitcher stitcher = new(terrainChunk);

        builder.SetColor(ColorFromHash(terrainChunk.Id.GetHashCode()));

        for (int z = 0; z < WorldChunk.KlotzCountZ; z++)
        {
            for (int y = 0; y < WorldChunk.KlotzCountY; y++)
            {
                for (int x = 0; x < WorldChunk.KlotzCountX; x++)
                {
                    bool clear = worldChunk.Get(x, y, z).IsClear;
                    if (clear)
                        continue; // later this will be more complex, I guess.

                    builder.MoveTo(x, y, z);
                    if (stitcher.IsKnownClearAt(x - 1, y, z)) builder.AddFaceXM1();
                    if (stitcher.IsKnownClearAt(x + 1, y, z)) builder.AddFaceXP1();
                    if (stitcher.IsKnownClearAt(x, y - 1, z)) builder.AddFaceYM1();
                    if (stitcher.IsKnownClearAt(x, y + 1, z)) builder.AddFaceYP1();
                    if (stitcher.IsKnownClearAt(x, y, z - 1)) builder.AddFaceZM1();
                    if (stitcher.IsKnownClearAt(x, y, z + 1)) builder.AddFaceZP1();
                }
            }
        }

        return builder;
    }

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

    public WorldStitcher(TerrainChunk chunk)
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

    public SubKlotz? At(int x, int y, int z)
    {
        if (x < 0) { return _neighborXM1?.Get(x + MAX_X, y, z); }
        else if (x >= MAX_X) { return _neighborXP1?.Get(x - MAX_X, y, z); }
        else if (y < 0) { return _neighborYM1?.Get(x, y + MAX_Y, z); }
        else if (y >= MAX_Y) { return _neighborYP1?.Get(x, y - MAX_Y, z); }
        else if (z < 0) { return _neighborZM1?.Get(x, y, z + MAX_Z); }
        else if (z >= MAX_Z) { return _neighborZP1?.Get(x, y, z - MAX_Z); }
        else { return _worldChunk.Get(x, y, z); }
    }

    public bool IsKnownClearAt(int x, int y, int z)
    {
        SubKlotz? subKlotz = At(x, y, z);
        if (!subKlotz.HasValue)
            return false;

        return subKlotz.Value.IsClear;
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

    private Color32 _color;

    private float _x1, _x2, _y1, _y2, _z1, _z2;

    private readonly System.Random _todoRemove = new();

    public CellWallBuilder(Vector3 size, Vector3Int subDivs)
    {
        _segmentSize = new(size.x / subDivs.x, size.y / subDivs.y, size.z / subDivs.z);
        _color = Color.magenta;
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

    public void SetColor(Color32 color)
    {
        _color = color;
    }

    /// <summary>
    /// A.K.A. the left face
    /// </summary>
    public void AddFaceXM1()
    {
        AddFace(new(_x1, _y1, _z1), new(_x1, _y2, _z1), new(_x1, _y2, _z2), new(_x1, _y1, _z2),
            new(-1, 0, 0), false);
    }

    /// <summary>
    /// A.K.A. the right face
    /// </summary>
    public void AddFaceXP1()
    {
        AddFace(new(_x2, _y1, _z1), new(_x2, _y2, _z1), new(_x2, _y2, _z2), new(_x2, _y1, _z2),
            new(1, 0, 0), true);
    }

    /// <summary>
    /// A.K.A. the bottom face
    /// </summary>
    public void AddFaceYM1()
    {
        AddFace(new(_x1, _y1, _z1), new(_x1, _y1, _z2), new(_x2, _y1, _z2), new(_x2, _y1, _z1),
            new(0, -1, 0), false);
    }

    /// <summary>
    /// A.K.A. the top face
    /// </summary>
    public void AddFaceYP1()
    {
        AddFace(new(_x1, _y2, _z1), new(_x1, _y2, _z2), new(_x2, _y2, _z2), new(_x2, _y2, _z1),
            new(0, 1, 0), true);
    }

    /// <summary>
    /// A.K.A. the back face
    /// </summary>
    public void AddFaceZM1()
    {
        AddFace(new(_x1, _y1, _z1), new(_x2, _y1, _z1), new(_x2, _y2, _z1), new(_x1, _y2, _z1),
            new(0, 0, -1), false);
    }

    /// <summary>
    /// A.K.A. the front face
    /// </summary>
    public void AddFaceZP1()
    {
        AddFace(new(_x1, _y1, _z2), new(_x2, _y1, _z2), new(_x2, _y2, _z2), new(_x1, _y2, _z2),
            new(0, 0, 1), true);
    }

    /// <summary>
    /// A.K.A. the front face
    /// </summary>
    private void AddFace(Vector3 corner1, Vector3 corner2, Vector3 corner3, Vector3 corner4, Vector3 normal, bool clockwise)
    {
        Color32 color = AdjustColorBrightness(_color, 0.2f);
        int v0 = Vertices.Count;

        Vertices.AddRange(new Vector3[] { corner1, corner2, corner3, corner4 });
        Colors.AddRange(new Color32[] { color, color, color, color });
        Normals.AddRange(new Vector3[] { normal, normal, normal, normal });
        if (clockwise) Triangles.AddRange(new int[] { v0 + 0, v0 + 1, v0 + 2, v0 + 0, v0 + 2, v0 + 3 });
        else Triangles.AddRange(new int[] { v0 + 0, v0 + 2, v0 + 1, v0 + 0, v0 + 3, v0 + 2 });
    }

    public Color32 AdjustColorBrightness(Color32 color, float adjustmentRange = 0.1f)
    {
        // Generate random adjustment factor
        float adjustmentFactor = (float)(_todoRemove.NextDouble() * 2 - 1) * adjustmentRange;

        // Adjust color values
        byte r = (byte)Mathf.Clamp(color.r + (color.r * adjustmentFactor), 0, 255);
        byte g = (byte)Mathf.Clamp(color.g + (color.g * adjustmentFactor), 0, 255);
        byte b = (byte)Mathf.Clamp(color.b + (color.b * adjustmentFactor), 0, 255);

        return new Color32(r, g, b, color.a);
    }
}
