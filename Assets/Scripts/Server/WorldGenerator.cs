using System;
using Clotzbergh.Server.ChunkGeneration;
using Clotzbergh.Server.StructureGeneration;
using UnityEngine;

namespace Clotzbergh.Server
{
    public struct WorldGenParams
    {
        public int Seed { get; set; }
        public WorldRoughnessType Roughness { get; set; }
        public WorldGranularityType Granularity { get; set; }
        public WorldStructureCreation Structures { get; set; }

        public static WorldGenParams HillyRegular(int seed) => new()
        {
            Seed = seed,
            Roughness = WorldRoughnessType.Hilly,
            Granularity = WorldGranularityType.Regular,
            Structures = WorldStructureCreation.WithStructures,
        };

        public static WorldGenParams HillyMicroBlocks(int seed) => new()
        {
            Seed = seed,
            Roughness = WorldRoughnessType.Hilly,
            Granularity = WorldGranularityType.MicroBlocks,
            Structures = WorldStructureCreation.WithStructures,
        };

        public static WorldGenParams FlatRegular(int seed) => new()
        {
            Seed = seed,
            Roughness = WorldRoughnessType.Flat,
            Granularity = WorldGranularityType.Regular,
            Structures = WorldStructureCreation.NoStructures,
        };

        public static WorldGenParams FlatMicroBlocks(int seed) => new()
        {
            Seed = seed,
            Roughness = WorldRoughnessType.Flat,
            Granularity = WorldGranularityType.MicroBlocks,
            Structures = WorldStructureCreation.NoStructures,
        };
    }

    public enum WorldRoughnessType { Flat, Hilly, }

    public enum WorldGranularityType { MicroBlocks, Regular, }

    public enum WorldStructureCreation { NoStructures, WithStructures, }

    public delegate KlotzColor ColorFunction(int x, int y, int z);

    public class WorldGenerator
    {
        protected IHeightMap HeightMap { get; }
        protected ColorFunction ColorFunc { get; }
        protected GeneratorFactory<ChunkGenerator> ChunkGeneratorFactory { get; }
        protected GeneratorFactory<StructureGenerator> StructureGeneratorFactory { get; }

        public WorldGenerator(int seed)
         : this(WorldGenParams.HillyRegular(seed)) { }

        public WorldGenerator(WorldGenParams genParams)
        {
            HeightMap = genParams.Roughness switch
            {
                WorldRoughnessType.Flat => new FlatHeightMap(-10f),
                WorldRoughnessType.Hilly => new DefaultHeightMap(genParams.Seed),
                _ => throw new ArgumentOutOfRangeException(),
            };

            ColorFunc = genParams.Roughness switch
            {
                WorldRoughnessType.Flat => ColorByChunk,
                WorldRoughnessType.Hilly => ColorFromHeight,
                _ => throw new ArgumentOutOfRangeException(),
            };

            ChunkGeneratorFactory = new(genParams.Granularity switch
            {
                WorldGranularityType.MicroBlocks => typeof(CG02_MicroBlockChunkGenerator),
                WorldGranularityType.Regular => typeof(CG04_WaveFunctionCollapseGeneratorV2),
                _ => throw new ArgumentOutOfRangeException(),
            });

            StructureGeneratorFactory = new(genParams.Structures switch
            {
                WorldStructureCreation.NoStructures => typeof(NoStructureGenerator),
                WorldStructureCreation.WithStructures => typeof(SimpleCentralHouseGenerator),
                _ => throw new ArgumentOutOfRangeException(),
            });
        }

        public WorldChunk GetChunk(ChunkCoords chunkCoords)
        {
            StructureGenerator structureGenerator = StructureGeneratorFactory.CreateGenerator(chunkCoords);
            ChunkGenerator chunkGenerator = ChunkGeneratorFactory.CreateGenerator(chunkCoords);

            FieldResolver resolver = new(chunkCoords, HeightMap);
            resolver.AddHeightMapOverride(structureGenerator);

            WorldChunk chunk = chunkGenerator.Generate(resolver, ColorFunc);
            //structureGenerator.PopulateStructures(chunk);
            return chunk;
        }

        public Mesh GeneratePreviewMesh(int dist)
        {
            int size = 2 * dist;

            Vector3[] vertices = new Vector3[size * size];
            int[] triangles = new int[(size - 1) * (size - 1) * 6];

            int vIndex = 0;
            for (int y = -dist; y < dist; y++)
            {
                for (int x = -dist; x < dist; x++)
                {
                    vertices[vIndex++] = new Vector3(
                        x * WorldDef.SubKlotzSize.x,
                        HeightMap.At(x, y),
                        y * WorldDef.SubKlotzSize.z);
                }
            }

            int triIndex = 0;
            for (int iy = 0; iy < size - 1; iy++)
            {
                for (int ix = 0; ix < size - 1; ix++)
                {
                    int current = ix + iy * size;

                    triangles[triIndex++] = current;
                    triangles[triIndex++] = current + size;
                    triangles[triIndex++] = current + 1;

                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = current + size;
                    triangles[triIndex++] = current + size + 1;
                }
            }

            Mesh mesh = new()
            {
                vertices = vertices,
                triangles = triangles
            };

            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>
        /// 
        /// </summary>
        public static KlotzColor ColorFromHeight(int absX, int absY, int absZ)
        {
            if (absY < -80) return KlotzColor.Azure;
            if (absY < -70) return KlotzColor.Yellow;
            if (absY < -20) return KlotzColor.DarkGreen;
            if (absY < 30) return KlotzColor.DarkBrown;
            if (absY < 70) return KlotzColor.Gray;
            return KlotzColor.White;
        }

        /// <summary>
        /// 
        /// </summary>
        public static KlotzColor ColorByChunk(int absX, int absY, int absZ)
        {
            return UniqueColor(
                AbsKlotzCoords.FloorDiv(absX, WorldDef.ChunkSubDivsX),
                AbsKlotzCoords.FloorDiv(absY, WorldDef.ChunkSubDivsY),
                AbsKlotzCoords.FloorDiv(absZ, WorldDef.ChunkSubDivsZ));
        }

        private static int Hash3(int x, int y, int z)
        {
            unchecked
            {
                int h = x;
                h = h * 374761393 + y * 668265263;
                h = h * 2147483647 + z * 1274126177;
                h ^= (h >> 13);
                h *= 1274126177;
                return h;
            }
        }

        public static KlotzColor UniqueColor(int x, int y, int z)
        {
            int h = Hash3(x, y, z);
            return (KlotzColor)(Math.Abs(h) % (int)KlotzColor.Count);
        }
    }
}
