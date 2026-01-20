using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Clotzbergh
{
    public readonly struct KlotzSize : IEquatable<KlotzSize>
    {
        private readonly Vector3Int value;

        public KlotzSize(Vector3Int value) => this.value = value;
        public KlotzSize(int x, int y, int z) => this.value = new Vector3Int(x, y, z);

        public readonly int X => value.x;
        public readonly int Y => value.y;
        public readonly int Z => value.z;

        public override readonly string ToString() => value.ToString();
        public override readonly bool Equals(object obj) => obj is KlotzSize ks && value.Equals(ks.value);
        public bool Equals(KlotzSize other) { return value.Equals(other.value); }
        public override readonly int GetHashCode() => value.GetHashCode();

        public static readonly KlotzSize Zero = new(0, 0, 0);

        public readonly Vector3Int ToVector() => value;

        public static bool operator ==(KlotzSize a, KlotzSize b) => a.value == b.value;
        public static bool operator !=(KlotzSize a, KlotzSize b) => a.value != b.value;
    }

    public readonly struct ChunkCoords : IEquatable<ChunkCoords>
    {
        private readonly Vector3Int value;

        public ChunkCoords(Vector3Int value) => this.value = value;
        public ChunkCoords(int x, int y, int z) => this.value = new Vector3Int(x, y, z);

        public readonly int X => value.x;
        public readonly int Y => value.y;
        public readonly int Z => value.z;

        public override readonly string ToString() => value.ToString();
        public override readonly bool Equals(object obj) => obj is ChunkCoords cc && value.Equals(cc.value);
        public bool Equals(ChunkCoords other) { return value.Equals(other.value); }
        public override readonly int GetHashCode() => value.GetHashCode();

        public static readonly ChunkCoords Zero = new(0, 0, 0);
        public static readonly ChunkCoords Invalid = new(int.MinValue, int.MinValue, int.MinValue);

        public readonly Vector3Int ToVector() => value;

        public static bool operator ==(ChunkCoords a, ChunkCoords b) => a.value == b.value;
        public static bool operator !=(ChunkCoords a, ChunkCoords b) => a.value != b.value;

        /// <summary>
        /// Calculates the Euclidean distance between two ChunkCoords.
        /// Examples: 
        ///   - Distance((0,0,0), (1,0,0)) == 1
        ///   - Distance((0,0,0), (1,1,0)) == 1.414...
        /// </summary>
        public static float Distance(ChunkCoords a, ChunkCoords b)
        {
            return Vector3Int.Distance(a.value, b.value);
        }

        public AbsKlotzCoords AsBaseAbsKlotzCoords()
        {
            return new AbsKlotzCoords(
                X * WorldDef.ChunkSubDivs.x,
                Y * WorldDef.ChunkSubDivs.y,
                Z * WorldDef.ChunkSubDivs.z);
        }
    }

    public readonly struct RelKlotzCoords : IEquatable<RelKlotzCoords>
    {
        private readonly Vector3Int value;

        public RelKlotzCoords(Vector3Int value) => this.value = value;
        public RelKlotzCoords(int x, int y, int z) => this.value = new Vector3Int(x, y, z);

        public readonly int X => value.x;
        public readonly int Y => value.y;
        public readonly int Z => value.z;

        public override readonly string ToString() => value.ToString();
        public override readonly bool Equals(object obj) => obj is RelKlotzCoords cc && value.Equals(cc.value);
        public bool Equals(RelKlotzCoords other) { return value.Equals(other.value); }
        public override readonly int GetHashCode() => value.GetHashCode();

        public static readonly RelKlotzCoords Zero = new(0, 0, 0);

        public readonly Vector3Int ToVector() => value;

        public static bool operator ==(RelKlotzCoords a, RelKlotzCoords b) => a.value == b.value;
        public static bool operator !=(RelKlotzCoords a, RelKlotzCoords b) => a.value != b.value;

        public AbsKlotzCoords ToAbs(ChunkCoords chunkCoords)
        {
            return new AbsKlotzCoords(
                chunkCoords.X * WorldDef.ChunkSubDivs.x + X,
                chunkCoords.Y * WorldDef.ChunkSubDivs.y + Y,
                chunkCoords.Z * WorldDef.ChunkSubDivs.z + Z);
        }
    }

    public readonly struct AbsKlotzCoords : IEquatable<AbsKlotzCoords>
    {
        private readonly Vector3Int value;

        public AbsKlotzCoords(Vector3Int value) => this.value = value;
        public AbsKlotzCoords(int x, int y, int z) => this.value = new Vector3Int(x, y, z);

        public readonly int X => value.x;
        public readonly int Y => value.y;
        public readonly int Z => value.z;

        public override readonly string ToString() => value.ToString();
        public override readonly bool Equals(object obj) => obj is AbsKlotzCoords cc && value.Equals(cc.value);
        public bool Equals(AbsKlotzCoords other) { return value.Equals(other.value); }
        public override readonly int GetHashCode() => value.GetHashCode();

        public static readonly AbsKlotzCoords Zero = new(0, 0, 0);

        public readonly Vector3Int ToVector() => value;

        public static bool operator ==(AbsKlotzCoords a, AbsKlotzCoords b) => a.value == b.value;
        public static bool operator !=(AbsKlotzCoords a, AbsKlotzCoords b) => a.value != b.value;

        public RelKlotzCoords ToRel()
        {
            return new RelKlotzCoords(
                X % WorldDef.ChunkSubDivs.x,
                Y % WorldDef.ChunkSubDivs.y,
                Z % WorldDef.ChunkSubDivs.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorDiv(int x, int d)
        {
            int q = x / d;
            int r = x % d;
            return (r != 0 && ((r ^ d) < 0)) ? q - 1 : q;
        }

        public ChunkCoords ToChunkCoords()
        {
            return new ChunkCoords(
                FloorDiv(X, WorldDef.ChunkSubDivsX),
                FloorDiv(Y, WorldDef.ChunkSubDivsY),
                FloorDiv(Z, WorldDef.ChunkSubDivsZ));
        }
    }

    public readonly struct KlotzIndex : IEquatable<KlotzIndex>
    {
        private readonly Vector3Int value;

        public KlotzIndex(Vector3Int value) => this.value = value;
        public KlotzIndex(int x, int y, int z) => this.value = new Vector3Int(x, y, z);

        public readonly int X => value.x;
        public readonly int Y => value.y;
        public readonly int Z => value.z;

        public override readonly string ToString() => value.ToString();
        public override readonly bool Equals(object obj) => obj is KlotzIndex cc && value.Equals(cc.value);
        public bool Equals(KlotzIndex other) { return value.Equals(other.value); }
        public override readonly int GetHashCode() => value.GetHashCode();

        public static readonly KlotzIndex Zero = new(0, 0, 0);

        public readonly Vector3Int ToVector() => value;

        public static bool operator ==(KlotzIndex a, KlotzIndex b) => a.value == b.value;
        public static bool operator !=(KlotzIndex a, KlotzIndex b) => a.value != b.value;
    }

    public static class BoundsIntExt
    {
        /// <summary>
        /// Determines whether two BoundsInt intersect.
        /// </summary>
        public static bool Intersects(this BoundsInt a, BoundsInt b)
        {
            return (a.xMin < b.xMax) && (a.xMax > b.xMin) &&
                (a.yMin < b.yMax) && (a.yMax > b.yMin) &&
                (a.zMin < b.zMax) && (a.zMax > b.zMin);
        }

        /// <summary>
        /// Determines whether two BoundsInt intersect, including edges.
        /// </summary>
        public static bool Touches(this BoundsInt a, BoundsInt b)
        {
            return (a.xMin <= b.xMax) && (a.xMax >= b.xMin) &&
                (a.yMin <= b.yMax) && (a.yMax >= b.yMin) &&
                (a.zMin <= b.zMax) && (a.zMax >= b.zMin);
        }
    }
}
