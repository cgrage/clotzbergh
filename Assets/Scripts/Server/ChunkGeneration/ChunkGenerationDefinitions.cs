using System.Collections.Generic;

namespace Clotzbergh.Server.ChunkGeneration
{
    public static class GroundDefinitions
    {
        public static readonly KlotzType[] AllGroundTypes = {
            KlotzType.Plate1x1, KlotzType.Plate1x2, KlotzType.Plate1x3, KlotzType.Plate1x4,
            KlotzType.Plate1x6, KlotzType.Plate1x8, KlotzType.Plate2x2, KlotzType.Plate2x3,
            KlotzType.Plate2x4, KlotzType.Plate2x6, KlotzType.Plate2x8, KlotzType.Plate4x4,
            KlotzType.Plate4x6, KlotzType.Plate4x8, KlotzType.Plate6x6, KlotzType.Plate6x8,
            KlotzType.Plate8x8,
            KlotzType.Brick1x1, KlotzType.Brick1x2, KlotzType.Brick1x3, KlotzType.Brick1x4,
            KlotzType.Brick1x6, KlotzType.Brick1x8, KlotzType.Brick2x2, KlotzType.Brick2x3,
            KlotzType.Brick2x4, KlotzType.Brick2x6, KlotzType.Brick2x8, KlotzType.Brick4x6 };

        public static readonly KlotzType[] NiceGroundTypes = {
            KlotzType.Plate1x1, KlotzType.Plate1x2, KlotzType.Plate1x3, KlotzType.Plate1x4,
            KlotzType.Plate2x2, KlotzType.Plate2x3, KlotzType.Plate2x4,
            KlotzType.Brick1x1, KlotzType.Brick1x2, KlotzType.Brick1x3, KlotzType.Brick1x4,
            KlotzType.Brick2x2, KlotzType.Brick2x3, KlotzType.Brick2x4, };

        public static readonly KlotzType[] AllGroundTypesSortedByVolumeDesc = SortByVolumeDesc(AllGroundTypes);
        public static readonly KlotzType[] NiceGroundTypesSortedByVolumeDesc = SortByVolumeDesc(NiceGroundTypes);

        public static readonly KlotzTypeSet64 AllGroundTypesSet = new(AllGroundTypes);
        public static readonly KlotzTypeSet64 NiceGroundTypesSet = new(NiceGroundTypes);

        private static KlotzType[] SortByVolumeDesc(IEnumerable<KlotzType> types)
        {
            List<KlotzType> list = new(types);
            list.Sort((a, b) =>
            {
                KlotzSize sa = KlotzKB.Size(a);
                KlotzSize sb = KlotzKB.Size(b);
                return (sb.X * sb.Y * sb.Z).CompareTo(sa.X * sa.Y * sa.Z);
            });
            return list.ToArray();
        }
    }
}
