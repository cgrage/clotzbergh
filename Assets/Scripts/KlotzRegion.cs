using System.Collections.Generic;
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

        public static KlotzRegion Cylindrical(RelKlotzCoords anchor, int radius, int height)
        {
            return new CylindricalKlotzRegion(anchor, radius, height);
        }

        public abstract bool Contains(int x, int y, int z);

        public bool IsEmpty { get { return this is EmptyKlotzRegion; } }
    }

    public sealed class EmptyKlotzRegion : KlotzRegion
    {
        public EmptyKlotzRegion() { }

        public override bool Contains(int x, int y, int z)
        {
            return false;
        }
    }

    public class CylindricalKlotzRegion : KlotzRegion
    {
        private readonly RelKlotzCoords _anchor;
        private readonly int _radius;
        private readonly int _height;

        public CylindricalKlotzRegion(RelKlotzCoords anchor, int radius, int height)
        {
            _anchor = anchor;
            _radius = radius;
            _height = height;
        }

        public override bool Contains(int x, int y, int z)
        {
            if (y < _anchor.Y - _height / 2 || y > _anchor.Y + _height / 2)
                return false;

            var hDist = new Vector2(x - _anchor.X, z - _anchor.Z).magnitude;
            if (hDist > _radius)
                return false;

            return true;
        }
    }
}
