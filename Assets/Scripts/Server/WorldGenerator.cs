using System;
using Clotzbergh.Server.ChunkGeneration;
using UnityEngine;

namespace Clotzbergh.Server
{
    public delegate KlotzColor ColorFunction(int x, int y, int z);

    public class WorldGenerator
    {
        protected Type ChunkGeneratorType { get; }
        protected IHeightMap HeightMap { get; }
        protected ColorFunction ColorFunc { get; }

        public WorldGenerator(int seed, WorldType type = WorldType.HillyRegular)
        {
            HeightMap = type switch
            {
                WorldType.FlatMicroBlocks or WorldType.FlatRegular => new FlatHeightMap(-10f),
                WorldType.HillyMicroBlocks or WorldType.HillyRegular => new DefaultHeightMap(seed),
                _ => throw new ArgumentOutOfRangeException(),
            };

            ChunkGeneratorType = type switch
            {
                WorldType.FlatMicroBlocks or WorldType.HillyMicroBlocks => typeof(CG02_MicroBlockChunkGenerator),
                WorldType.FlatRegular or WorldType.HillyRegular => typeof(CG04_WaveFunctionCollapseGeneratorV2),
                _ => throw new ArgumentOutOfRangeException(),
            };

            ColorFunc = type switch
            {
                WorldType.FlatMicroBlocks or WorldType.FlatRegular => ColorByChunk,
                WorldType.HillyMicroBlocks or WorldType.HillyRegular => ColorFromHeight,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public WorldChunk GetChunk(ChunkCoords chunkCoords)
        {
            IChunkGenerator chunkGenerator = (IChunkGenerator)Activator.CreateInstance(ChunkGeneratorType);
            return chunkGenerator.Generate(chunkCoords, HeightMap, ColorFunc);
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
