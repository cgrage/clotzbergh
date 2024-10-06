using UnityEngine;

public class WorldGenerator
{
    private readonly HeightMap _heightMap = new();

    public WorldChunk GetChunk(Vector3Int chunkCoords)
    {
        var chunk = WorldChunk.CreateEmpty();

        for (int iz = 0; iz < WorldChunk.KlotzCountZ; iz++)
        {
            for (int ix = 0; ix < WorldChunk.KlotzCountX; ix++)
            {
                int x = chunkCoords.x * WorldChunk.KlotzCountX + ix;
                int z = chunkCoords.z * WorldChunk.KlotzCountZ + iz;
                float height = _heightMap.At(x, z);

                for (int iy = 0; iy < WorldChunk.KlotzCountY; iy++)
                {
                    int y = chunkCoords.y * WorldChunk.KlotzCountY + iy;
                    float scaledY = y * SubKlotz.Size.y;

                    if (scaledY > height)
                    {
                        chunk.Set(ix, iy, iz, new SubKlotz(KlotzType.Air, KlotzDirection.ToPosX, 0, 0, 0));
                    }
                    else
                    {
                        chunk.Set(ix, iy, iz, new SubKlotz(KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0));
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
                    x * SubKlotz.Size.x,
                    _heightMap.At(x, y),
                    y * SubKlotz.Size.z);
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
