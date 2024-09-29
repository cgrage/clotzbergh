using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMap
{
    public const float NoisePlaneScale = 0.03f;
    public const float NoiseHeightScale = 5f;
    public const float NoisePersistence = 0.5f;
    public const float NoiseLacunarity = 2f;

    private readonly Vector2[] _octaveOffsets;

    public HeightMap(int octaves = 4, int seed = 0)
    {
        System.Random rnd = new(seed);

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
