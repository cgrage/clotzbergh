using System.Collections.Generic;
using Clotzbergh.Server.ChunkGeneration;
using UnityEngine;

namespace Clotzbergh.Server.StructureGeneration
{
    public abstract class StructureGenerator : SingleUseGenerator
    {
        public virtual IGenerationModifier GenModifier => null;
        public abstract void PopulateStructures(WorldChunk chunk);
    }

    public class NoStructureGenerator : StructureGenerator
    {
        public override void PopulateStructures(WorldChunk chunk) { }
    }

    public class SimpleCentralHouseGenerator : StructureGenerator, IGenerationModifier
    {
        private readonly struct HouseDesc
        {
            public const int RoofInset = 1;
            public const int HouseInset = 1;
            public const int RoofSlope = 3;

            public readonly Vector2Int AreaLocationXZ { get; }
            public readonly Vector2Int AreaSizeXZ { get; }
            public readonly int LocationY { get; }
            public readonly Vector2Int HouseLocationXZ { get; }
            public readonly Vector2Int HouseSizeXZ { get; }
            public readonly Vector2Int RoofLocationXZ { get; }
            public readonly Vector2Int RoofSizeXZ { get; }
            public readonly int RoofHeight { get; }
            public readonly int BaseHeight { get; }
            public readonly int StoryHeight { get; }
            public readonly int StoryCount { get; }
            public readonly int TotalHeight { get; }
            public readonly BoundsInt TotalBounds { get; }

            public HouseDesc(Vector2Int locationXZ, Vector2Int maxSizeXZ, int locationY, int maxHeight)
            {
                AreaLocationXZ = locationXZ;
                AreaSizeXZ = maxSizeXZ;
                LocationY = locationY;

                RoofLocationXZ = new Vector2Int(AreaLocationXZ.x + RoofInset, AreaLocationXZ.y + RoofInset);
                RoofSizeXZ = new Vector2Int(AreaSizeXZ.x - 2 * RoofInset, AreaSizeXZ.y - 2 * RoofInset);

                HouseLocationXZ = new Vector2Int(RoofLocationXZ.x + HouseInset, RoofLocationXZ.y + HouseInset);
                HouseSizeXZ = new Vector2Int(RoofSizeXZ.x - 2 * HouseInset, RoofSizeXZ.y - 2 * HouseInset);

                BaseHeight = 1;
                StoryHeight = 3 * 5;
                RoofHeight = (RoofSizeXZ.x / 2) * RoofSlope;

                if (BaseHeight + RoofHeight < maxHeight)
                {
                    StoryCount = (maxHeight - BaseHeight - RoofHeight) / StoryHeight;
                }
                else
                {
                    RoofHeight = 0;
                    StoryCount = 0;
                }

                TotalHeight = BaseHeight + StoryCount * StoryHeight + RoofHeight;
                TotalBounds = new BoundsInt(new Vector3Int(AreaLocationXZ.x, LocationY, AreaLocationXZ.y), new Vector3Int(AreaSizeXZ.x, TotalHeight, AreaSizeXZ.y));
            }
        }

        private readonly List<HouseDesc> _destinations = new();

        public override IGenerationModifier GenModifier => this;

        public void OnBeforeGeneration(FieldResolver r)
        {
            for (int i = 0; i < 1; i++)
            {
                Vector3Int dimensions = new(18, 42, 18);

                Vector2Int sizeXZ = new(dimensions.x, dimensions.z);
                Vector2Int posXZ = NextRandRelCoordsXZ(sizeXZ);
                int y = r.GroundStartAtRelPos(posXZ.x + sizeXZ.x / 2, posXZ.y + sizeXZ.y / 2);
                int yRel = y - r.Coords.Y * WorldDef.ChunkSubDivsY;

                // yRel may be negative and out of bounds
                if (yRel < 0 || yRel >= WorldDef.ChunkSubDivsY - dimensions.y)
                    continue;

                HouseDesc coords = new(posXZ, sizeXZ, yRel, dimensions.y);
                if (!_destinations.Exists(dest => dest.TotalBounds.Intersects(coords.TotalBounds)))
                {
                    _destinations.Add(coords);
                    Debug.Log($"Placing house at chunk {ChunkCoords} relPos {posXZ.x},{yRel},{posXZ.y}");
                }
            }
        }

