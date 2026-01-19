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

        public abstract bool Contains(RelKlotzCoords coords);
    }

    public sealed class EmptyKlotzRegion : KlotzRegion
    {
        public EmptyKlotzRegion() { }

        public override bool Contains(RelKlotzCoords coords)
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

        public override bool Contains(RelKlotzCoords coords)
        {
            if (coords.Y < _anchor.Y || coords.Y > _anchor.Y + _height)
                return false;

            var horizontalDistance = new Vector2(coords.X - _anchor.X, coords.Z - _anchor.Z).magnitude;
            return horizontalDistance <= _radius;
        }
    }
}
