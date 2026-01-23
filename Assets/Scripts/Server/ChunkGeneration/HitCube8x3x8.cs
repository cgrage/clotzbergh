using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clotzbergh.Server.ChunkGeneration
{
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
