using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Client
{
    /// <summary>
    /// Represents a region of the world in 1x1x1 klotzes.
    /// </summary>
    public class KlotzRegion
    {
        // private ClientChunk[] _chunks;

        /// <summary>
        /// Creates an empty region
        /// </summary>
        public KlotzRegion()
        {

        }

        public static KlotzRegion CreateEmpty()
        {
            return new KlotzRegion();
        }

        public static KlotzRegion Create(/*ClientChunk center, int radius*/)
        {
            return new()
            {
                // _chunks = new ClientChunk[] { center }
            };
        }

        // IEnumerable<ClientChunk> Chunks => _chunks;
    }
}
