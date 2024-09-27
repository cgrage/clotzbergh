using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator2
{
    // AnimationCurve heightCurve = new(new Keyframe[] { new(0, 0, 0, 0, 0, 0), new(1, 1, 2, 2, 0, 0) });
    // float heightMultiplier = 20f;

    public MeshGenerator2()
    {
        // 
    }

    public MeshBuilder GenerateTerrainMesh(WorldChunk worldChunk, int lod)
    {
        const float w = WorldChunk.ChunkWidth - 1;
        const float h = WorldChunk.ChunkHeight - 1;
        const float d = WorldChunk.ChunkDepth - 1;

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

        return new MeshBuilder(vertices, triangles);
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
    //public Vector2[] uvs;

    //int triangleIndex;

    public MeshBuilder(Vector3[] vertices, int[] triangles)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        // uvs = new Vector2[meshWidth * meshHeight];
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
            //uv = uvs,
        };

        mesh.RecalculateNormals();
        return mesh;
    }
}
