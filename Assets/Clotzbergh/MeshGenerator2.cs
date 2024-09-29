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
                    bool bt = worldChunk.Get(x, y, z).IsSeeThrough;
                    bool btx1 = worldChunk.Get(x + 1, y, z).IsSeeThrough;
                    bool bty1 = worldChunk.Get(x, y + 1, z).IsSeeThrough;
                    bool btz1 = worldChunk.Get(x, y, z + 1).IsSeeThrough;

                    builder.AddWalls(new Vector3Int(x, y, z),
                        bt != btx1, bt != bty1, bt != btz1, bt);
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
            new(x1, y1, z1), new(x1, y2, z1), new(x1, y2, z2), new(x1, y1, z2), // Left face
            new(x2, y1, z1), new(x2, y2, z1), new(x2, y2, z2), new(x2, y1, z2), // Right face
            new(x1, y1, z1), new(x1, y1, z2), new(x2, y1, z2), new(x2, y1, z1), // Bottom face
            new(x1, y2, z1), new(x1, y2, z2), new(x2, y2, z2), new(x2, y2, z1), // Top face
            new(x1, y1, z1), new(x2, y1, z1), new(x2, y2, z1), new(x1, y2, z1), // Back face
            new(x1, y1, z2), new(x2, y1, z2), new(x2, y2, z2), new(x1, y2, z2), // Front face
        };

        Vector2[] uvs = {
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Left face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Right face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Bottom face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Top face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Back face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Front face
        };

        int[] triangles = {
            00+0, 00+2, 00+1,   00+0, 00+3, 00+2, // Left face
            04+0, 04+1, 04+2,   04+0, 04+2, 04+3, // Right face
            08+0, 08+2, 08+1,   08+0, 08+3, 08+2, // Bottom face
            12+0, 12+1, 12+2,   12+0, 12+2, 12+3, // Top face
            16+0, 16+2, 16+1,   16+0, 16+3, 16+2, // Back face
            20+0, 20+1, 20+2,   20+0, 20+2, 20+3, // Front face
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
    /// Unity uses a left   +----+
    /// handed coordinate  /    /|
    /// system            +----+ |
    ///     +----+----+   | Y2 | +     +----+
    ///    /    /    /|   |    |/|    / Z1 /|
    ///   +----+----+ |   +----+ |   +----+ |
    ///   | X1 | X2 | +   | Y1 | +  /    /| +
    ///   |    |    |/    |    |/  +----+ |/
    ///   +----+----+     +----+   | Z2 | +
    ///                            |    |/
    ///                            +----+
    /// </summary>
    public void AddWalls(Vector3Int cell, bool addWallXP1, bool addWallYP1, bool addWallZP1, bool wallDir)
    {
        float x1 = cell.x * _segm.x;
        float x2 = x1 + _segm.x;
        float y1 = cell.y * _segm.y;
        float y2 = y1 + _segm.y;
        float z1 = cell.z * _segm.z;
        float z2 = z1 + _segm.z;

        if (addWallXP1)
            AddWall(new(x2, y1, z1), new(x2, y2, z1), new(x2, y2, z2), new(x2, y1, z2), wallDir); // Right face

        if (addWallYP1)
            AddWall(new(x1, y2, z1), new(x1, y2, z2), new(x2, y2, z2), new(x2, y2, z1), wallDir); // Top face

        if (addWallZP1)
            AddWall(new(x1, y1, z2), new(x2, y1, z2), new(x2, y2, z2), new(x1, y2, z2), wallDir); // Front face
    }

    private void AddWall(Vector3 corner1, Vector3 corner2, Vector3 corner3, Vector3 corner4, bool direction)
    {
        int v0 = Vertices.Count;
        Vertices.AddRange(new Vector3[] { corner1, corner2, corner3, corner4 });
        UVs.AddRange(new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) });
        if (direction) Triangles.AddRange(new int[] { v0 + 0, v0 + 2, v0 + 1, v0 + 0, v0 + 3, v0 + 2 });
        else Triangles.AddRange(new int[] { v0 + 0, v0 + 1, v0 + 2, v0 + 0, v0 + 2, v0 + 3 });
    }
}