        public float OnHeightMapOverride(FieldResolver r, int absX, int absZ)
        {
            int relX = absX - r.Coords.X * WorldDef.ChunkSubDivsX;
            int relZ = absZ - r.Coords.Z * WorldDef.ChunkSubDivsZ;

            foreach (var dest in _destinations)
            {
                if (relX >= dest.AreaLocationXZ.x && relX < dest.AreaLocationXZ.x + dest.AreaSizeXZ.x &&
                    relZ >= dest.AreaLocationXZ.y && relZ < dest.AreaLocationXZ.y + dest.AreaSizeXZ.y)
                {
                    return r.HeightMap.At(
                        r.Coords.X * WorldDef.ChunkSubDivsX + dest.AreaLocationXZ.x + dest.AreaSizeXZ.x / 2,
                        r.Coords.Z * WorldDef.ChunkSubDivsZ + dest.AreaLocationXZ.y + dest.AreaSizeXZ.y / 2);
                }
            }

            return r.HeightMap.At(absX, absZ);
        }

        public override void PopulateStructures(WorldChunk chunk)
        {
            foreach (var dest in _destinations)
            {
                CreateBasePlate(chunk, dest);
                for (int storyIndex = 0; storyIndex < dest.StoryCount; storyIndex++)
                {
                    CreateStory(chunk, dest, storyIndex);
                }
                CreateRoof(chunk, dest);
            }
        }

        private void CreateBasePlate(WorldChunk chunk, HouseDesc dest)
        {
            int baseY = dest.LocationY;
            KlotzColor baseColor = KlotzColor.Brown;

            for (int dx = 0; dx < dest.AreaSizeXZ.x; dx++)
            {
                for (int dz = 0; dz < dest.AreaSizeXZ.y; dz++)
                {
                    for (int dy = 0; dy < dest.BaseHeight; dy++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            baseColor,
                            NextRandVariant(),
                            new RelKlotzCoords(dest.AreaLocationXZ.x + dx, dy + baseY, dest.AreaLocationXZ.y + dz),
                            KlotzDirection.ToPosX);
                    }
                }
            }
        }

        private void CreateStory(WorldChunk chunk, HouseDesc dest, int storyIndex)
        {
            int storyBaseY = dest.LocationY + dest.BaseHeight + storyIndex * dest.StoryHeight;
            KlotzColor wallColor = (storyIndex % 2 == 0) ? KlotzColor.White : KlotzColor.Yellow;

            for (int dx = 0; dx < dest.HouseSizeXZ.x; dx++)
            {
                for (int dz = 0; dz < dest.HouseSizeXZ.y; dz++)
                {
                    for (int dy = 0; dy < dest.StoryHeight; dy++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            wallColor,
                            NextRandVariant(),
                            new RelKlotzCoords(dest.HouseLocationXZ.x + dx, dy + storyBaseY, dest.HouseLocationXZ.y + dz),
                            KlotzDirection.ToPosX);
                    }
                }
            }
        }

        private void CreateRoof(WorldChunk chunk, HouseDesc dest)
        {
            int roofBaseY = dest.LocationY + dest.BaseHeight + dest.StoryCount * dest.StoryHeight;
            KlotzColor roofColor = KlotzColor.Red;

            for (int dx = 0; dx < dest.RoofSizeXZ.x / 2; dx++)
            {
                for (int dz = 0; dz < dest.RoofSizeXZ.y; dz++)
                {
                    for (int dy = 0; dy < (dx + 1) * HouseDesc.RoofSlope; dy++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            roofColor,
                            NextRandVariant(),
                            new RelKlotzCoords(dest.RoofLocationXZ.x + dx, dy + roofBaseY, dest.RoofLocationXZ.y + dz),
                            KlotzDirection.ToPosX);

                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            roofColor,
                            NextRandVariant(),
                            new RelKlotzCoords(dest.RoofLocationXZ.x + dest.RoofSizeXZ.x - 1 - dx, dy + roofBaseY, dest.RoofLocationXZ.y + dz),
                            KlotzDirection.ToPosX);
                    }
                }
            }
        }
    }
}
