using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator
{
    public class WorldGeneratorConfig
    {
        public float NoiseScale { get; set; }
        public int NoiseOctaves { get; set; }
        public float NoisePersistence { get; set; }
        public float NoiseLacunarity { get; set; }
        public int NoiseSeed { get; set; }

        public WorldGeneratorConfig()
        {
            NoiseScale = 20f;
            NoiseOctaves = 4;
            NoisePersistence = 0.5f;
            NoiseLacunarity = 2f;
            NoiseSeed = 0;
        }
    }

    public WorldGenerator()
    {
        Config = new WorldGeneratorConfig();
    }

    public WorldGeneratorConfig Config { get; private set; }

    public WorldChunk GetChunk(Vector3Int pos)
    {
        float[,] heightNoise = HeightNoise.Generate(WorldChunk.ChunkWidth, WorldChunk.ChunkDepth,
            Config.NoiseSeed, Config.NoiseScale, Config.NoiseOctaves, Config.NoisePersistence,
            Config.NoiseLacunarity, Vector2.zero);

        return WorldChunk.CreateFilled(KlotzType.Plate1x1, 40);
    }
}
