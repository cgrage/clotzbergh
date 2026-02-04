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
            public readonly int Height { get; }
            public readonly BoundsInt Bounds { get; }

            public SimpleHouseDestination(Vector2Int locationXZ, Vector2Int sizeXZ, int locationY, int height)
            {
                LocationXZ = locationXZ;
                SizeXZ = sizeXZ;
                LocationY = locationY;
                Height = height;
                Bounds = new BoundsInt(new Vector3Int(locationXZ.x, locationY, locationXZ.y), new Vector3Int(sizeXZ.x, height, sizeXZ.y));
            }
        }

        private readonly List<SimpleHouseDestination> _destinations = new();

        public override IGenerationModifier GenModifier => this;

        public void OnBeforeGeneration(FieldResolver r)
        {
            for (int i = 0; i < 1; i++)
            {
                Vector3Int dimensions = new(8, 8, 8);

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
                int baseLayerY = dest.LocationY;

                for (int x = 0; x < dest.SizeXZ.x; x++)
                {
                    for (int z = 0; z < dest.SizeXZ.y; z++)
                    {
                        chunk.PlaceKlotz(
                            KlotzType.Plate1x1,
                            KlotzColor.Brown,
                            NextRandVariant(),
                            new RelKlotzCoords(dest.LocationXZ.x + x, baseLayerY, dest.LocationXZ.y + z),
                            KlotzDirection.ToPosX);
                    }
                }
            }
        }
    }
}
