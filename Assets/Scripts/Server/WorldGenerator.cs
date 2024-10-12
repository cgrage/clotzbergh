using UnityEngine;

public class WorldGenerator
{
    private readonly HeightMap _heightMap = new();

    public WorldChunk GetChunk(Vector3Int chunkCoords)
    {
        var chunk = WorldChunk.CreateEmpty();

        for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
        {
            for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
            {
                int x = chunkCoords.x * WorldDef.ChunkSubDivsX + ix;
                int z = chunkCoords.z * WorldDef.ChunkSubDivsZ + iz;
                float height = _heightMap.At(x, z);

                for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                {
                    int y = chunkCoords.y * WorldDef.ChunkSubDivsY + iy;
                    float scaledY = y * WorldDef.SubKlotzSize.y;

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

        PlaceBrick4x2(chunk, 14, 39, 15, KlotzDirection.ToPosX);

        return chunk;
    }

    private static void PlaceBrick4x2(WorldChunk chunk, int x, int y, int z, KlotzDirection dir)
    {
        for (int subZ = 0; subZ < 2; subZ++)
        {
            for (int subX = 0; subX < 4; subX++)
            {
                for (int subY = 0; subY < 3; subY++)
                {
                    chunk.Set(x + subX, y + subY, z + subZ,
                        new SubKlotz(KlotzType.Brick4x2, dir, subX, subY, subZ));
                }
            }
        }
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
                    x * WorldDef.SubKlotzSize.x,
                    _heightMap.At(x, y),
                    y * WorldDef.SubKlotzSize.z);
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
