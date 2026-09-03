using UnityEngine;

namespace Clotzbergh
{
    /// <summary>
    /// Represents a region of the world in 1x1x1 klotzes.
    /// </summary>
    public abstract class KlotzRegion
    {
        public static readonly KlotzRegion Empty = new EmptyKlotzRegion();

        protected KlotzRegion() { }

        public static KlotzRegion AroundKlotz(AbsKlotzCoords klotzMin, AbsKlotzCoords klotzMax, int radius, int height)
        {
            return new AroundKlotzRegion(klotzMin, klotzMax, radius, height);
        }

        public abstract bool Touches(ChunkCoords chunkCoords);

        public bool Contains(ChunkCoords chunkCoords, int x, int y, int z)
        {
            return ContainsAbs(
                x + chunkCoords.X * WorldDef.ChunkSubDivsX,
                y + chunkCoords.Y * WorldDef.ChunkSubDivsY,
                z + chunkCoords.Z * WorldDef.ChunkSubDivsZ);
        }

        public bool Contains(AbsKlotzCoords absKlotzCoords)
        {
            return ContainsAbs(
                absKlotzCoords.X,
                absKlotzCoords.Y,
                absKlotzCoords.Z);
        }

        public abstract bool ContainsAbs(int x, int y, int z);

        public bool IsEmpty { get { return this is EmptyKlotzRegion; } }
    }

    public sealed class EmptyKlotzRegion : KlotzRegion
    {
        public EmptyKlotzRegion() { }

        public override bool Touches(ChunkCoords chunkCoords) { return false; }

        public override bool ContainsAbs(int x, int y, int z) { return false; }
    }

    /// <summary>
    /// Everything within a radius of a klotz, horizontally. Since the klotz's footprint is not
    /// generally square, this is not a circle but that footprint grown by the radius in X/Z -
    /// a rectangle with rounded corners, which for a 1x1 klotz is a circle again.
    /// </summary>
    public class AroundKlotzRegion : KlotzRegion
    {
        private readonly AbsKlotzCoords _klotzMin;
        private readonly AbsKlotzCoords _klotzMax;
        private readonly int _radius;
        private readonly int _height;
        private readonly BoundsInt _roughBounds;

        /// <summary>
        /// The region sits on top of the klotz rather than being centred on it - centred, it
        /// would cut away mostly below what the player is looking at.
        /// </summary>
        private int Bottom => _klotzMin.Y;
        private int Top => _klotzMin.Y + _height;

        public AroundKlotzRegion(AbsKlotzCoords klotzMin, AbsKlotzCoords klotzMax, int radius, int height)
        {
            _klotzMin = klotzMin;
            _klotzMax = klotzMax;
            _radius = radius;
            _height = height;
            _roughBounds = new(
                klotzMin.X - radius, Bottom, klotzMin.Z - radius,
                klotzMax.X - klotzMin.X + radius * 2, height, klotzMax.Z - klotzMin.Z + radius * 2);
        }

        public override bool Touches(ChunkCoords chunkCoords)
        {
            var chunkBounds = new BoundsInt(
                chunkCoords.X * WorldDef.ChunkSubDivsX,
                chunkCoords.Y * WorldDef.ChunkSubDivsY,
                chunkCoords.Z * WorldDef.ChunkSubDivsZ,
                WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ);

            return chunkBounds.Touches(_roughBounds);
        }

        public override bool ContainsAbs(int x, int y, int z)
        {
            if (y < Bottom || y > Top)
                return false;

            // Distance to the footprint rectangle, which is 0 for anything inside it. Compared
            // squared to stay in integers and skip the square root.
            int dx = Mathf.Max(0, Mathf.Max(_klotzMin.X - x, x - _klotzMax.X));
            int dz = Mathf.Max(0, Mathf.Max(_klotzMin.Z - z, z - _klotzMax.Z));

            return dx * dx + dz * dz <= _radius * _radius;
        }
    }
}
