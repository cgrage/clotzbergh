using System;
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

        public static int Distance(ChunkCoords a, ChunkCoords b)
        {
            return (int)Vector3Int.Distance(a.value, b.value);
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


        public static int Distance(RelKlotzCoords a, RelKlotzCoords b)
        {
            return (int)Vector3Int.Distance(a.value, b.value);
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

        public static int Distance(KlotzIndex a, KlotzIndex b)
        {
            return (int)Vector3Int.Distance(a.value, b.value);
        }
    }
}
