using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode { Local, Global }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        if (mapWidth < 1) mapWidth = 1;
        if (mapHeight < 1) mapHeight = 1;
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

        float[,] noiseMap = new float[mapWidth, mapHeight];
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - mapWidth / 2f + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - mapHeight / 2f + octaveOffsets[i].y) / scale * frequency;

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

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1f) / (2f * maxPossibleHeight / 2f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, float.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
