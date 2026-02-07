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
        readonly struct SimpleHouseDestination
        {
            public readonly Vector2Int LocationXZ { get; }
            public readonly Vector2Int SizeXZ { get; }
            public readonly int LocationY { get; }
            public readonly int RoofHeight { get; }
            public readonly int BaseHeight { get; }
            public readonly int StoryHeight { get; }
            public readonly int StoryCount { get; }
            public readonly int TotalHeight { get; }
            public readonly BoundsInt Bounds { get; }

            public SimpleHouseDestination(Vector2Int locationXZ, Vector2Int maxSizeXZ, int locationY, int maxHeight)
            {
                LocationXZ = locationXZ;
                SizeXZ = maxSizeXZ;
                LocationY = locationY;

                if (maxHeight < 15)
                {
                    BaseHeight = 1;
                    RoofHeight = 0;
                    StoryHeight = 0;
                    StoryCount = 0;
                }
                else
                {
                    BaseHeight = 1;
                    RoofHeight = 3 * 4;
                    StoryHeight = 3 * 5;
                    StoryCount = (maxHeight - BaseHeight - RoofHeight) / StoryHeight;
                }

                TotalHeight = BaseHeight + StoryCount * StoryHeight + RoofHeight;
                Bounds = new BoundsInt(new Vector3Int(LocationXZ.x, LocationY, LocationXZ.y), new Vector3Int(SizeXZ.x, TotalHeight, SizeXZ.y));
            }
        }

        private readonly List<SimpleHouseDestination> _destinations = new();

        public override IGenerationModifier GenModifier => this;

        public void OnBeforeGeneration(FieldResolver r)
        {
            for (int i = 0; i < 1; i++)
            {
                Vector3Int dimensions = new(18, 32, 18);

                Vector2Int sizeXZ = new(dimensions.x, dimensions.z);
                Vector2Int posXZ = NextRandRelCoordsXZ(sizeXZ);
                int y = r.GroundStartAtRelPos(posXZ.x + sizeXZ.x / 2, posXZ.y + sizeXZ.y / 2);
                int yRel = y - r.Coords.Y * WorldDef.ChunkSubDivsY;

                // yRel may be negative and out of bounds
                if (yRel < 0 || yRel >= WorldDef.ChunkSubDivsY - dimensions.y)
                    continue;

                SimpleHouseDestination coords = new(posXZ, sizeXZ, yRel, dimensions.y);
                if (!_destinations.Exists(dest => dest.Bounds.Intersects(coords.Bounds)))
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
                if (relX >= dest.LocationXZ.x && relX < dest.LocationXZ.x + dest.SizeXZ.x &&
                    relZ >= dest.LocationXZ.y && relZ < dest.LocationXZ.y + dest.SizeXZ.y)
                {
                    return r.HeightMap.At(
                        r.Coords.X * WorldDef.ChunkSubDivsX + dest.LocationXZ.x + dest.SizeXZ.x / 2,
                        r.Coords.Z * WorldDef.ChunkSubDivsZ + dest.LocationXZ.y + dest.SizeXZ.y / 2);
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

        private void CreateBasePlate(WorldChunk chunk, SimpleHouseDestination dest)
        {
            int baseY = dest.LocationY;
            KlotzColor baseColor = KlotzColor.Brown;

            for (int x = dest.LocationXZ.x; x < dest.LocationXZ.x + dest.SizeXZ.x; x++)
            {
                for (int z = dest.LocationXZ.y; z < dest.LocationXZ.y + dest.SizeXZ.y; z++)
                {
                    for (int y = baseY; y < baseY + dest.BaseHeight; y++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            baseColor,
                            NextRandVariant(),
                            new RelKlotzCoords(x, y, z),
                            KlotzDirection.ToPosX);
                    }
                }
            }
        }

        private void CreateStory(WorldChunk chunk, SimpleHouseDestination dest, int storyIndex)
        {
            int storyBaseY = dest.LocationY + dest.BaseHeight + storyIndex * dest.StoryHeight;
            KlotzColor wallColor = (storyIndex % 2 == 0) ? KlotzColor.White : KlotzColor.Yellow;

            for (int x = dest.LocationXZ.x; x < dest.LocationXZ.x + dest.SizeXZ.x; x++)
            {
                for (int z = dest.LocationXZ.y; z < dest.LocationXZ.y + dest.SizeXZ.y; z++)
                {
                    for (int y = storyBaseY; y < storyBaseY + dest.StoryHeight; y++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            wallColor,
                            NextRandVariant(),
                            new RelKlotzCoords(x, y, z),
                            KlotzDirection.ToPosX);
                    }
                }
            }
        }

        private void CreateRoof(WorldChunk chunk, SimpleHouseDestination dest)
        {
            int roofBaseY = dest.LocationY + dest.BaseHeight + dest.StoryCount * dest.StoryHeight;
            KlotzColor roofColor = KlotzColor.Red;

            for (int x = dest.LocationXZ.x; x < dest.LocationXZ.x + dest.SizeXZ.x; x++)
            {
                for (int z = dest.LocationXZ.y; z < dest.LocationXZ.y + dest.SizeXZ.y; z++)
                {
                    for (int y = roofBaseY; y < roofBaseY + dest.RoofHeight; y++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            roofColor,
                            NextRandVariant(),
                            new RelKlotzCoords(x, y, z),
                            KlotzDirection.ToPosX);
                    }
                }
            }
        }
    }
}
