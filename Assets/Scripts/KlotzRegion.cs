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

        /// <summary>
        /// Whether a chunk's mesh is affected by this region. Includes chunks holding klotzes
        /// that only reach into the region, since those are cut away whole.
        /// </summary>
        public abstract bool Touches(ChunkCoords chunkCoords);

        /// <summary>
        /// Bounds no part of the region falls outside of.
        /// </summary>
        public abstract BoundsInt RoughBounds { get; }

        /// <summary>
        /// Whether any cell of the inclusive range from min to max lies within the region.
        /// </summary>
        public abstract bool IntersectsAbs(AbsKlotzCoords min, AbsKlotzCoords max);

        public bool IsEmpty { get { return this is EmptyKlotzRegion; } }
    }

    public sealed class EmptyKlotzRegion : KlotzRegion
    {
        public EmptyKlotzRegion() { }

        public override bool Touches(ChunkCoords chunkCoords) { return false; }

        public override BoundsInt RoughBounds => default;

        public override bool IntersectsAbs(AbsKlotzCoords min, AbsKlotzCoords max) { return false; }
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
        private readonly BoundsInt _reachBounds;

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

            // Klotzes are cut away whole, so one reaching into the region from a neighbouring
            // chunk changes that chunk's mesh too. Horizontal and vertical reach differ a lot -
            // a single value would pull in chunks that cannot be affected at all.
            int reachXZ = KlotzKB.MaxExtentXZ;
            int reachY = KlotzKB.MaxExtentY;
            _reachBounds = new(
                _roughBounds.xMin - reachXZ, _roughBounds.yMin - reachY, _roughBounds.zMin - reachXZ,
                _roughBounds.size.x + 2 * reachXZ, _roughBounds.size.y + 2 * reachY, _roughBounds.size.z + 2 * reachXZ);
        }

        public override BoundsInt RoughBounds => _roughBounds;

        public override bool Touches(ChunkCoords chunkCoords)
        {
            var chunkBounds = new BoundsInt(
                chunkCoords.X * WorldDef.ChunkSubDivsX,
                chunkCoords.Y * WorldDef.ChunkSubDivsY,
                chunkCoords.Z * WorldDef.ChunkSubDivsZ,
                WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ);

            return chunkBounds.Touches(_reachBounds);
        }

        public override bool IntersectsAbs(AbsKlotzCoords min, AbsKlotzCoords max)
        {
            if (max.Y < Bottom || min.Y > Top)
                return false;

            // Distance between the two rectangles, which is 0 where they overlap. Compared
            // squared to stay in integers and skip the square root.
            int dx = Mathf.Max(0, Mathf.Max(_klotzMin.X - max.X, min.X - _klotzMax.X));
            int dz = Mathf.Max(0, Mathf.Max(_klotzMin.Z - max.Z, min.Z - _klotzMax.Z));

            return dx * dx + dz * dz <= _radius * _radius;
        }
    }
}
