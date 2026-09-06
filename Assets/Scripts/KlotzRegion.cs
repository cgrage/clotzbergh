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

        /// <summary>
        /// A ball of the given radius around the klotz. The radius is horizontal, and since a
        /// sub-klotz is 2.5 times shorter in Y than in X/Z, the ball covers 2.5 times as many
        /// cells vertically - which is what makes it round in the world rather than in cells.
        /// </summary>
        public static KlotzRegion AroundKlotz(AbsKlotzCoords klotzMin, AbsKlotzCoords klotzMax, int radius)
        {
            // Rounded up, so the bounds never cut into the shape the containment test allows.
            int verticalReach = (radius * 5 + 1) / 2;

            return new NearKlotzRegion(klotzMin, klotzMax, radius,
                klotzMin.Y - verticalReach, klotzMax.Y + verticalReach, ballShaped: true);
        }

        /// <summary>
        /// The klotz's footprint grown by the radius, covering everything standing higher than
        /// anchorTopY and nothing at or below it - levelling a patch of ground to that height.
        /// The height comes separately from the footprint so it can stay put while the player
        /// drags across uneven ground.
        /// </summary>
        public static KlotzRegion AboveKlotz(AbsKlotzCoords klotzMin, AbsKlotzCoords klotzMax, int radius, int anchorTopY, int verticalReach)
        {
            return new NearKlotzRegion(klotzMin, klotzMax, radius,
                anchorTopY + 1, anchorTopY + verticalReach, ballShaped: false);
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
    /// A klotz's footprint grown by a radius horizontally, over an explicit range of heights.
    /// Since the footprint is not generally square, the horizontal shape is not a circle but a
    /// rectangle with rounded corners, which for a 1x1 klotz is a circle again. Where the
    /// vertical range sits relative to the klotz is what separates the tools - see the factory
    /// methods on <see cref="KlotzRegion"/>.
    /// </summary>
    public class NearKlotzRegion : KlotzRegion
    {
        private readonly AbsKlotzCoords _klotzMin;
        private readonly AbsKlotzCoords _klotzMax;
        private readonly int _radius;
        private readonly int _bottom;
        private readonly int _top;
        private readonly bool _ballShaped;
        private readonly BoundsInt _roughBounds;
        private readonly BoundsInt _reachBounds;

        public NearKlotzRegion(AbsKlotzCoords klotzMin, AbsKlotzCoords klotzMax, int radius, int bottom, int top, bool ballShaped)
        {
            _klotzMin = klotzMin;
            _klotzMax = klotzMax;
            _radius = radius;
            _bottom = bottom;
            _top = top;
            _ballShaped = ballShaped;
            _roughBounds = new(
                klotzMin.X - radius, bottom, klotzMin.Z - radius,
                klotzMax.X - klotzMin.X + radius * 2,
                top - bottom,
                klotzMax.Z - klotzMin.Z + radius * 2);

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
            if (max.Y < _bottom || min.Y > _top)
                return false;

            // Distance between the two boxes per axis, which is 0 where they overlap. Compared
            // squared to stay in integers and skip the square root.
            int dx = Mathf.Max(0, Mathf.Max(_klotzMin.X - max.X, min.X - _klotzMax.X));
            int dz = Mathf.Max(0, Mathf.Max(_klotzMin.Z - max.Z, min.Z - _klotzMax.Z));

            if (!_ballShaped)
                return dx * dx + dz * dz <= _radius * _radius;

            int dy = Mathf.Max(0, Mathf.Max(_klotzMin.Y - max.Y, min.Y - _klotzMax.Y));

            // Y joins the same distance rather than being clamped on its own - that is what
            // rounds the shape off instead of leaving a cylinder. Counted in horizontal cell
            // widths, one cell of Y is 0.4 of one in X/Z, and everything is scaled by 5 to keep
            // the arithmetic in integers.
            return 25 * (dx * dx + dz * dz) + 4 * (dy * dy) <= 25 * _radius * _radius;
        }
    }
}
