using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public class WG02_MicroBlockWorldGenerator : VoxelChunkGenerator
    {
        public WG02_MicroBlockWorldGenerator() : base(false) { }

        protected override WorldChunk InnerGenerate()
        {
            PlaceGround();
            FillNonCompletedWith1x1Plates();

            // PlaceKlotz(new Vector3Int(16, 39, 16), KlotzType.Brick2x4, KlotzDirection.ToPosX);
            // PlaceKlotz(new Vector3Int(16, 39, 18), KlotzType.Brick2x4, KlotzDirection.ToPosX);
            // PlaceKlotz(new Vector3Int(15, 39, 16), KlotzType.Brick2x4, KlotzDirection.ToPosZ);
            // PlaceKlotz(new Vector3Int(13, 39, 16), KlotzType.Brick2x4, KlotzDirection.ToPosZ);
            // PlaceKlotz(new Vector3Int(15, 39, 15), KlotzType.Brick2x4, KlotzDirection.ToNegX);
            // PlaceKlotz(new Vector3Int(15, 39, 13), KlotzType.Brick2x4, KlotzDirection.ToNegX);
            // PlaceKlotz(new Vector3Int(16, 39, 15), KlotzType.Brick2x4, KlotzDirection.ToNegZ);
            // PlaceKlotz(new Vector3Int(18, 39, 15), KlotzType.Brick2x4, KlotzDirection.ToNegZ);

            return ToWorldChunk();
        }
    }
}
