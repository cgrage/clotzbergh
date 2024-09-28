using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator2
{
    public MeshGenerator2()
    {
        // 
    }

    /// <summary>
    /// 
    /// </summary>
    public MeshBuilder GenerateTerrainMesh(WorldChunk worldChunk, int lod)
    {
        CellWallBuilder builder = new(WorldChunk.Size, WorldChunk.KlotzCount);

        for (int z = 0; z < WorldChunk.KlotzCountZ; z++)
        {
            for (int y = 0; y < WorldChunk.KlotzCountY; y++)
            {
                for (int x = -1; x < WorldChunk.KlotzCountX; x++)
                {
                    bool b1t = worldChunk.Get(x + 0, y, z).IsSeeThrough;
                    bool b2t = worldChunk.Get(x + 1, y, z).IsSeeThrough;
                    if (b1t != b2t) { builder.AddWallX(x, x + 1, y, z, b1t); }
                }
            }
        }

        return builder;
        // return MeshBuilder.FromCuboid(Vector3.zero, WorldChunk.Size);
    }
}

public class MeshBuilder
{
    public List<Vector3> Vertices { get; private set; }
    public List<Vector2> UVs { get; private set; }
    public List<int> Triangles { get; private set; }

    public MeshBuilder(int estimatedVertexCount = 0, int estimatedTriangleCount = 0)
    {
        Vertices = new(estimatedVertexCount);
        UVs = new(estimatedVertexCount);
        Triangles = new List<int>(capacity: estimatedTriangleCount * 3);
    }

    public MeshBuilder(Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        Vertices = new(vertices);
        UVs = new(uvs);
        Triangles = new(triangles);
    }

    public static MeshBuilder FromCuboid(Vector3 pos, Vector3 size)
    {
        float x1 = pos.x, y1 = pos.y, z1 = pos.z;
        float x2 = x1 + size.x, y2 = y1 + size.y, z2 = z1 + size.z;

        Vector3[] vertices = {
            new(x1, y1, z1), new(x2, y1, z1), new(x2, y2, z1), new(x1, y2, z1), // Front face
            new(x1, y1, z2), new(x2, y1, z2), new(x2, y2, z2), new(x1, y2, z2), // Back face
            new(x1, y1, z1), new(x1, y2, z1), new(x1, y2, z2), new(x1, y1, z2), // Left face
            new(x2, y1, z1), new(x2, y2, z1), new(x2, y2, z2), new(x2, y1, z2), // Right face
            new(x1, y2, z1), new(x2, y2, z1), new(x2, y2, z2), new(x1, y2, z2), // Top face
            new(x1, y1, z1), new(x2, y1, z1), new(x2, y1, z2), new(x1, y1, z2)  // Bottom face
        };

        Vector2[] uvs = {
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Front face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Back face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Left face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Right face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Top face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1)  // Bottom face
        };

        int[] triangles = {
            00, 02, 01,   00, 03, 02, // Front face
            04, 05, 06,   04, 06, 07, // Back face
            08, 10, 09,   08, 11, 10, // Left face
            12, 13, 14,   12, 14, 15, // Right face
            16, 18, 17,   16, 19, 18, // Top face
            20, 21, 22,   20, 22, 23  // Bottom face
        };

        return new MeshBuilder(vertices, uvs, triangles);
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
            uv = UVs.ToArray(),
            triangles = Triangles.ToArray(),
        };

        mesh.RecalculateNormals();
        return mesh;
    }
}

public class CellWallBuilder : MeshBuilder
{
    private readonly Vector3 _size;

    private readonly Vector3Int _subDivs;

    private readonly Vector3 _segm;

    public CellWallBuilder(Vector3 size, Vector3Int subDivs)
    {
        _size = size;
        _subDivs = subDivs;
        _segm = new(size.x / subDivs.x, size.y / subDivs.y, size.z / subDivs.z);
    }

    /// <summary>
    ///     +----X----+
    ///    / 1  / 2  /|
    ///   +----X----+ |   (-> dirX1toX2)
    ///   |    |    | +  
    ///   |    |    |/
    ///   +----X----+
    /// </summary>

    public void AddWallX(int cellX1, int cellX2, int cellY, int cellZ, bool dirX1toX2)
    {
        float x2 = cellX2 * _segm.x;
        float y1 = cellY * _segm.y;
        float y2 = y1 + _segm.y;
        float z1 = cellZ * _segm.z;
        float z2 = z1 + _segm.z;

        int v0 = Vertices.Count;
        Vertices.AddRange(new Vector3[] { new(x2, y1, z1), new(x2, y2, z1), new(x2, y2, z2), new(x2, y1, z2) }); // Right face
        UVs.AddRange(new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) }); // Right face
        if (dirX1toX2) Triangles.AddRange(new int[] { v0 + 0, v0 + 2, v0 + 1, v0 + 0, v0 + 3, v0 + 2 });
        else Triangles.AddRange(new int[] { v0 + 0, v0 + 1, v0 + 2, v0 + 0, v0 + 2, v0 + 3 });
    }
}
