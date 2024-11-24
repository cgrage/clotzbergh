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
            //WG01_TrivialWorldGenerator gen = new();
            WG02_MicroBlockWorldGenerator gen = new();
            // WaveFunctionCollapseGenerator gen = new();

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

    public class ChunkGenVoxel
    {
        private readonly GeneralVoxelType _generalType;
        private SubKlotz? _collapsedType;

        public ChunkGenVoxel(GeneralVoxelType generalType)
        {
            _generalType = generalType;
            _collapsedType = null;

            if (generalType == GeneralVoxelType.Air)
            {
                _collapsedType = SubKlotz.Air;
            }
        }

        public GeneralVoxelType GeneralType => _generalType;

        public bool IsAir => _collapsedType?.Type == KlotzType.Air;

        public bool IsCollapsed => _collapsedType.HasValue;

        public SubKlotz CollapsedType => _collapsedType.Value;

        public void SetCollapsedType(SubKlotz value)
        {
            _collapsedType = value;
        }
    }

    public abstract class VoxelChunkGenerator<T> : ChunkGenerator where T : ChunkGenVoxel
    {
        private T[,,] _positions;

        private List<Vector3Int> _nonCollapsed;

        public VoxelChunkGenerator()
        {
            _nonCollapsed = new();
            _positions = new T[
                WorldDef.ChunkSubDivsX,
                WorldDef.ChunkSubDivsY,
                WorldDef.ChunkSubDivsZ];

            _nonCollapsed = new();
            _positions = new T[
                WorldDef.ChunkSubDivsX,
                WorldDef.ChunkSubDivsY,
                WorldDef.ChunkSubDivsZ];
        }

        protected IReadOnlyList<Vector3Int> NonCollapsed => _nonCollapsed;

        protected abstract T CreateVoxel(GeneralVoxelType voxelType);

        protected T AtPosition(int x, int y, int z)
        {
            return _positions[x, y, z];
        }

        protected T AtPosition(Vector3Int coords)
        {
            return _positions[coords.x, coords.y, coords.z];
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
                            _positions[ix, iy, iz] = CreateVoxel(GeneralVoxelType.Air);
                        }
                        else if (y == groundStart)
                        {
                            _positions[ix, iy, iz] = CreateVoxel(GeneralVoxelType.AirOrGround);
                            _nonCollapsed.Add(new(ix, iy, iz));
                        }
                        else
                        {
                            _positions[ix, iy, iz] = CreateVoxel(GeneralVoxelType.Ground);
                            _nonCollapsed.Add(new(ix, iy, iz));
                        }
                    }
                }
            }
        }

        protected void PlaceAir(Vector3Int coords)
        {
            _positions[coords.x, coords.y, coords.z].SetCollapsedType(SubKlotz.Air);
            _nonCollapsed.Remove(coords);
        }

        protected void PlaceKlotz(Vector3Int rootCoords, KlotzType type, KlotzDirection dir)
        {
            Vector3Int size = KlotzKB.KlotzSize(type);
            KlotzColor color = ColorFromHeight(ChunkCoords.y * WorldDef.ChunkSubDivsY + rootCoords.y);
            KlotzVariant variant = NextRandVariant();

            for (int subZ = 0; subZ < size.z; subZ++)
            {
                for (int subX = 0; subX < size.x; subX++)
                {
                    for (int subY = 0; subY < size.y; subY++)
                    {
                        Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
                            rootCoords, new(subX, subY, subZ), dir);

                        if (subX == 0 && subY == 0 && subZ == 0)
                        {
                            _positions[coords.x, coords.y, coords.z].SetCollapsedType(SubKlotz.Root(type, color, variant, dir));
                        }
                        else
                        {
                            _positions[coords.x, coords.y, coords.z].SetCollapsedType(SubKlotz.NonRoot(type, dir, subX, subY, subZ));
                        }

                        _nonCollapsed.Remove(coords);
                    }
                }
            }
        }

        protected void FillNonCollapsedWith1x1Plates()
        {
            int baseHeight = ChunkCoords.y * WorldDef.ChunkSubDivsY;

            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        if (_positions[x, y, z].IsCollapsed)
                            continue;

                        _positions[x, y, z].SetCollapsedType(SubKlotz.Root(
                            KlotzType.Plate1x1,
                            ColorFromHeight(baseHeight + y),
                            NextRandVariant(),
                            KlotzDirection.ToPosX));
                    }
                }
            }
        }


        protected bool IsPossible(int x, int y, int z, KlotzType type, KlotzDirection dir)
        {
            return IsPossible(new(x, y, z), type, dir);
        }

        protected bool IsPossible(Vector3Int root, KlotzType type, KlotzDirection dir)
        {
            Vector3Int size = KlotzKB.KlotzSize(type);

            for (int subZ = 0; subZ < size.z; subZ++)
            {
                for (int subX = 0; subX < size.x; subX++)
                {
                    for (int subY = 0; subY < size.y; subY++)
                    {
                        Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
                            root, new(subX, subY, subZ), dir);

                        if (IsOutOfBounds(coords))
                            return false;

                        if (_positions[coords.x, coords.y, coords.z].IsCollapsed)
                            return false;
                    }
                }
            }

            return true;
        }

        protected WorldChunk ToWorldChunk()
        {
            WorldChunk chunk = new();

            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        chunk.Set(x, y, z, _positions[x, y, z].CollapsedType);
                    }
                }
            }

            return chunk;
        }
    }
}
