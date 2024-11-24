using Clotzbergh;
using Clotzbergh.Server;
using Clotzbergh.Server.WorldGeneration;
using UnityEngine;

public class WG01_TrivialWorldGenerator : IChunkGenerator
{
    public WorldChunk Generate(Vector3Int coords, IHeightMap heightMap)
    {
        WorldChunk chunk = new();

        for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
        {
            for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
            {
                int x = coords.x * WorldDef.ChunkSubDivsX + ix;
                int z = coords.z * WorldDef.ChunkSubDivsZ + iz;
                int groundStart = Mathf.RoundToInt(heightMap.At(x, z) / WorldDef.SubKlotzSize.y);

                for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                {
                    int y = coords.y * WorldDef.ChunkSubDivsY + iy;
                    if (y > groundStart)
                    {
                        chunk.Set(ix, iy, iz, SubKlotz.Air);
                    }
                    else
                    {
                        chunk.Set(ix, iy, iz, SubKlotz.Root(
                            KlotzType.Plate1x1,
                            KlotzColor.White,
                            KlotzVariant.Zero,
                            KlotzDirection.ToPosX));
                    }
                }
            }
        }

        return chunk;
    }
}