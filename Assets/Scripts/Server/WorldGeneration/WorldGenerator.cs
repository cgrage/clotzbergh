using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Clotzbergh.Server.WorldGeneration
{
    public class WorldGenerator
    {
        protected IHeightMap HeightMap { get; } = new DefaultHeightMap();

        public WorldChunk GetChunk(Vector3Int chunkCoords)
        {
            // ChunkGenerator gen = new MicroBlockWorldGenerator();
            ChunkGenerator gen = new WaveFunctionCollapseGenerator();

            return gen.Generate(chunkCoords, HeightMap);
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
    }

    public abstract class ChunkGenerator
    {
        private static readonly object RandomCreationLock = new();

        private Random _random;

        private Vector3Int _chunkCoords;

        private IHeightMap _heightMap;

        public virtual WorldChunk Generate(Vector3Int chunkCoords, IHeightMap heightMap)
        {
            lock (RandomCreationLock) { _random = new(chunkCoords.x + chunkCoords.y * 1000 + chunkCoords.z * 1000000); }
            _chunkCoords = chunkCoords;
            _heightMap = heightMap;

            return InnerGenerate();
        }

        public abstract WorldChunk InnerGenerate();

        protected bool IsOutOfBounds(Vector3Int coords)
        {
            return IsOutOfBounds(coords.x, coords.y, coords.z);
        }

        protected bool IsOutOfBounds(int x, int y, int z)
        {
            return
                x < 0 || y < 0 || z < 0 ||
                x >= WorldDef.ChunkSubDivsX ||
                y >= WorldDef.ChunkSubDivsY ||
                z >= WorldDef.ChunkSubDivsZ;
        }

        protected Vector3Int ChunkCoords { get => _chunkCoords; }

        protected float HeightAt(int x, int y)
        {
            return _heightMap.At(x, y);
        }

        /// <summary>
        /// 
        /// </summary>
        protected KlotzColor ColorFromHeight(int y)
        {
            if (y < -85) return KlotzColor.DarkBlue;
            if (y < -80) return KlotzColor.Azure;
            if (y < -70) return KlotzColor.Yellow;
            if (y < -20) return KlotzColor.DarkGreen;
            if (y < 30) return KlotzColor.DarkBrown;
            if (y < 70) return KlotzColor.Gray;
            return KlotzColor.White;
        }

        /// <summary>
        /// 
        /// </summary>
        protected KlotzColor NextRandColor()
        {
            return (KlotzColor)_random.Next(0, (int)KlotzColor.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        protected KlotzDirection NextRandDirection()
        {
            return (KlotzDirection)_random.Next(0, (int)KlotzDirection.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        protected KlotzVariant NextRandVariant()
        {
            return (KlotzVariant)(uint)_random.Next(0, KlotzVariant.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool NextRandomCoinFlip()
        {
            return _random.Next() % 2 == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public T NextRandomElement<T>(List<T> list)
        {
            return list[_random.Next(0, list.Count)];
        }

        /// <summary>
        /// Biased random selection using geometric distribution.
        /// Adjust the <c>biasFactor</c> to control the bias.
        /// </summary>
        public T NextRandomElementBiased<T>(List<T> list, double biasFactor = 0.5)
        {
            if (list.Count == 0)
                throw new ArgumentException("list is empty");

            int n = list.Count;
            double randomValue = _random.NextDouble();

            // Calculate the weight for the geometric distribution
            double maxWeight = Math.Pow(biasFactor, n);
            double weightedRandom = randomValue * (1 - maxWeight);
            double logValue = Math.Log(1 - weightedRandom);
            int biasedIndex = (int)(logValue / Math.Log(biasFactor));

            return list[Math.Min(biasedIndex, n - 1)];
        }
    }
}
