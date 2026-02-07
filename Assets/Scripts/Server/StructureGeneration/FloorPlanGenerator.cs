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

    public class FloorPlanGenerator
    {
        private enum WallDirection { Horizontal, Vertical }

        private readonly FloorPlanCell[][] _plan;

        public static FloorPlanCell[][] Generate(int sizeX, int sizeY)
        {
            FloorPlanGenerator generator = new(sizeX, sizeY);
            return generator._plan;
        }

        private FloorPlanGenerator(int sizeX, int sizeY)
        {
            _plan = new FloorPlanCell[sizeX][];
            for (int x = 0; x < sizeX; x++)
            {
                _plan[x] = new FloorPlanCell[sizeY];
            }

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

            for (int i = 0; i < length; i++)
            {
                if (i > 2 && i <= 6 && hasDoor) // TODO: Randomize door position
                {
                    _plan[x][y] = FloorPlanCell.Door;
                }
                else
                {
                    _plan[x][y] = FloorPlanCell.Wall;
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
                    if (x >= 0 && x < _plan.Length && y >= 0 && y < _plan[0].Length)
                    {
                        _plan[x][y] = FloorPlanCell.Interior;
                    }
                }
            }
        }
    }
}
