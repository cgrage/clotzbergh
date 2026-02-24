using UnityEngine;

namespace Clotzbergh.Server.StructureGeneration
{
    public enum StoryFloorPlanCell
    {
        Unknown,
        Wall,
        Interior,
        Door,
        Window
    }

    public readonly struct DoorInfo
    {
        public Vector2Int Location { get; }
        public KlotzDirection Direction { get; }

        public DoorInfo(Vector2Int location, KlotzDirection direction)
        {
            Location = location;
            Direction = direction;
        }
    }

    public readonly struct WindowInfo
    {
        public Vector2Int Location { get; }
        public KlotzDirection Direction { get; }

        public WindowInfo(Vector2Int location, KlotzDirection direction)
        {
            Location = location;
            Direction = direction;
        }
    }

    public class StoryFloorPlan
    {
        public StoryFloorPlanCell[][] Plan { get; private set; }
        public DoorInfo[] Doors { get; set; }
        public WindowInfo[] Windows { get; set; }

        public StoryFloorPlan(int sizeX, int sizeY)
        {
            Plan = new StoryFloorPlanCell[sizeX][];
            for (int x = 0; x < sizeX; x++)
            {
                Plan[x] = new StoryFloorPlanCell[sizeY];
            }
        }
    }

    public class StoryFloorPlanGenerator
    {
        private readonly StoryFloorPlan _plan;

        public static StoryFloorPlan Generate(int sizeX, int sizeY)
        {
            StoryFloorPlanGenerator generator = new(sizeX, sizeY);
            return generator._plan;
        }

        private StoryFloorPlanGenerator(int sizeX, int sizeY)
        {
            _plan = new StoryFloorPlan(sizeX, sizeY);

            PlaceWall(0, 0, sizeX, KlotzDirection.ToPosX, true);
            PlaceWall(sizeX - 1, 0, sizeY, KlotzDirection.ToPosZ, false);
            PlaceWall(sizeX - 1, sizeY - 1, sizeX, KlotzDirection.ToNegX, false);
            PlaceWall(0, sizeY - 1, sizeY, KlotzDirection.ToNegZ, false);
            FillRoom(1, 1, sizeX - 2, sizeY - 2);

            _plan.Doors = new DoorInfo[]
            {
                new(new Vector2Int(3, 0), KlotzDirection.ToPosX),
            };

            _plan.Windows = new WindowInfo[]
            {
                new (new Vector2Int(sizeX - 1, 3), KlotzDirection.ToPosZ),
                new (new Vector2Int(sizeX - 1 - 3, sizeY - 1), KlotzDirection.ToNegX),
                new (new Vector2Int(0, sizeY - 1 - 3), KlotzDirection.ToNegZ),
            };
        }

        private void PlaceWall(int startX, int startY, int length, KlotzDirection direction, bool hasDoor = false)
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
                        _plan.Plan[x][y] = StoryFloorPlanCell.Door;
                    }
                    else
                    {
                        _plan.Plan[x][y] = StoryFloorPlanCell.Window;
                    }
                }
                else
                {
                    _plan.Plan[x][y] = StoryFloorPlanCell.Wall;
                }

                if (direction == KlotzDirection.ToPosX) x++;
                else if (direction == KlotzDirection.ToNegX) x--;
                else if (direction == KlotzDirection.ToPosZ) y++;
                else if (direction == KlotzDirection.ToNegZ) y--;
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
                        _plan.Plan[x][y] = StoryFloorPlanCell.Interior;
                    }
                }
            }
        }
    }

    public enum PlotFloorPlanCell
    {
        Unknown,
        House,
        Garden,
        Walkway,
    }

    public readonly struct PlotFloorPlan
    {
        public const int RoofSlope = 3;
        public const int DoorHeight = 5 * 3;
        public const int WindowSillHeight = 2 * 3;
        public const int WindowFrameHeight = 3 * 3;

        public readonly RectInt PlotLocation { get; }
        public readonly int LocationY { get; }
        public readonly RectInt HouseLocation { get; }
        public readonly RectInt RoofLocation { get; }
        public readonly int RoofHeight { get; }
        public readonly int BaseHeight { get; }
        public readonly int StoryHeight { get; }
        public readonly int StoryCount { get; }
        public readonly int TotalHeight { get; }
        public readonly BoundsInt TotalBounds { get; }

        public PlotFloorPlanCell[][] PlotPlan { get; }

        public PlotFloorPlan(RectInt plotLocation, int locationY, int maxHeight)
        {
            PlotLocation = plotLocation;
            LocationY = locationY;

            int RoofInset = 4;
            RoofLocation = new RectInt(
                1,
                1,
                PlotLocation.width - 2 * RoofInset,
                PlotLocation.height - 2 * RoofInset); // TODO: Randomize this (0..RoofInset)

            int HouseInset = 1;
            HouseLocation = new RectInt(
                RoofLocation.x + HouseInset,
                RoofLocation.y + HouseInset,
                RoofLocation.width - 2 * HouseInset,
                RoofLocation.height - 2 * HouseInset);

            BaseHeight = 1;
            StoryHeight = 3 * 6;
            RoofHeight = (RoofLocation.width / 2) * RoofSlope;

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
            TotalBounds = new BoundsInt(
                new Vector3Int(PlotLocation.x, LocationY, PlotLocation.y),
                new Vector3Int(PlotLocation.width, TotalHeight, PlotLocation.height));

            PlotPlan = new PlotFloorPlanCell[PlotLocation.width][];
            for (int x = 0; x < PlotLocation.width; x++)
            {
                PlotPlan[x] = new PlotFloorPlanCell[PlotLocation.height];
                for (int z = 0; z < PlotLocation.height; z++)
                {
                    if (HouseLocation.Contains(new Vector2Int(x, z)))
                    {
                        PlotPlan[x][z] = PlotFloorPlanCell.House;
                    }
                    else
                    {
                        PlotPlan[x][z] = PlotFloorPlanCell.Garden;
                    }
                }
            }
        }
    }
}
