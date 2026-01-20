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

        public static KlotzRegion Cylindrical(AbsKlotzCoords anchor, int radius, int height)
        {
            return new CylindricalKlotzRegion(anchor, radius, height);
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

    public class CylindricalKlotzRegion : KlotzRegion
    {
        private readonly AbsKlotzCoords _anchor;
        private readonly int _radius;
        private readonly int _height;
        private readonly BoundsInt _roughBounds;

        private int Bottom => _anchor.Y - _height / 2;
        private int Top => _anchor.Y + _height / 2;

        public CylindricalKlotzRegion(AbsKlotzCoords anchor, int radius, int height)
        {
            _anchor = anchor;
            _radius = radius;
            _height = height;
            _roughBounds = new(
                anchor.X - radius, Bottom, anchor.Z - radius,
                radius * 2, height, radius * 2);
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

            var hDist = new Vector2(x - _anchor.X, z - _anchor.Z).magnitude;
            if (hDist > _radius)
                return false;

            return true;
        }
    }
}
