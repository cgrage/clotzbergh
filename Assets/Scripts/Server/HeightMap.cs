using UnityEngine;
using Random = System.Random;

namespace Clotzbergh.Server
{
    public interface IHeightMap
    {
        float At(int x, int y);
    }

    public class DefaultHeightMap : IHeightMap
    {
        public const float NoisePlaneScale = 0.003f;
        public const float NoiseHeightScale = 20f;
        public const float NoisePersistence = 0.5f;
        public const float NoiseLacunarity = 2f;
        public const int DefaultOctaves = 4;

        private readonly Vector2[] _octaveOffsets;

        public DefaultHeightMap(int octaves = DefaultOctaves, int seed = 0)
        {
            Random rnd = new(seed);

            _octaveOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                _octaveOffsets[i] = new Vector2(
                    rnd.Next(-100000, 100000),
                    rnd.Next(-100000, 100000)
                );
            }
        }

        public float At(int x, int y)
        {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            for (int i = 0; i < _octaveOffsets.Length; i++)
            {
                float sampleX = (x + _octaveOffsets[i].x) * NoisePlaneScale * frequency;
                float sampleY = (y + _octaveOffsets[i].y) * NoisePlaneScale * frequency;

                float pVal = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                noiseHeight += pVal * amplitude;

                amplitude *= NoisePersistence;
                frequency *= NoiseLacunarity;
            }

            return noiseHeight * NoiseHeightScale;
        }
    }
}