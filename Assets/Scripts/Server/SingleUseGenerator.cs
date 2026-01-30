using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Clotzbergh.Server.ChunkGeneration
{
    public abstract class SingleUseGenerator
    {
        private static readonly object RandomCreationLock = new();

        private Random Random { get; set; }

        protected ChunkCoords ChunkCoords { get; private set; }

        public void Initialize(ChunkCoords chunkCoords)
        {
            lock (RandomCreationLock) { Random = new(chunkCoords.X + chunkCoords.Y * 1000 + chunkCoords.Z * 1000000); }
            ChunkCoords = chunkCoords;
        }

        protected bool IsOutOfBounds(RelKlotzCoords coords)
        {
            return IsOutOfBounds(coords.X, coords.Y, coords.Z);
        }

        protected bool IsOutOfBounds(int x, int y, int z)
        {
            return
                x < 0 || y < 0 || z < 0 ||
                x >= WorldDef.ChunkSubDivsX ||
                y >= WorldDef.ChunkSubDivsY ||
                z >= WorldDef.ChunkSubDivsZ;
        }

        /// <summary>
        /// 
        /// </summary>
        protected KlotzColor NextRandColor()
        {
            return (KlotzColor)Random.Next(0, (int)KlotzColor.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        protected KlotzDirection NextRandDirection()
        {
            return (KlotzDirection)Random.Next(0, (int)KlotzDirection.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        protected KlotzVariant NextRandVariant()
        {
            return (KlotzVariant)(uint)Random.Next(0, KlotzVariant.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        protected bool NextRandomCoinFlip()
        {
            return Random.Next() % 2 == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        protected TList NextRandomElement<TList>(IReadOnlyList<TList> list)
        {
            return list[Random.Next(0, list.Count)];
        }

        /// <summary>
        /// Biased random selection using geometric distribution.
        /// Adjust the <c>biasFactor</c> to control the bias.
        /// </summary>
        protected TList NextRandomElementBiased<TList>(IReadOnlyList<TList> list, double biasFactor = 0.5)
        {
            if (list.Count == 0)
                throw new ArgumentException("list is empty");

            int n = list.Count;
            double randomValue = Random.NextDouble();

            // Calculate the weight for the geometric distribution
            double maxWeight = Math.Pow(biasFactor, n);
            double weightedRandom = randomValue * (1 - maxWeight);
            double logValue = Math.Log(1 - weightedRandom);
            int biasedIndex = (int)(logValue / Math.Log(biasFactor));

            return list[Math.Min(biasedIndex, n - 1)];
        }

        /// <summary>
        /// 
        /// </summary>
        protected (int, int) NextRandRelCoordsXZ(int paddingX = 0, int paddingZ = 0)
        {
            return (
                Random.Next(paddingX, WorldDef.ChunkSubDivsX - paddingX),
                Random.Next(paddingZ, WorldDef.ChunkSubDivsZ - paddingZ)
            );
        }
    }

    public class GeneratorFactory<TAbstract> where TAbstract : SingleUseGenerator
    {
        private readonly Type _concreteType;

        public GeneratorFactory(Type concreteType)
        {
            /*
            if (!typeof(TAbstract).IsAssignableFrom(concreteType))
                throw new ArgumentException($"Type {concreteType.FullName} is not a subtype of {typeof(TAbstract).FullName}");

            if (concreteType.IsAbstract)
                throw new ArgumentException($"Type {concreteType.FullName} is abstract and cannot be instantiated");
            */

            _concreteType = concreteType;
        }

        public TAbstract CreateGenerator(ChunkCoords chunkCoords)
        {
            TAbstract generator = (TAbstract)Activator.CreateInstance(_concreteType);
            generator.Initialize(chunkCoords);
            return generator;
        }
    }
}

