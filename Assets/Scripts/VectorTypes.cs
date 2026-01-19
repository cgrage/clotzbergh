using UnityEngine;

namespace Clotzbergh
{
    public struct KlotzSize
    {
        private Vector3Int value;

        public KlotzSize(Vector3Int value) => this.value = value;
        public KlotzSize(int x, int y, int z) => this.value = new Vector3Int(x, y, z);

        public readonly int X => value.x;
        public readonly int Y => value.y;
        public readonly int Z => value.z;

        public override readonly string ToString() => value.ToString();
        public override readonly bool Equals(object obj) => obj is KlotzSize ks && value.Equals(ks.value);
        public override readonly int GetHashCode() => value.GetHashCode();

        public static readonly KlotzSize Zero = new(0, 0, 0);

        public readonly Vector3Int ToVector() => value;

        public static bool operator ==(KlotzSize a, KlotzSize b) => a.value == b.value;
        public static bool operator !=(KlotzSize a, KlotzSize b) => a.value != b.value;
    }

    public struct ChunkCoords
    {
        private Vector3Int value;

        public ChunkCoords(Vector3Int value) => this.value = value;
        public ChunkCoords(int x, int y, int z) => this.value = new Vector3Int(x, y, z);

        public readonly int X => value.x;
        public readonly int Y => value.y;
        public readonly int Z => value.z;

        public override readonly string ToString() => value.ToString();
        public override readonly bool Equals(object obj) => obj is ChunkCoords cc && value.Equals(cc.value);
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
}
