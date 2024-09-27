using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator2
{
    public MeshGenerator2()
    {
        // 
    }

    public MeshBuilder GenerateTerrainMesh(WorldChunk worldChunk, int lod)
    {
        return GenerateCuboid(WorldChunk.Size);
    }

    private MeshBuilder GenerateCuboid(Vector3 size)
    {
        float w = size.x, h = size.y, d = size.z;
        Vector3[] vertices = {
            new(0, 0, 0), new(w, 0, 0), new(w, h, 0), new(0, h, 0), // Front face
            new(0, 0, d), new(w, 0, d), new(w, h, d), new(0, h, d)  // Back face
        };

        int[] triangles = {
            0, 2, 1,   0, 3, 2,   // Front face
            4, 5, 6,   4, 6, 7,   // Back face
            0, 7, 3,   0, 4, 7,   // Left face
            1, 2, 6,   1, 6, 5,   // Right face
            2, 3, 7,   2, 7, 6,   // Top face
            0, 1, 5,   0, 5, 4    // Bottom face
        };


        Vector2[] uvs = {
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Front face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Back face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Left face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Right face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1), // Top face
            new(0, 0), new(1, 0), new(1, 1), new(0, 1)  // Bottom face
        };

        return new MeshBuilder(vertices, triangles, uvs);
    }


    /*
    WorldChunk.ChunkSize;

    int width = heightMap.GetLength(0);
    int height = heightMap.GetLength(1);
    float topLeftX = (width - 1) / -2f;
    float topLeftZ = (height - 1) / 2f;

    int meshIncrement = lod <= 0 ? 1 : lod * 2;
    int verticesPerLine = (width - 1) / meshIncrement + 1;

    MeshBuilder meshData = new MeshBuilder(verticesPerLine, verticesPerLine);
    int vertexIndex = 0;

    for (int y = 0; y < height; y += meshIncrement)
    {
        for (int x = 0; x < width; x += meshIncrement)
        {
            meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, newHeighCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
            meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

            if (x < width - 1 && y < height - 1)
            {
                meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
            }

            vertexIndex++;
        }
    }

    return meshData;
    */
}

public class MeshBuilder
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    //int triangleIndex;

    public MeshBuilder(Vector3[] vertices, int[] triangles, Vector2[] uvs)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs;
    }



    /*
        public MeshBuilder(int meshWidth, int meshHeight)
        {
            vertices = new Vector3[meshWidth * meshHeight];
            triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 2 * 3];
            // uvs = new Vector2[meshWidth * meshHeight];
        }*/

    /*
        public void AddTriangle(int a, int b, int c)
        {
            triangles[triangleIndex++] = a;
            triangles[triangleIndex++] = b;
            triangles[triangleIndex++] = c;
        }
        */

    public Mesh ToMesh()
    {
        Mesh mesh = new()
        {
            vertices = vertices,
            triangles = triangles,
            uv = uvs,
        };

        mesh.RecalculateNormals();
        return mesh;
    }
}
