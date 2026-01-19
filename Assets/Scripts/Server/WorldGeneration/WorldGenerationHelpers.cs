using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
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

    public readonly struct KlotzTypeSet64 : IEnumerable<KlotzType>
    {
        private readonly ulong _value;

        public static readonly KlotzTypeSet64 Empty = new();

        public static readonly KlotzTypeSet64 Air = new(new KlotzType[] { KlotzType.Air });

        public static readonly KlotzTypeSet64 All1x1x1Types = new(new KlotzType[] { KlotzType.Air, KlotzType.Plate1x1 });

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

        public KlotzTypeSet64 Add(KlotzType type)
        {
            return new(_value | 1UL << (int)type);
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

    public readonly struct HitCube8x3x8
    {
        private readonly ulong _xz0;
        private readonly ulong _xz1;
        private readonly ulong _xz2;

        public static readonly HitCube8x3x8 Empty = new();

        private HitCube8x3x8(ulong xz0, ulong xz1, ulong xz2)
        {
            _xz0 = xz0;
            _xz1 = xz1;
            _xz2 = xz2;
        }

        private static readonly Dictionary<KlotzType, HitCube8x3x8> TypeCache;

        static HitCube8x3x8()
        {
            TypeCache = new()
            {
                [KlotzType.Air] = new HitCube8x3x8()
            };

            foreach (KlotzType type in GroundDefinitions.AllGroundTypes)
            {
                TypeCache[type] = Draw(type);
            }
        }

        public static HitCube8x3x8 FromSet(KlotzTypeSet64 set)
        {
            HitCube8x3x8 sum = Empty;
            foreach (var type in set)
            {
                sum = sum.Combine(FromType(type));
            }
            return sum;
        }

        public static HitCube8x3x8 FromType(KlotzType type)
        {
            return TypeCache[type];
        }

        private static HitCube8x3x8 Draw(KlotzType type)
        {
            KlotzSize size = KlotzKB.Size(type);
            return new HitCube8x3x8(
                DrawKlotzLayer(size, 0, RelKlotzCoords.Zero),
                DrawKlotzLayer(size, 1, RelKlotzCoords.Zero),
                DrawKlotzLayer(size, 2, RelKlotzCoords.Zero));
        }

        private static ulong DrawKlotzLayer(KlotzSize klotzSize, int yLayer, RelKlotzCoords relPos)
        {
            int y = yLayer + relPos.Y;
            if (y < 0 || y >= klotzSize.Y)
                return 0;

            int xStart = Math.Clamp(0 - relPos.X, 0, 7);
            int zStart = Math.Clamp(0 - relPos.Z, 0, 7);
            int xEnd = Math.Clamp(klotzSize.X - relPos.X, 0, 8);
            int zEnd = Math.Clamp(klotzSize.Z - relPos.Z, 0, 8);

            ulong value = 0;
            for (int z = zStart; z < zEnd; z++)
            {
                for (int x = xStart; x < xEnd; x++)
                {
                    value |= 1UL << (z * 8 + x);
                }
            }
            return value;
        }

        public static HitCube8x3x8 Draw(KlotzType type, RelKlotzCoords relPos)
        {
            KlotzSize size = KlotzKB.Size(type);
            return new HitCube8x3x8(
                DrawKlotzLayer(size, 0, relPos),
                DrawKlotzLayer(size, 1, relPos),
                DrawKlotzLayer(size, 2, relPos));
        }

        public HitCube8x3x8 Combine(HitCube8x3x8 other)
        {
            return new HitCube8x3x8(
                _xz0 | other._xz0,
                _xz1 | other._xz1,
                _xz2 | other._xz2);
        }

        public bool Hits(HitCube8x3x8 other)
        {
            return
                (_xz0 & other._xz0) > 0 ||
                (_xz1 & other._xz1) > 0 ||
                (_xz2 & other._xz2) > 0;
        }

        private static string FieldRowString(ulong field, int row)
        {
            byte data = (byte)(field >> (8 * row));
            string bits = Convert.ToString(data, 2).Replace('1', 'X');
            return new string(bits.PadLeft(8, '0').Reverse().ToArray());
        }

        public override string ToString()
        {
            StringBuilder b = new();

            for (int i = 0; i < 8; i++)
            {
                b.Append(FieldRowString(_xz0, i));
                b.Append(" | ");
                b.Append(FieldRowString(_xz1, i));
                b.Append(" | ");
                b.Append(FieldRowString(_xz2, i));
                b.AppendLine();
            }

            return b.ToString();
        }
    }
}
