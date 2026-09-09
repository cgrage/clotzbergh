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
        /// <summary>
        /// The height terrain is generated around. Derived from the world floor rather than
        /// fixed, so it keeps two chunks of room underneath for valleys to run down into - move
        /// the limits and the terrain moves with them instead of being cut off from below.
        /// </summary>
        private static readonly float TerrainBaseHeight =
            (WorldDef.Limits.MinCoordsY + 2) * WorldDef.ChunkSize.y;

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
                WorldRoughnessType.Flat => new FlatHeightMap(TerrainBaseHeight - 10f),
                WorldRoughnessType.Hilly => new DefaultHeightMap(genParams.Seed, TerrainBaseHeight),
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

            if (structureGenerator.GenModifier != null)
                resolver.AddModifier(structureGenerator.GenModifier);

            resolver.RunOnBeforeGeneration();
            WorldChunk chunk = chunkGenerator.Generate(resolver, ColorFunc);
            structureGenerator.PopulateStructures(chunk);
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
            // Measured from the water line, so the bands keep their meaning if it ever moves.
            // Sand covers everything submerged plus a strip of beach above it.
            if (absY < WorldDef.WaterLevel + 10) return KlotzColor.Yellow;     // -80 ..   9
            if (absY < WorldDef.WaterLevel + 60) return KlotzColor.DarkGreen;  //  10 ..  59
            if (absY < WorldDef.WaterLevel + 110) return KlotzColor.DarkBrown; //  60 .. 109
            if (absY < WorldDef.WaterLevel + 150) return KlotzColor.Gray;      // 110 .. 149
            return KlotzColor.White;                                           // 150 .. 319
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

        /// <summary>
        /// A color per cell, where no two cells sharing a face ever get the same one. A step
        /// along any axis flips the parity of x+y+z, so letting that parity pick which half of
        /// the palette to draw from separates neighbors no matter what the hash returns. Cells
        /// meeting only at an edge or corner keep the same parity and may well collide.
        /// </summary>
        public static KlotzColor UniqueColor(int x, int y, int z)
        {
            int parity = (x + y + z) & 1;
            int half = (int)KlotzColor.Count / 2;
            int h = Hash3(x, y, z) & 0x7fffffff;

            return (KlotzColor)(2 * (h % half) + parity);
        }
    }
}
