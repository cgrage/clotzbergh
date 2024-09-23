using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator
{
    public class WorldGeneratorConfig
    {
        public float MeshHeightMultiplier { get; set; }
        // public AnimationCurve meshHeightCurve;
        public float NoiseScale { get; set; }
        public int Octaves { get; set; }
        public float Persistence { get; set; }
        public float Lacunarity { get; set; }
        public int Seed { get; set; }

        public WorldGeneratorConfig()
        {
            MeshHeightMultiplier = 20f;
            NoiseScale = 20f;
            Octaves = 4;
            Persistence = 0.5f;
            Lacunarity = 2f;
            Seed = 0;
        }
    }

    public WorldGenerator()
    {
        Config = new WorldGeneratorConfig();
    }

    public WorldGeneratorConfig Config { get; private set; }
}
