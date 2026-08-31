using System;
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
                // X/Z capped just under WorldDef.ChunkSubDivsX/Z (32) - NextRandRelCoordsXZ
                // places structures within a single chunk, so this is as big as they can get.
                Vector3Int dimensions = new(30, 60, 30);

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

                Vector2Int?[] stairsPositions = ComputeStairsPositions(dest);
                for (int storyIndex = 0; storyIndex < dest.StoryCount; storyIndex++)
                {
                    RenderStory(chunk, dest, storyIndex, stairsPositions[storyIndex]);
                    RenderCeiling(chunk, dest, storyIndex, stairsPositions[storyIndex]);
                }
                RenderRoof(chunk, dest);
                RenderGableWalls(chunk, dest);
                RenderGarden(chunk, dest);
            }
        }

        /// <summary>
        /// Where each story's own staircase up goes - index storyIndex holds the position of the
        /// stairs that story places (<see cref="RenderStory"/>) and that its own ceiling needs an
        /// opening for (<see cref="RenderCeiling"/>). Every story gets one, including the top one
        /// - its stairs lead up into the attic under the roof. A story's incoming stairwell -
        /// where the stairs from the story below arrive - is simply index storyIndex-1 of this
        /// same array; once we start filling floors with furniture, that's the spot to leave clear.
        /// </summary>
        private Vector2Int?[] ComputeStairsPositions(PlotFloorPlan dest)
        {
            var positions = new Vector2Int?[dest.StoryCount];
            KlotzSize stairsSize = KlotzKB.Size(KlotzType.Stairs4x7);
            int stairsX = dest.HouseLocation.x + (dest.HouseLocation.width - stairsSize.X) / 2;

            for (int storyIndex = 0; storyIndex < dest.StoryCount; storyIndex++)
            {
                // Varies per floor instead of always sitting at the same depth, so stacked
                // stairs don't all land in exactly the same spot.
                int depthQuarters = NextRandomElement(new[] { 1, 2, 3 });
                int stairsZ = dest.HouseLocation.y + (dest.HouseLocation.height - stairsSize.Z) * depthQuarters / 4;
                positions[storyIndex] = new Vector2Int(stairsX, stairsZ);
            }

            return positions;
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

        private void RenderStory(WorldChunk chunk, PlotFloorPlan dest, int storyIndex, Vector2Int? stairsPosition)
        {
            int storyBaseY = dest.LocationY + dest.BaseHeight + storyIndex * dest.StoryHeight;
            KlotzColor wallColor = (storyIndex % 2 == 0) ? KlotzColor.White : KlotzColor.Gray;
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

            if (stairsPosition.HasValue)
            {
                Vector2Int stairsPos = stairsPosition.Value;
                KlotzSize stairsSize = KlotzKB.Size(KlotzType.Stairs4x7);
                int stairsBaseY = storyBaseY + 1;
                chunk.PlaceKlotz(
                    KlotzType.Stairs4x7,
                    KlotzColor.Gray,
                    NextRandVariant(),
                    new RelKlotzCoords(
                        dest.PlotLocation.x + stairsPos.x,
                        stairsBaseY,
                        dest.PlotLocation.y + stairsPos.y),
                    KlotzDirection.ToPosX);

                // A 1x2 plate under the entrance closes the 1-unit gap left by raising the
                // stairs, and one on the nose closes the gap up to the ceiling above - the nose
                // itself sits 1 unit below the top of the last real step (see
                // ArtSource/build_stairs.py).
                int plateX = stairsPos.x + 1;
                chunk.PlaceKlotz(
                    KlotzType.Plate1x2,
                    KlotzColor.Gray,
                    NextRandVariant(),
                    new RelKlotzCoords(
                        dest.PlotLocation.x + plateX,
                        storyBaseY,
                        dest.PlotLocation.y + stairsPos.y),
                    KlotzDirection.ToPosX);
                chunk.PlaceKlotz(
                    KlotzType.Plate1x2,
                    KlotzColor.Gray,
                    NextRandVariant(),
                    new RelKlotzCoords(
                        dest.PlotLocation.x + plateX,
                        stairsBaseY + stairsSize.Y - 1,
                        dest.PlotLocation.y + stairsPos.y + stairsSize.Z - 1),
                    KlotzDirection.ToPosX);
            }
        }

        /// <summary>
        /// The 2-plate-thick ceiling above a story, with an opening over this story's own stairs
        /// (see <see cref="ComputeStairsPositions"/>) so there's room to climb through - or fully
        /// solid if this story has none (the top story, with nothing above to climb to). The
        /// opening is shifted 2 cells towards the entrance (covering the first 6 steps plus 2
        /// cells of standing room before them) and deliberately stops short of the nose (the
        /// stairs' last cell) - no opening needed there.
        /// </summary>
        private void RenderCeiling(WorldChunk chunk, PlotFloorPlan dest, int storyIndex, Vector2Int? stairsPosition)
        {
            int ceilingY = dest.LocationY + dest.BaseHeight + storyIndex * dest.StoryHeight + PlotFloorPlan.WallHeight;
            KlotzColor color = KlotzColor.DarkGray;

            RectInt? cutout = null;
            if (stairsPosition.HasValue)
            {
                Vector2Int stairsPos = stairsPosition.Value;
                KlotzSize stairsSize = KlotzKB.Size(KlotzType.Stairs4x7);
                cutout = new RectInt(stairsPos.x, stairsPos.y - 2, stairsSize.X, stairsSize.Z + 1);
            }

            for (int dx = 0; dx < dest.HouseLocation.width; dx++)
            {
                for (int dz = 0; dz < dest.HouseLocation.height; dz++)
                {
                    if (cutout.HasValue && cutout.Value.Contains(new Vector2Int(dest.HouseLocation.x + dx, dest.HouseLocation.y + dz)))
                        continue;

                    for (int dy = 0; dy < PlotFloorPlan.CeilingHeight; dy++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            color,
                            NextRandVariant(),
                            new RelKlotzCoords(
                                dest.PlotLocation.x + dest.HouseLocation.x + dx,
                                ceilingY + dy,
                                dest.PlotLocation.y + dest.HouseLocation.y + dz),
                            KlotzDirection.ToPosX);
                    }
                }
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

        // Available plate lengths for the finishing row that caps every wall, longest first.
        private static readonly (int Length, KlotzType Type)[] PlateLengths = new[]
        {
            (4, KlotzType.Plate1x4),
            (3, KlotzType.Plate1x3),
            (2, KlotzType.Plate1x2),
            (1, KlotzType.Plate1x1),
        };

        private static (int Length, KlotzType Type) PickPlateLength(int maxLength)
        {
            foreach (var candidate in PlateLengths)
            {
                if (candidate.Length <= maxLength)
                    return candidate;
            }

            return PlateLengths[PlateLengths.Length - 1];
        }

        /// <summary>
        /// Brick-tiles the 4 perimeter wall runs, one course (one brick's height) at a time, then
        /// caps them with one more course of plates in orange. Real masonry corners alternate
        /// which wall's pieces wrap the corner between courses, so it never sits on the exact same
        /// joint twice - here that's approximated by alternating which axis (X-running or
        /// Z-running walls) "owns" the corner cells each course (including the plate cap, which
        /// continues the same alternation); the other axis's runs are inset by 1 cell that course.
        /// </summary>
        private void RenderWalls(WorldChunk chunk, PlotFloorPlan dest, StoryFloorPlan floorPlan, int storyBaseY, KlotzColor wallColor)
        {
            int courseHeight = KlotzKB.Size(KlotzType.Brick1x1).Y;
            int brickCourseCount = PlotFloorPlan.WallHeight / courseHeight;

            for (int course = 0; course < brickCourseCount; course++)
            {
                int localY = course * courseHeight; // story-relative, for the door/window thresholds
                int worldY = storyBaseY + localY;

                RenderWallCourseRing(chunk, dest, floorPlan, localY, worldY, course, wallColor, PickBrickLength);
            }

            // The plate cap: one more course, one unit tall, right above the last brick course.
            int capLocalY = brickCourseCount * courseHeight;
            RenderWallCourseRing(chunk, dest, floorPlan, capLocalY, storyBaseY + capLocalY, brickCourseCount, KlotzColor.Orange, PickPlateLength);
        }

        private void RenderWallCourseRing(WorldChunk chunk, PlotFloorPlan dest, StoryFloorPlan floorPlan, int localY, int worldY, int course, KlotzColor color, Func<int, (int Length, KlotzType Type)> pickLength)
        {
            bool xRunsOwnCorners = course % 2 == 0;

            foreach (WallRun run in floorPlan.WallRuns)
            {
                bool isXRun = run.Direction == KlotzDirection.ToPosX || run.Direction == KlotzDirection.ToNegX;
                bool ownsCorners = isXRun == xRunsOwnCorners;
                int inset = ownsCorners ? 0 : 1;

                RenderWallCourse(chunk, dest, floorPlan, run, localY, worldY, inset, run.Length - inset, color, pickLength);
            }
        }

        /// <summary>
        /// Tiles one course of one wall run between [rangeStart, rangeEnd), splitting into
        /// separate runs around any door/window opening that's open at this course's height.
        /// </summary>
        private void RenderWallCourse(WorldChunk chunk, PlotFloorPlan dest, StoryFloorPlan floorPlan, WallRun run, int localY, int worldY, int rangeStart, int rangeEnd, KlotzColor color, Func<int, (int Length, KlotzType Type)> pickLength)
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
                    RenderWallPieceRun(chunk, dest, run, worldY, segmentStart, pos - segmentStart, color, pickLength);
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

        private void RenderWallPieceRun(WorldChunk chunk, PlotFloorPlan dest, WallRun run, int y, int start, int length, KlotzColor color, Func<int, (int Length, KlotzType Type)> pickLength)
        {
            int pos = start;
            while (pos < start + length)
            {
                (int pieceLength, KlotzType type) = pickLength(start + length - pos);
                Vector2Int cell = StepAlongRun(run, pos);

                chunk.PlaceKlotz(
                    type,
                    color,
                    NextRandVariant(),
                    new RelKlotzCoords(
                        dest.PlotLocation.x + dest.HouseLocation.x + cell.x,
                        y,
                        dest.PlotLocation.y + dest.HouseLocation.y + cell.y),
                    run.Direction);

                pos += pieceLength;
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

            for (int row = 0; row < renderedRowCount; row++)
            {
                int y = roofBaseY + row * rowHeight;

                // Each row's plinth (the low outer edge) sits on top of the previous row's
                // flat/studded lane - like a real slope brick clutching the studs below it -
                // so consecutive rows advance by only 1 cell in X, not the piece's own 2-cell
                // footprint. That overlap is what recreates the original 1:3 (X:Y) pitch.

                // Every other row starts with a short 2x2 instead of jumping straight to the
                // longest piece that fits, so the seams along the ridge don't all line up
                // between rows the way they would if every row tiled identically.
                bool staggered = row % 2 == 1;

                int zOffset = 0;
                while (zOffset < dest.RoofLocation.height)
                {
                    int remaining = dest.RoofLocation.height - zOffset;
                    (int length, KlotzType type) = (staggered && zOffset == 0)
                        ? PickSlopeLength(Mathf.Min(2, remaining))
                        : PickSlopeLength(remaining);
                    int z = dest.PlotLocation.y + dest.RoofLocation.y + zOffset;

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

                    zOffset += length;
                }
            }
        }

        /// <summary>
        /// Closes the two gable ends (the open triangular faces at each end of the roof ridge)
        /// with brick courses. Each roof row's own end caps already seal its own 2-cell
        /// footprint (see the Slope45Single model), but at that row's height, everything from
        /// there to the ridge is still hollow attic - the rows that will eventually close it are
        /// higher up, further towards the ridge, not built yet at this height. So the notch to
        /// fill runs from just past the row's own footprint to the ridge, shrinking from the eave
        /// side as rows go up - wide near the wall-top, tapering to nothing near the ridge, like
        /// a real gable. Unlike the perimeter walls, no explicit joint-staggering trick is needed
        /// here: every course already has a different length than the one below it.
        /// </summary>
        private void RenderGableWalls(WorldChunk chunk, PlotFloorPlan dest)
        {
            int roofBaseY = dest.LocationY + dest.BaseHeight + dest.StoryCount * dest.StoryHeight;
            int rowHeight = PlotFloorPlan.RoofRowHeight;
            int renderedRowCount = dest.RoofRowCount - 1;
            int halfWidth = dest.RoofRowCount;
            KlotzColor wallColor = (dest.StoryCount % 2 == 0) ? KlotzColor.White : KlotzColor.Gray;

            // In Z, sit flush with the house walls (HouseLocation), not the roof's own footprint
            // (RoofLocation) - the roof overhangs the walls by design, but the gable wall itself
            // shouldn't stick out into that overhang.
            int xBase = dest.PlotLocation.x + dest.RoofLocation.x;
            int zNear = dest.PlotLocation.y + dest.HouseLocation.y;
            int zFar = dest.PlotLocation.y + dest.HouseLocation.y + dest.HouseLocation.height - 1;

            for (int row = 0; row < renderedRowCount; row++)
            {
                int length = halfWidth - row - 2;
                if (length <= 0)
                    continue;

                int y = roofBaseY + row * rowHeight;
                int leftStart = xBase + row + 2;
                int rightStart = xBase + halfWidth;

                RenderGableBrickSegment(chunk, leftStart, zNear, y, length, wallColor);
                RenderGableBrickSegment(chunk, leftStart, zFar, y, length, wallColor);
                RenderGableBrickSegment(chunk, rightStart, zNear, y, length, wallColor);
                RenderGableBrickSegment(chunk, rightStart, zFar, y, length, wallColor);
            }
        }

        private void RenderGableBrickSegment(WorldChunk chunk, int xStart, int z, int y, int length, KlotzColor wallColor)
        {
            int pos = 0;
            while (pos < length)
            {
                (int brickLength, KlotzType type) = PickBrickLength(length - pos);

                chunk.PlaceKlotz(
                    type,
                    wallColor,
                    NextRandVariant(),
                    new RelKlotzCoords(xStart + pos, y, z),
                    KlotzDirection.ToPosX);

                pos += brickLength;
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
