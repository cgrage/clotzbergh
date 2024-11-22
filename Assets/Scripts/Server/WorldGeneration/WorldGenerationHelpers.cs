using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public enum GeneralVoxelType
    {
        Air, Ground, AirOrGround
    }

    public static class WorldGenDefs
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

        public static readonly KlotzType[] AllGroundTypesSortedByVolumeDesc = SortByVolumeDesc(AllGroundTypes);
        public static readonly KlotzType[] All1x1x1Types = { KlotzType.Air, KlotzType.Plate1x1 };

        public static readonly KlotzTypeSet64 AirSet = new(new KlotzType[] { KlotzType.Air });
        public static readonly KlotzTypeSet64 AllGroundTypesSet = new(AllGroundTypes);
        public static readonly KlotzTypeSet64 All1x1x1TypesSet = new(All1x1x1Types);

        private static KlotzType[] SortByVolumeDesc(IEnumerable<KlotzType> types)
        {
            List<KlotzType> list = new(types);
            list.Sort((a, b) =>
            {
                Vector3Int sa = KlotzKB.KlotzSize(a);
                Vector3Int sb = KlotzKB.KlotzSize(b);
                return (sb.x * sb.y * sb.z).CompareTo(sa.x * sa.y * sa.z);
            });
            return list.ToArray();
        }
    }

    public readonly struct KlotzTypeSet64 : IEnumerable<KlotzType>
    {
        private readonly ulong _value;

        public static readonly KlotzTypeSet64 Empty = new();

        public KlotzTypeSet64(ulong value) { _value = value; }

        public KlotzTypeSet64(IEnumerable<KlotzType> types)
        {
            _value = 0;
            foreach (var type in types)
            {
                _value |= 1UL << (int)type;
            }
        }

        public KlotzTypeSet64 Merge(KlotzTypeSet64 other)
        {
            return new KlotzTypeSet64(_value | other._value);
        }

        public bool Contains(KlotzType type)
        {
            return (_value & 1UL << (int)type) != 0;
        }

        public KlotzTypeSet64 Remove(KlotzType type)
        {
            return new(_value & ~(1UL << (int)type));
        }

        public bool ContainsOnly(KlotzTypeSet64 other)
        {
            return (_value & ~other._value) == 0;
        }

        public int Count
        {
            get { return CountSetBits(_value); }
        }

        private static int CountSetBits(ulong bitField)
        {
            int count = 0;
            while (bitField != 0)
            {
                bitField &= (bitField - 1); // Clear the least significant bit set
                count++;
            }
            return count;
        }

        private static IEnumerable<int> GetSetBitPositions(ulong bitField)
        {
            int position = 0;
            while (bitField != 0)
            {
                if ((bitField & 1) != 0)
                {
                    yield return position;
                }
                bitField >>= 1;
                position++;
            }
        }

        // Explicit cast to int
        public static explicit operator ulong(KlotzTypeSet64 variant) { return variant._value; }

        // Explicit cast from uint
        public static explicit operator KlotzTypeSet64(ulong value) { return new KlotzTypeSet64(value); }

        IEnumerator<KlotzType> IEnumerable<KlotzType>.GetEnumerator()
        {
            foreach (int bitPosition in GetSetBitPositions(_value))
            {
                yield return (KlotzType)bitPosition;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KlotzType>)this).GetEnumerator();
        }
    }
}
