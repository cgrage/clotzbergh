using UnityEngine;

public class WorldGenerator
{
    private readonly HeightMap _heightMap = new();

    public WorldChunk GetChunk(Vector3Int chunkCoords)
    {
        // return WorldChunk.CreateCoreFilled(KlotzType.Plate1x1);
        return WorldChunk.CreateFloodFilled(KlotzType.Plate1x1);

        var chunk = WorldChunk.CreateEmpty();

        for (int iz = 0; iz < WorldChunk.KlotzCountRawZ; iz++)
        {
            for (int ix = 0; ix < WorldChunk.KlotzCountRawX; ix++)
            {
                int x = chunkCoords.x * WorldChunk.KlotzCountX - WorldChunk.BorderSize + ix;
                int z = chunkCoords.z * WorldChunk.KlotzCountZ - WorldChunk.BorderSize + iz;
                float height = _heightMap.At(x, z);

                for (int iy = 0; iy < WorldChunk.KlotzCountRawY; iy++)
                {
                    int y = chunkCoords.y * WorldChunk.KlotzCountY - WorldChunk.BorderSize + iy;
                    float scaledY = y * Klotz.Size.y;

                    Debug.Log($"height at ({x}, {z}) = {height}");

                    if (scaledY > height)
                    {
                        chunk.SetRaw(ix, iy, iz, new Klotz(KlotzType.Plate1x1));
                    }
                    else
                    {
                        chunk.SetRaw(ix, iy, iz, new Klotz(KlotzType.Plate1x1));
                    }
                }
            }
        }

        return chunk;
    }

    public Mesh GeneratePreviewMesh(int dist)
    {
        int size = 2 * dist;

        Vector3[] vertices = new Vector3[size * size];
        int[] triangles = new int[(size - 1) * (size - 1) * 6];

        int vIndex = 0;
        for (int y = -dist; y < dist; y++)
        {
            for (int x = -dist; x < dist; x++)
            {
                vertices[vIndex++] = new Vector3(
                    x * Klotz.Size.x,
                    _heightMap.At(x, y),
                    y * Klotz.Size.z);
            }
        }

        int triIndex = 0;
        for (int iy = 0; iy < size - 1; iy++)
        {
            for (int ix = 0; ix < size - 1; ix++)
            {
                int current = ix + iy * size;

                triangles[triIndex++] = current;
                triangles[triIndex++] = current + size;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = current + size;
                triangles[triIndex++] = current + size + 1;
            }
        }

        Mesh mesh = new()
        {
            vertices = vertices,
            triangles = triangles
        };

        mesh.RecalculateNormals();
        return mesh;
    }
}
