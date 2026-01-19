using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Clotzbergh.Server.WorldGeneration
{
    public class WorldGenerator
    {
        private readonly WorldType _type;
        protected IHeightMap HeightMap { get; }

        public WorldGenerator(int seed, WorldType type = WorldType.HillyRegular)
        {
            _type = type;

            HeightMap = _type switch
            {
                WorldType.FlatMicroBlocks or WorldType.FlatRegular => new FlatHeightMap(-10f),
                WorldType.HillyMicroBlocks or WorldType.HillyRegular => new DefaultHeightMap(seed),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public WorldChunk GetChunk(Vector3Int chunkCoords)
        {
            IChunkGenerator gen = _type switch
            {
                WorldType.FlatMicroBlocks or WorldType.HillyMicroBlocks => new WG02_MicroBlockWorldGenerator(),
                WorldType.FlatRegular or WorldType.HillyRegular => new WG04_WaveFunctionCollapseGeneratorV2(),
                _ => throw new ArgumentOutOfRangeException(),
            };

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

    public interface IChunkGenerator
    {
        WorldChunk Generate(Vector3Int chunkCoords, IHeightMap heightMap);
    }

    public abstract class ChunkGenerator : IChunkGenerator
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

        protected abstract WorldChunk InnerGenerate();

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
        protected static KlotzColor ColorFromHeight(int y)
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

        private readonly List<Vector3Int> _nonCompleted;

        public VoxelChunkGenerator(bool trackNonCompleted = true)
        {
            _chunk = new WorldChunk();
            _generalTypeArray = new GeneralVoxelType[WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
            _isCompletedArray = new bool[WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
            _nonCompleted = trackNonCompleted ? new() : null;
        }

        protected IReadOnlyList<Vector3Int> NonCompleted => _nonCompleted;

        protected bool IsCompletedAt(int x, int y, int z)
        {
            return _isCompletedArray[x, y, z];
        }

        protected bool IsCompletedAt(Vector3Int coords)
        {
            return _isCompletedArray[coords.x, coords.y, coords.z];
        }

        protected bool SetCompletedAt(int x, int y, int z)
        {
            return _isCompletedArray[x, y, z] = true;
        }

        protected bool SetCompletedAt(Vector3Int coords)
        {
            return _isCompletedArray[coords.x, coords.y, coords.z] = true;
        }

        protected SubKlotz SubKlotzAt(int x, int y, int z)
        {
            return _chunk.Get(x, y, z);
        }

        protected SubKlotz SubKlotzAt(Vector3Int coords)
        {
            return _chunk.Get(coords);
        }

        protected void PlaceGround()
        {
            for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
            {
                for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
                {
                    int x = ChunkCoords.x * WorldDef.ChunkSubDivsX + ix;
                    int z = ChunkCoords.z * WorldDef.ChunkSubDivsZ + iz;
                    int groundStart = Mathf.RoundToInt(HeightAt(x, z) / WorldDef.SubKlotzSize.y);

                    for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                    {
                        int y = ChunkCoords.y * WorldDef.ChunkSubDivsY + iy;
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

        protected void PlaceAir(Vector3Int coords)
        {
            _chunk.Set(coords, SubKlotz.Air);
            SetCompletedAt(coords);
            _nonCompleted?.Remove(coords);
        }

        protected void PlaceKlotz(Vector3Int rootCoords, KlotzType type, KlotzDirection dir)
        {
            KlotzSize size = KlotzKB.Size(type);
            KlotzColor color = ColorFromHeight(ChunkCoords.y * WorldDef.ChunkSubDivsY + rootCoords.y);
            KlotzVariant variant = NextRandVariant();

            for (int subZ = 0; subZ < size.Z; subZ++)
            {
                for (int subX = 0; subX < size.X; subX++)
                {
                    for (int subY = 0; subY < size.Y; subY++)
                    {
                        Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
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
            int baseHeight = ChunkCoords.y * WorldDef.ChunkSubDivsY;

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
                            ColorFromHeight(baseHeight + y),
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

        protected bool IsFreeToComplete(Vector3Int root, KlotzType type, KlotzDirection dir)
        {
            KlotzSize size = KlotzKB.Size(type);

            for (int subZ = 0; subZ < size.Z; subZ++)
            {
                for (int subX = 0; subX < size.X; subX++)
                {
                    for (int subY = 0; subY < size.Y; subY++)
                    {
                        Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
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
