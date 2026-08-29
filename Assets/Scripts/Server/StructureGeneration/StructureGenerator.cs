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
        private readonly List<PlotFloorPlan> _destinations = new();

        public override IGenerationModifier GenModifier => this;

        public void OnBeforeGeneration(FieldResolver r)
        {
            for (int i = 0; i < 1; i++)
            {
                Vector3Int dimensions = new(24, 60, 24);

                Vector2Int sizeXZ = new(dimensions.x, dimensions.z);
                Vector2Int posXZ = NextRandRelCoordsXZ(sizeXZ);
                int y = r.GroundStartAtRelPos(posXZ.x + sizeXZ.x / 2, posXZ.y + sizeXZ.y / 2);
                int yRel = y - r.Coords.Y * WorldDef.ChunkSubDivsY;

                // yRel may be negative and out of bounds
                if (yRel < 0 || yRel >= WorldDef.ChunkSubDivsY - dimensions.y)
                    continue;

                PlotFloorPlan coords = new(new RectInt(posXZ, sizeXZ), yRel, dimensions.y);
                if (!_destinations.Exists(dest => dest.TotalBounds.Intersects(coords.TotalBounds)))
                {
                    _destinations.Add(coords);
                    // Debug.Log($"Placing house at chunk {ChunkCoords} relPos {posXZ.x},{yRel},{posXZ.y}");
                }
            }
        }

        public float OnHeightMapOverride(FieldResolver r, int absX, int absZ)
        {
            int relX = absX - r.Coords.X * WorldDef.ChunkSubDivsX;
            int relZ = absZ - r.Coords.Z * WorldDef.ChunkSubDivsZ;

            foreach (var dest in _destinations)
            {
                if (dest.PlotLocation.Contains(new Vector2Int(relX, relZ)))
                {
                    return r.HeightMap.At(
                        r.Coords.X * WorldDef.ChunkSubDivsX + dest.PlotLocation.x + dest.PlotLocation.width / 2,
                        r.Coords.Z * WorldDef.ChunkSubDivsZ + dest.PlotLocation.y + dest.PlotLocation.height / 2);
                }
            }

            return r.HeightMap.At(absX, absZ);
        }

        public override void PopulateStructures(WorldChunk chunk)
        {
            foreach (var dest in _destinations)
            {
                RenderBasePlate(chunk, dest);
                for (int storyIndex = 0; storyIndex < dest.StoryCount; storyIndex++)
                {
                    RenderStory(chunk, dest, storyIndex);
                }
                RenderRoof(chunk, dest);
                RenderGarden(chunk, dest);
            }
        }

        private void RenderBasePlate(WorldChunk chunk, PlotFloorPlan dest)
        {
            int baseY = dest.LocationY;
            KlotzColor baseColor = KlotzColor.Brown;

            for (int dx = 0; dx < dest.PlotLocation.width; dx++)
            {
                for (int dz = 0; dz < dest.PlotLocation.height; dz++)
                {
                    for (int dy = 0; dy < dest.BaseHeight; dy++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            baseColor,
                            NextRandVariant(),
                            new RelKlotzCoords(dest.PlotLocation.x + dx, dy + baseY, dest.PlotLocation.y + dz),
                            KlotzDirection.ToPosX);
                    }
                }
            }
        }

        private void RenderStory(WorldChunk chunk, PlotFloorPlan dest, int storyIndex)
        {
            int storyBaseY = dest.LocationY + dest.BaseHeight + storyIndex * dest.StoryHeight;
            KlotzColor wallColor = (storyIndex % 2 == 0) ? KlotzColor.White : KlotzColor.Yellow;
            StoryFloorPlan floorPlan = StoryFloorPlanGenerator.Generate(dest.HouseLocation.width, dest.HouseLocation.height);

            RenderWalls(chunk, dest, floorPlan, storyBaseY, wallColor);

            foreach (var door in floorPlan.Doors)
            {
                chunk.PlaceKlotz(
                    KlotzType.DoorFrame1x4,
                    KlotzColor.Brown,
                    NextRandVariant(),
                    new RelKlotzCoords(
                        dest.PlotLocation.x + dest.HouseLocation.x + door.Location.x,
                        storyBaseY,
                        dest.PlotLocation.y + dest.HouseLocation.y + door.Location.y),
                    door.Direction);
            }

            foreach (var window in floorPlan.Windows)
            {
                chunk.PlaceKlotz(
                    KlotzType.WindowFrame1x4,
                    KlotzColor.Brown,
                    NextRandVariant(),
                    new RelKlotzCoords(
                        dest.PlotLocation.x + dest.HouseLocation.x + window.Location.x,
                        storyBaseY + PlotFloorPlan.WindowSillHeight,
                        dest.PlotLocation.y + dest.HouseLocation.y + window.Location.y),
                    window.Direction);
            }

        }

        // Available brick lengths used to tile a wall run, longest first.
        private static readonly (int Length, KlotzType Type)[] BrickLengths = new[]
        {
            (4, KlotzType.Brick1x4),
            (3, KlotzType.Brick1x3),
            (2, KlotzType.Brick1x2),
            (1, KlotzType.Brick1x1),
        };

        private static (int Length, KlotzType Type) PickBrickLength(int maxLength)
        {
            foreach (var candidate in BrickLengths)
            {
                if (candidate.Length <= maxLength)
                    return candidate;
            }

            return BrickLengths[BrickLengths.Length - 1];
        }

        /// <summary>
        /// Brick-tiles the 4 perimeter wall runs, one course (one brick's height) at a time.
        /// Real masonry corners alternate which wall's bricks wrap the corner between courses,
        /// so it never sits on the exact same joint twice - here that's approximated by
        /// alternating which axis (X-running or Z-running walls) "owns" the corner cells each
        /// course; the other axis's runs are inset by 1 cell at each end that course.
        /// </summary>
        private void RenderWalls(WorldChunk chunk, PlotFloorPlan dest, StoryFloorPlan floorPlan, int storyBaseY, KlotzColor wallColor)
        {
            int courseHeight = KlotzKB.Size(KlotzType.Brick1x1).Y;
            int courseCount = dest.StoryHeight / courseHeight;

            for (int course = 0; course < courseCount; course++)
            {
                int localY = course * courseHeight; // story-relative, for the door/window thresholds
                int worldY = storyBaseY + localY;
                bool xRunsOwnCorners = course % 2 == 0;

                foreach (WallRun run in floorPlan.WallRuns)
                {
                    bool isXRun = run.Direction == KlotzDirection.ToPosX || run.Direction == KlotzDirection.ToNegX;
                    bool ownsCorners = isXRun == xRunsOwnCorners;
                    int inset = ownsCorners ? 0 : 1;

                    RenderWallCourse(chunk, dest, floorPlan, run, localY, worldY, inset, run.Length - inset, wallColor);
                }
            }
        }

        /// <summary>
        /// Tiles one course of one wall run between [rangeStart, rangeEnd), splitting into
        /// separate brick runs around any door/window opening that's open at this course's height.
        /// </summary>
        private void RenderWallCourse(WorldChunk chunk, PlotFloorPlan dest, StoryFloorPlan floorPlan, WallRun run, int localY, int worldY, int rangeStart, int rangeEnd, KlotzColor wallColor)
        {
            int segmentStart = -1;

            for (int pos = rangeStart; pos <= rangeEnd; pos++)
            {
                bool solid = pos < rangeEnd && IsWallCellSolid(floorPlan, StepAlongRun(run, pos), localY);

                if (solid && segmentStart < 0)
                {
                    segmentStart = pos;
                }
                else if (!solid && segmentStart >= 0)
                {
                    RenderBrickRun(chunk, dest, run, worldY, segmentStart, pos - segmentStart, wallColor);
                    segmentStart = -1;
                }
            }
        }

        private static bool IsWallCellSolid(StoryFloorPlan floorPlan, Vector2Int cell, int localY)
        {
            StoryFloorPlanCell type = floorPlan.Plan[cell.x][cell.y];
            return type == StoryFloorPlanCell.Wall
                || (type == StoryFloorPlanCell.Door && localY >= PlotFloorPlan.DoorHeight)
                || (type == StoryFloorPlanCell.Window && (localY < PlotFloorPlan.WindowSillHeight || localY >= PlotFloorPlan.WindowSillHeight + PlotFloorPlan.WindowFrameHeight));
        }

        private void RenderBrickRun(WorldChunk chunk, PlotFloorPlan dest, WallRun run, int y, int start, int length, KlotzColor wallColor)
        {
            int pos = start;
            while (pos < start + length)
            {
                (int brickLength, KlotzType type) = PickBrickLength(start + length - pos);
                Vector2Int cell = StepAlongRun(run, pos);

                chunk.PlaceKlotz(
                    type,
                    wallColor,
                    NextRandVariant(),
                    new RelKlotzCoords(
                        dest.PlotLocation.x + dest.HouseLocation.x + cell.x,
                        y,
                        dest.PlotLocation.y + dest.HouseLocation.y + cell.y),
                    run.Direction);

                pos += brickLength;
            }
        }

        private static Vector2Int StepAlongRun(WallRun run, int pos)
        {
            return run.Direction switch
            {
                KlotzDirection.ToPosX => new(run.Start.x + pos, run.Start.y),
                KlotzDirection.ToNegX => new(run.Start.x - pos, run.Start.y),
                KlotzDirection.ToPosZ => new(run.Start.x, run.Start.y + pos),
                KlotzDirection.ToNegZ => new(run.Start.x, run.Start.y - pos),
                _ => run.Start,
            };
        }

        // Available Slope45Single lengths used to tile an arbitrary roof depth, longest first.
        // 2x6/2x8 are deliberately excluded here - long unbroken slope runs look too coarse on a
        // roof, even though the klotz types themselves are fine for other uses.
        private static readonly (int Length, KlotzType Type)[] SlopeLengths = new[]
        {
            (4, KlotzType.Slope45Single2x4),
            (3, KlotzType.Slope45Single2x3),
            (2, KlotzType.Slope45Single2x2),
            (1, KlotzType.Slope45Single2x1),
        };

        private void RenderRoof(WorldChunk chunk, PlotFloorPlan dest)
        {
            int roofBaseY = dest.LocationY + dest.BaseHeight + dest.StoryCount * dest.StoryHeight;
            KlotzColor roofColor = KlotzColor.Red;
            int rowHeight = PlotFloorPlan.RoofRowHeight;

            // The topmost row would sit on the ridge itself, where a Slope45Double belongs
            // (studless, slopes down on both sides) instead of a single-sided Slope45Single.
            // That klotz doesn't exist yet, so leave the ridge open for now.
            int renderedRowCount = dest.RoofRowCount - 1;

            int zOffset = 0;
            while (zOffset < dest.RoofLocation.height)
            {
                int remaining = dest.RoofLocation.height - zOffset;
                (int length, KlotzType type) = PickSlopeLength(remaining);
                int z = dest.PlotLocation.y + dest.RoofLocation.y + zOffset;

                for (int row = 0; row < renderedRowCount; row++)
                {
                    int y = roofBaseY + row * rowHeight;

                    // Each row's plinth (the low outer edge) sits on top of the previous row's
                    // flat/studded lane - like a real slope brick clutching the studs below it -
                    // so consecutive rows advance by only 1 cell in X, not the piece's own 2-cell
                    // footprint. That overlap is what recreates the original 1:3 (X:Y) pitch.

                    // Left half: eave at the edge of RoofLocation, ridge towards the center.
                    chunk.PlaceKlotz(
                        type,
                        roofColor,
                        NextRandVariant(),
                        new RelKlotzCoords(
                            dest.PlotLocation.x + dest.RoofLocation.x + row + 1,
                            y,
                            z),
                        KlotzDirection.ToPosZ);

                    // Right half: mirrored, eave at the opposite edge of RoofLocation.
                    chunk.PlaceKlotz(
                        type,
                        roofColor,
                        NextRandVariant(),
                        new RelKlotzCoords(
                            dest.PlotLocation.x + dest.RoofLocation.x + dest.RoofLocation.width - 2 - row,
                            y,
                            z + length - 1),
                        KlotzDirection.ToNegZ);
                }

                zOffset += length;
            }
        }

        private static (int Length, KlotzType Type) PickSlopeLength(int maxLength)
        {
            foreach (var candidate in SlopeLengths)
            {
                if (candidate.Length <= maxLength)
                    return candidate;
            }

            return SlopeLengths[SlopeLengths.Length - 1];
        }

        private void RenderGarden(WorldChunk chunk, PlotFloorPlan dest)
        {
            int baseY = dest.LocationY + dest.BaseHeight;
            KlotzColor color = KlotzColor.Green;

            for (int dx = 0; dx < dest.PlotLocation.width; dx++)
            {
                for (int dz = 0; dz < dest.PlotLocation.height; dz++)
                {
                    if (dest.PlotPlan[dx][dz] != PlotFloorPlanCell.Garden)
                        continue;

                    chunk.PlaceKlotz(
                        KlotzType.Plate1x1,
                        color,
                        NextRandVariant(),
                        new RelKlotzCoords(dest.PlotLocation.x + dx, baseY, dest.PlotLocation.y + dz),
                        KlotzDirection.ToPosX);
                }
            }
        }
    }
}
