using UnityEngine;

namespace Clotzbergh.Server.ChunkGeneration
{
    public class CG01_TrivialChunkGenerator : IChunkGenerator
    {
        public WorldChunk Generate(ChunkCoords coords, IHeightMap heightMap, ColorFunction colorFunc)
        {
            WorldChunk chunk = new();

            for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
            {
                for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
                {
                    int x = coords.X * WorldDef.ChunkSubDivsX + ix;
                    int z = coords.Z * WorldDef.ChunkSubDivsZ + iz;
                    int groundStart = Mathf.RoundToInt(heightMap.At(x, z) / WorldDef.SubKlotzSize.y);

                    for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                    {
                        int y = coords.Y * WorldDef.ChunkSubDivsY + iy;
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
}
