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

        public static KlotzRegion Cylindrical(Vector3Int anchor, int radius, int height)
        {
            return new CylindricalKlotzRegion(anchor, radius, height);
        }

        public abstract bool Contains(Vector3Int coords);
    }

    public sealed class EmptyKlotzRegion : KlotzRegion
    {
        public EmptyKlotzRegion() { }

        public override bool Contains(Vector3Int coords)
        {
            return false;
        }
    }

    public class CylindricalKlotzRegion : KlotzRegion
    {
        private readonly Vector3Int _anchor;
        private readonly int _radius;
        private readonly int _height;

        public CylindricalKlotzRegion(Vector3Int anchor, int radius, int height)
        {
            _anchor = anchor;
            _radius = radius;
            _height = height;
        }

        public override bool Contains(Vector3Int coords)
        {
            if (coords.y < _anchor.y || coords.y > _anchor.y + _height)
                return false;

            var horizontalDistance = new Vector2(coords.x - _anchor.x, coords.z - _anchor.z).magnitude;
            return horizontalDistance <= _radius;
        }
    }
}
