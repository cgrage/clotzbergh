using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public class WG05_OpportunisticGenerator : VoxelChunkGenerator
    {
        private const int Range = 4;

        private readonly int[,,] _scoresArray;

        public WG05_OpportunisticGenerator()
        {
            _scoresArray = new int[WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
        }

        public int ScoreAt(int x, int y, int z)
        {
            return _scoresArray[x, y, z];
        }

        public int ScoreAt(Vector3Int coords)
        {
            return _scoresArray[coords.x, coords.y, coords.z];
        }

        public void SetScoreAt(Vector3Int coords, int score)
        {
            _scoresArray[coords.x, coords.y, coords.z] = score;
        }

        protected override WorldChunk InnerGenerate()
        {
            PlaceGround();
            RecalculateAllScores();

            foreach (var type in GroundDefinitions.NiceGroundTypesSortedByVolumeDesc)
            {
                int failCount = 0;
                while (failCount < 3 && NonCompleted.Count > 0)
                {
                    Vector3Int coords = NextRandomElement(NonCompleted);
                    KlotzDirection dir = NextRandDirection();
                    bool possible = IsFreeToComplete(coords, type, dir);

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

            FillNonCompletedWith1x1Plates();
            return ToWorldChunk();
        }

        private void RecalculateAllScores()
        {
            foreach (var pos in NonCompleted)
            {
                RecalculateScoreOfPos(pos);
            }
        }

        private void RecalculateScoreOfPos(Vector3Int pos)
        {
            int score = 0;

            for (int iz = pos.z - (Range - 1); iz < pos.z + Range; iz++)
            {
                for (int ix = pos.x - (Range - 1); ix < pos.x + Range; ix++)
                {
                    for (int iy = pos.y - (Range - 1); iy < pos.y + Range; iy++)
                    {
                        if (IsOutOfBounds(ix, iy, iz))
                            continue;

                        if (!IsCompletedAt(ix, iy, iz))
                            score++;
                    }
                }
            }

            SetScoreAt(pos, score);
        }
    }
}
