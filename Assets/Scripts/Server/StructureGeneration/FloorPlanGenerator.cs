using System.Collections.Generic;
using Clotzbergh.Server.ChunkGeneration;
using UnityEngine;

namespace Clotzbergh.Server.StructureGeneration
{
    public enum FloorPlanCell
    {
        Empty,
        Wall,
        Interior,
        Door,
        Window
    }

    public class FloorPlan
    {
        public FloorPlanCell[][] Plan { get; private set; }
        public Vector2Int[] DoorLocations { get; private set; }
        public Vector2Int[] WindowLocations { get; private set; }

        public FloorPlan(int sizeX, int sizeY)
        {
            Plan = new FloorPlanCell[sizeX][];
            for (int x = 0; x < sizeX; x++)
            {
                Plan[x] = new FloorPlanCell[sizeY];
            }
            DoorLocations = new Vector2Int[0];
            WindowLocations = new Vector2Int[0];
        }
    }

    public class FloorPlanGenerator
    {
        private enum WallDirection { Horizontal, Vertical }

        private readonly FloorPlan _plan;

        public static FloorPlan Generate(int sizeX, int sizeY)
        {
            FloorPlanGenerator generator = new(sizeX, sizeY);
            return generator._plan;
        }

        private FloorPlanGenerator(int sizeX, int sizeY)
        {
            _plan = new FloorPlan(sizeX, sizeY);

            int sideWithDoor = 0; // TODO: Randomize this

            PlaceWall(0, 0, sizeX, WallDirection.Horizontal, sideWithDoor == 0);
            PlaceWall(0, sizeY - 1, sizeX, WallDirection.Horizontal, sideWithDoor == 1);
            PlaceWall(0, 0, sizeY, WallDirection.Vertical, sideWithDoor == 2);
            PlaceWall(sizeX - 1, 0, sizeY, WallDirection.Vertical, sideWithDoor == 3);
            FillRoom(1, 1, sizeX - 2, sizeY - 2);
        }

        private void PlaceWall(int startX, int startY, int length, WallDirection direction, bool hasDoor = false)
        {
            int x = startX;
            int y = startY;

            for (int pos = 0; pos < length; pos++)
            {
                int remaining = length - pos;

                if (pos > 2 && pos <= 6 && remaining > 4) // TODO: Randomize door location
                {
                    if (hasDoor)
                    {
                        _plan.Plan[x][y] = FloorPlanCell.Door;
                    }
                    else
                    {
                        _plan.Plan[x][y] = FloorPlanCell.Window;
                    }
                }
                else
                {
                    _plan.Plan[x][y] = FloorPlanCell.Wall;
                }

                if (direction == WallDirection.Horizontal) x++; else y++;
            }
        }

        private void FillRoom(int startX, int startY, int sizeX, int sizeY)
        {
            for (int x = startX; x < startX + sizeX; x++)
            {
                for (int y = startY; y < startY + sizeY; y++)
                {
                    if (x >= 0 && x < _plan.Plan.Length && y >= 0 && y < _plan.Plan[0].Length)
                    {
                        _plan.Plan[x][y] = FloorPlanCell.Interior;
                    }
                }
            }
        }
    }
}
