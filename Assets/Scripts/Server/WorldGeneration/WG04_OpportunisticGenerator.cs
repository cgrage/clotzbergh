using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public class WG04_Voxel : ChunkGenVoxel
    {
        private int _score;

        public WG04_Voxel(GeneralVoxelType generalType) : base(generalType)
        {
            _score = 0;
        }

        public int Score => _score;

        public void SetScore(int score)
        {
            _score = score;
        }
    }

    public class WG04_OpportunisticGenerator : VoxelChunkGenerator<WG04_Voxel>
    {
        private const int Range = 4;

        protected override WG04_Voxel CreateVoxel(GeneralVoxelType voxelType)
        {
            return new WG04_Voxel(voxelType);
        }

        protected override WorldChunk InnerGenerate()
        {
            PlaceGround();
            RecalculateAllScores();

            foreach (var type in WorldGenDefs.AllGroundTypesSortedByVolumeDesc)
            {
                int failCount = 0;
                while (failCount < 3 && NonCollapsed.Count > 0)
                {
                    Vector3Int coords = NextRandomElement(NonCollapsed);
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

        private void RecalculateAllScores()
        {
            foreach (var pos in NonCollapsed)
            {
                RecalculateScoreOfPos(pos);
            }
        }

        private void RecalculateScoreOfPos(Vector3Int pos)
        {
            WG04_Voxel voxel = AtPosition(pos);
            int score = 0;

            for (int iz = pos.z - (Range - 1); iz < pos.z + Range; iz++)
            {
                for (int ix = pos.x - (Range - 1); ix < pos.x + Range; ix++)
                {
                    for (int iy = pos.y - (Range - 1); iy < pos.y + Range; iy++)
                    {
                        if (IsOutOfBounds(ix, iy, iz))
                            continue;

                        if (!AtPosition(ix, iy, iz).IsCollapsed)
                            score++;
                    }
                }
            }

            voxel.SetScore(score);
        }
    }
}
