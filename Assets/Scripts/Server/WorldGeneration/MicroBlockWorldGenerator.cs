using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public class MicroBlockWorldGenerator : ChunkGenerator
    {
        public override WorldChunk InnerGenerate()
        {
            WorldChunk chunk = new();

            for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
            {
                for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
                {
                    int x = _chunkCoords.x * WorldDef.ChunkSubDivsX + ix;
                    int z = _chunkCoords.z * WorldDef.ChunkSubDivsZ + iz;
                    int groundStart = Mathf.RoundToInt(_heightMap.At(x, z) / WorldDef.SubKlotzSize.y);

                    for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                    {
                        int y = _chunkCoords.y * WorldDef.ChunkSubDivsY + iy;
                        if (y > groundStart)
                        {
                            chunk.Set(ix, iy, iz, SubKlotz.Air);
                        }
                        else
                        {
                            chunk.Set(ix, iy, iz, SubKlotz.Root(
                                KlotzType.Plate1x1,
                                ColorFromHeight(y),
                                NextRandVariant(),
                                KlotzDirection.ToPosX));
                        }
                    }
                }
            }

            // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 16), KlotzDirection.ToPosX);
            // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 18), KlotzDirection.ToPosX);
            // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 16), KlotzDirection.ToPosZ);
            // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(13, 39, 16), KlotzDirection.ToPosZ);
            // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 15), KlotzDirection.ToNegX);
            // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 13), KlotzDirection.ToNegX);
            // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 15), KlotzDirection.ToNegZ);
            // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(18, 39, 15), KlotzDirection.ToNegZ);

            return chunk;
        }
    }
}
