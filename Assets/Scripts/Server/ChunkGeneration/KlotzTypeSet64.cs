using System.Collections;
using System.Collections.Generic;

namespace Clotzbergh.Server.ChunkGeneration
{
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
}
