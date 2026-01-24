using UnityEngine;

namespace Clotzbergh.Server
{
    public interface IHeightMapOverride
    {
        float HeightMapOverride(IHeightMap heightMap, int absX, int absZ);
    }

    public class FieldResolver
    {
        private readonly ChunkCoords _coords;
        private readonly IHeightMap _heightMap;
        private IHeightMapOverride _heightMapOverride = null;

        public FieldResolver(ChunkCoords coords, IHeightMap heightMap)
        {
            _coords = coords;
            _heightMap = heightMap;
        }

        public void AddHeightMapOverride(IHeightMapOverride heightMapOverride)
        {
            _heightMapOverride = heightMapOverride;
        }

        public int GroundStartAtRelPos(int x, int z)
        {
            return Mathf.RoundToInt(HeightAtRelPos(x, z) / WorldDef.SubKlotzSize.y);
        }

        protected float HeightAtRelPos(int x, int z)
        {
            int absX = _coords.X * WorldDef.ChunkSubDivsX + x;
            int absZ = _coords.Z * WorldDef.ChunkSubDivsZ + z;

            if (_heightMapOverride != null)
            {
                return _heightMapOverride.HeightMapOverride(_heightMap, absX, absZ);
            }
            else
            {
                return _heightMap.At(absX, absZ);
            }
        }
    }
}
