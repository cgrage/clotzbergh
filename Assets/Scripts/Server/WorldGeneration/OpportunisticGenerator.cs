using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public class OpportunisticGenerator : ChunkGenerator
    {
        private Voxel[,,] _positions;
        private List<Vector3Int> _nonCollapsed;

        private const int Range = 4;

        public struct Voxel
        {
            private readonly GeneralVoxelType _generalType;
            private int _score;
            private SubKlotz? _collapsedType;

            public Voxel(GeneralVoxelType generalType)
            {
                _generalType = generalType;
                _score = 0;
                _collapsedType = null;

                if (generalType == GeneralVoxelType.Air)
                {
                    _collapsedType = SubKlotz.Air;
                }
            }

            public readonly GeneralVoxelType GeneralType => _generalType;

            public readonly int Score => _score;

            public readonly bool IsAir => _collapsedType?.Type == KlotzType.Air;

            public readonly bool IsCollapsed => _collapsedType.HasValue;

            public readonly SubKlotz CollapsedType => _collapsedType.Value;

            public void SetScore(int score)
            {
                _score = score;
            }

            public void SetCollapsedType(SubKlotz value)
            {
                _collapsedType = value;
            }

            public override string ToString()
            {
                if (_collapsedType.HasValue) { return $"Collapsed to: {_collapsedType.Value}"; }
                else { return $"Voxel with score of {_score}"; }
            }
        }

        public override WorldChunk InnerGenerate()
        {
            _nonCollapsed = new();
            _positions = new Voxel[
                WorldDef.ChunkSubDivsX,
                WorldDef.ChunkSubDivsY,
                WorldDef.ChunkSubDivsZ];

            PlaceGround();
            RecalculateAllScores();

            foreach (var type in WorldGenDefs.AllGroundTypesSortedByVolumeDesc)
            {
                int failCount = 0;

                _nonCollapsed.Sort((a, b) =>
                    _positions[a.x, a.y, a.z].Score.CompareTo(
                    _positions[b.x, b.y, b.z].Score));

                while (failCount < 3 && _nonCollapsed.Count > 0)
                {
                    Vector3Int coords = NextRandomElement(_nonCollapsed);
                    KlotzDirection dir = NextRandDirection();
                    bool possible = IsPossible(coords, type, dir);

                    if (possible)
                    {
                        PlaceKlotz(coords, type, dir);
                        failCount = 0;
                    }
                    else
                    {
                        failCount++;
                    }
                }
            }

            FillNonCollapsedWith1x1Plates();
            return ToWorldChunk();
        }

        private void PlaceGround()
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
                            _positions[ix, iy, iz] = new Voxel(GeneralVoxelType.Air);
                        }
                        else if (y == groundStart)
                        {
                            _positions[ix, iy, iz] = new Voxel(GeneralVoxelType.AirOrGround);

                        }
                        else
                        {
                            _positions[ix, iy, iz] = new Voxel(GeneralVoxelType.Ground);
                        }
                    }
                }
            }
        }

        private void RecalculateAllScores()
        {
            _nonCollapsed.Clear();

            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        int score = CalculateScoreOfPos(x, y, z);
                        if (score <= 0)
                            continue;

                        _positions[x, y, z].SetScore(score);
                        _nonCollapsed.Add(new(x, y, z));
                    }
                }
            }
        }

        private int CalculateScoreOfPos(int x, int y, int z)
        {
            if (IsOutOfBounds(x, y, z))
                return -1;

            Voxel voxel = _positions[x, y, z];
            if (voxel.IsCollapsed)
                return -1;

            int score = 0;

            for (int iz = z - (Range - 1); iz < z + Range; iz++)
            {
                for (int ix = x - (Range - 1); ix < x + Range; ix++)
                {
                    for (int iy = y - (Range - 1); iy < y + Range; iy++)
                    {
                        if (IsOutOfBounds(ix, iy, iz))
                            continue;

                        if (!_positions[ix, iy, iz].IsCollapsed)
                            score++;
                    }
                }
            }

            return score;
        }

        private bool IsPossible(Vector3Int root, KlotzType type, KlotzDirection dir)
        {
            Vector3Int size = KlotzKB.KlotzSize(type);

            for (int subZ = 0; subZ < size.z; subZ++)
            {
                for (int subX = 0; subX < size.x; subX++)
                {
                    for (int subY = 0; subY < size.y; subY++)
                    {
                        Vector3Int realCoords = SubKlotz.TranslateSubIndexToCoords(
                            root, new(subX, subY, subZ), dir);

                        if (IsOutOfBounds(realCoords))
                            return false;

                        if (_positions[realCoords.x, realCoords.y, realCoords.z].IsCollapsed)
                            return false;
                    }
                }
            }

            return true;
        }

        void PlaceKlotz(Vector3Int rootCoords, KlotzType type, KlotzDirection dir)
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

        private void FillNonCollapsedWith1x1Plates()
        {
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
                            ColorFromHeight(y),
                            NextRandVariant(),
                            KlotzDirection.ToPosX));
                    }
                }
            }
        }

        private WorldChunk ToWorldChunk()
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
