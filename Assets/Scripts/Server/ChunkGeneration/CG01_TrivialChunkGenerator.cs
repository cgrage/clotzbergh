using UnityEngine;

namespace Clotzbergh.Server.ChunkGeneration
{
    public class CG01_TrivialChunkGenerator : ChunkGenerator
    {
        protected override WorldChunk InnerGenerate()
        {
            WorldChunk chunk = new();

            for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
            {
                for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
                {
                    int groundStart = FieldResolver.GroundStartAtRelPos(ix, iz);

                    for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                    {
                        int y = ChunkCoords.Y * WorldDef.ChunkSubDivsY + iy;
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
