using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Clotzbergh.Server.ChunkGeneration
{
    public interface IChunkGenerator
    {
        WorldChunk Generate(ChunkCoords chunkCoords, IHeightMap heightMap, ColorFunction colorFunc);
    }

    public abstract class ChunkGenerator : IChunkGenerator
    {
        private static readonly object RandomCreationLock = new();

        private Random _random;

        private ChunkCoords _chunkCoords;

        private IHeightMap _heightMap;

        public ColorFunction _colorFunc;

        public virtual WorldChunk Generate(ChunkCoords chunkCoords, IHeightMap heightMap, ColorFunction colorFunc)
        {
            lock (RandomCreationLock) { _random = new(chunkCoords.X + chunkCoords.Y * 1000 + chunkCoords.Z * 1000000); }
            _chunkCoords = chunkCoords;
            _heightMap = heightMap;
            _colorFunc = colorFunc;

            return InnerGenerate();
        }

        protected abstract WorldChunk InnerGenerate();

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

        protected ChunkCoords ChunkCoords { get => _chunkCoords; }

        protected float HeightAt(int x, int y)
        {
            return _heightMap.At(x, y);
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
        protected bool NextRandomCoinFlip()
        {
            return _random.Next() % 2 == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        protected TList NextRandomElement<TList>(IReadOnlyList<TList> list)
        {
            return list[_random.Next(0, list.Count)];
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
            double randomValue = _random.NextDouble();

            // Calculate the weight for the geometric distribution
            double maxWeight = Math.Pow(biasFactor, n);
            double weightedRandom = randomValue * (1 - maxWeight);
            double logValue = Math.Log(1 - weightedRandom);
            int biasedIndex = (int)(logValue / Math.Log(biasFactor));

            return list[Math.Min(biasedIndex, n - 1)];
        }
    }

    public enum GeneralVoxelType
    {
        Air, Ground, AirOrGround
    }

    public abstract class VoxelChunkGenerator : ChunkGenerator
    {
        private readonly WorldChunk _chunk;
        private readonly GeneralVoxelType[,,] _generalTypeArray;
        private readonly bool[,,] _isCompletedArray;

        private readonly List<RelKlotzCoords> _nonCompleted;

        public VoxelChunkGenerator(bool trackNonCompleted = true)
        {
            _chunk = new WorldChunk();
            _generalTypeArray = new GeneralVoxelType[WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
            _isCompletedArray = new bool[WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
            _nonCompleted = trackNonCompleted ? new() : null;
        }

        protected IReadOnlyList<RelKlotzCoords> NonCompleted => _nonCompleted;

        protected bool IsCompletedAt(int x, int y, int z)
        {
            return _isCompletedArray[x, y, z];
        }

        protected bool IsCompletedAt(RelKlotzCoords coords)
        {
            return _isCompletedArray[coords.X, coords.Y, coords.Z];
        }

        protected bool SetCompletedAt(int x, int y, int z)
        {
            return _isCompletedArray[x, y, z] = true;
        }

        protected bool SetCompletedAt(RelKlotzCoords coords)
        {
            return _isCompletedArray[coords.X, coords.Y, coords.Z] = true;
        }

        protected SubKlotz SubKlotzAt(int x, int y, int z)
        {
            return _chunk.Get(x, y, z);
        }

        protected SubKlotz SubKlotzAt(RelKlotzCoords coords)
        {
            return _chunk.Get(coords);
        }

        protected void PlaceGround()
        {
            for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
            {
                for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
                {
                    int x = ChunkCoords.X * WorldDef.ChunkSubDivsX + ix;
                    int z = ChunkCoords.Z * WorldDef.ChunkSubDivsZ + iz;
                    int groundStart = Mathf.RoundToInt(HeightAt(x, z) / WorldDef.SubKlotzSize.y);

                    for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                    {
                        int y = ChunkCoords.Y * WorldDef.ChunkSubDivsY + iy;
                        if (y > groundStart)
                        {
                            _generalTypeArray[ix, iy, iz] = GeneralVoxelType.Air;
                            OnGeneralVoxelTypeDecided(ix, iy, iz, GeneralVoxelType.Air);
                            SetCompletedAt(ix, iy, iz);
                        }
                        else if (y == groundStart)
                        {
                            _generalTypeArray[ix, iy, iz] = GeneralVoxelType.AirOrGround;
                            OnGeneralVoxelTypeDecided(ix, iy, iz, GeneralVoxelType.AirOrGround);
                            _nonCompleted?.Add(new(ix, iy, iz));
                        }
                        else
                        {
                            _generalTypeArray[ix, iy, iz] = GeneralVoxelType.Ground;
                            OnGeneralVoxelTypeDecided(ix, iy, iz, GeneralVoxelType.Ground);
                            _nonCompleted?.Add(new(ix, iy, iz));
                        }
                    }
                }
            }
        }

        protected virtual void OnGeneralVoxelTypeDecided(int x, int y, int z, GeneralVoxelType generalType) { }

        protected void PlaceAir(RelKlotzCoords coords)
        {
            _chunk.Set(coords, SubKlotz.Air);
            SetCompletedAt(coords);
            _nonCompleted?.Remove(coords);
        }

        protected void PlaceKlotz(RelKlotzCoords rootCoords, KlotzType type, KlotzDirection dir)
        {
            KlotzSize size = KlotzKB.Size(type);
            KlotzColor color = _colorFunc(
                ChunkCoords.X * WorldDef.ChunkSubDivs.x + rootCoords.X,
                ChunkCoords.Y * WorldDef.ChunkSubDivs.y + rootCoords.Y,
                ChunkCoords.Z * WorldDef.ChunkSubDivs.z + rootCoords.Z);
            KlotzVariant variant = NextRandVariant();

            for (int subZ = 0; subZ < size.Z; subZ++)
            {
                for (int subX = 0; subX < size.X; subX++)
                {
                    for (int subY = 0; subY < size.Y; subY++)
                    {
                        RelKlotzCoords coords = SubKlotz.TranslateSubIndexToCoords(
                            rootCoords, new(subX, subY, subZ), dir);

                        if (IsOutOfBounds(coords))
                        {
                            bool isFree = IsFreeToComplete(rootCoords, type, dir);
                            throw new ArgumentException($"Failed to place {type} at {rootCoords}. Out of bounds at {coords}. IsFree={isFree}.");
                        }

                        if (subX == 0 && subY == 0 && subZ == 0)
                        {
                            _chunk.Set(coords, SubKlotz.Root(type, color, variant, dir));
                            SetCompletedAt(coords);
                        }
                        else
                        {
                            _chunk.Set(coords, SubKlotz.NonRoot(type, dir, subX, subY, subZ));
                            SetCompletedAt(coords);
                        }

                        _nonCompleted?.Remove(coords);
                    }
                }
            }
        }

        protected void FillNonCompletedWith1x1Plates()
        {
            AbsKlotzCoords coords = ChunkCoords.AsBaseAbsKlotzCoords();

            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        if (IsCompletedAt(x, y, z))
                            continue;

                        _chunk.Set(x, y, z, SubKlotz.Root(
                            KlotzType.Plate1x1,
                            _colorFunc(coords.X + x, coords.Y + y, coords.Z + z),
                            NextRandVariant(),
                            KlotzDirection.ToPosX));
                    }
                }
            }
        }


        protected bool IsFreeToComplete(int x, int y, int z, KlotzType type, KlotzDirection dir)
        {
            return IsFreeToComplete(new(x, y, z), type, dir);
        }

        protected bool IsFreeToComplete(RelKlotzCoords root, KlotzType type, KlotzDirection dir)
        {
            KlotzSize size = KlotzKB.Size(type);

            for (int subZ = 0; subZ < size.Z; subZ++)
            {
                for (int subX = 0; subX < size.X; subX++)
                {
                    for (int subY = 0; subY < size.Y; subY++)
                    {
                        RelKlotzCoords coords = SubKlotz.TranslateSubIndexToCoords(
                            root, new(subX, subY, subZ), dir);

                        if (IsOutOfBounds(coords))
                            return false;

                        if (IsCompletedAt(coords))
                            return false;
                    }
                }
            }

            return true;
        }

        protected WorldChunk ToWorldChunk()
        {
            return _chunk;
        }
    }
}
