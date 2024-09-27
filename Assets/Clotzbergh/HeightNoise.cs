using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightNoise
{
    public static readonly AnimationCurve HeightCurve = new(new Keyframe[] { new(0, 0, 0, 0, 0, 0), new(1, 1, 2, 2, 0, 0) });
    public const float HeightMultiplier = 20f;
    public const float NoiseScale = 20f;
    public const int NoiseOctaves = 4;
    public const float NoisePersistence = 0.5f;
    public const float NoiseLacunarity = 2f;
    public const int NoiseSeed = 0;

    public static float[,] Generate(int width, int height, Vector2 offset)
    {
        System.Random rnd = new System.Random(NoiseSeed);

        float maxPossibleHeight = 0;
        float amplitude = 1;

        Vector2[] octaveOffsets = new Vector2[NoiseOctaves];
        for (int i = 0; i < NoiseOctaves; i++)
        {
            octaveOffsets[i] = new Vector2(
                rnd.Next(-100000, 100000) + offset.x,
                rnd.Next(-100000, 100000) - offset.y
            );

            maxPossibleHeight += amplitude;
            amplitude *= NoisePersistence;
        }

        float[,] noiseMap = new float[width, height];
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < NoiseOctaves; i++)
                {
                    float sampleX = (x + octaveOffsets[i].x) / NoiseScale * frequency;
                    float sampleY = (y + octaveOffsets[i].y) / NoiseScale * frequency;

                    float pVal = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += pVal * amplitude;

                    amplitude *= NoisePersistence;
                    frequency *= NoiseLacunarity;
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
                normalizedHeight = Mathf.Clamp(normalizedHeight, 0, float.MaxValue);
                noiseMap[x, y] = HeightCurve.Evaluate(normalizedHeight) * HeightMultiplier;
            }
        }

        return noiseMap;
    }
}
