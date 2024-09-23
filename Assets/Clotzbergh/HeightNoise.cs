using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightNoise
{
    public static float[,] Generate(int width, int height, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        if (width < 1) width = 1;
        if (height < 1) height = 1;
        if (scale <= 0) scale = 0.001f;
        if (octaves < 0) octaves = 0;

        System.Random rnd = new System.Random(seed);

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new Vector2(
                rnd.Next(-100000, 100000) + offset.x,
                rnd.Next(-100000, 100000) - offset.y
            );

            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }

        float[,] noiseMap = new float[width, height];
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - width / 2f + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - height / 2f + octaveOffsets[i].y) / scale * frequency;

                    float pVal = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += pVal * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedHeight = (noiseMap[x, y] + 1f) / (2f * maxPossibleHeight / 2f);
                noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, float.MaxValue);
            }
        }

        return noiseMap;
    }
}
