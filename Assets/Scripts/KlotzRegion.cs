using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh
{
    /// <summary>
    /// Represents a region of the world in 1x1x1 klotzes.
    /// </summary>
    public readonly struct KlotzRegion
    {
        private readonly Vector3Int _anchor;
        private readonly int _a;

        public static readonly KlotzRegion Empty = new(Vector3Int.zero, 0);

        private KlotzRegion(Vector3Int anchor, int a)
        {
            _anchor = anchor;
            _a = a;
        }

        public static KlotzRegion Spherical(Vector3Int center, int radius)
        {
            return new KlotzRegion(center, radius);
        }

        public bool Contains(Vector3Int coords)
        {
            return Vector3Int.Distance(_anchor, coords) <= _a;
        }
    }
}
