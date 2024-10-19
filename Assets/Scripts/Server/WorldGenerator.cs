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
                        chunk.Set(ix, iy, iz, new SubKlotz(KlotzType.Air, KlotzColor.Green, KlotzDirection.ToPosX, 0, 0, 0));
                    }
                    else
                    {
                        chunk.Set(ix, iy, iz, new SubKlotz(KlotzType.Plate1x1, KlotzColor.Green, KlotzDirection.ToPosX, 0, 0, 0));
                    }
                }
            }
        }

        chunk.PlaceKlotz(KlotzType.Brick4x2, KlotzColor.Green, new Vector3Int(16, 39, 16), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick4x2, KlotzColor.Green, new Vector3Int(16, 39, 18), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick4x2, KlotzColor.Green, new Vector3Int(15, 39, 16), KlotzDirection.ToPosZ);
        chunk.PlaceKlotz(KlotzType.Brick4x2, KlotzColor.Green, new Vector3Int(13, 39, 16), KlotzDirection.ToPosZ);
        chunk.PlaceKlotz(KlotzType.Brick4x2, KlotzColor.Green, new Vector3Int(15, 39, 15), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick4x2, KlotzColor.Green, new Vector3Int(15, 39, 13), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick4x2, KlotzColor.Green, new Vector3Int(16, 39, 15), KlotzDirection.ToNegZ);
        chunk.PlaceKlotz(KlotzType.Brick4x2, KlotzColor.Green, new Vector3Int(18, 39, 15), KlotzDirection.ToNegZ);

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
