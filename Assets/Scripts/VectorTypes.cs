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
}
