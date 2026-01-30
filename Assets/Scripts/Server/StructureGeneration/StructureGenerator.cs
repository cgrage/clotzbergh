using System.Collections.Generic;
using Clotzbergh.Server.ChunkGeneration;
using UnityEngine;

namespace Clotzbergh.Server.StructureGeneration
{
    public abstract class StructureGenerator : SingleUseGenerator
    {
        public virtual IWorldGenerationManipulator Manipulator => null;

        // public abstract void PopulateStructures(WorldChunk chunk);
    }

    public class NoStructureGenerator : StructureGenerator
    {
        //
    }

    public class SimpleCentralHouseGenerator : StructureGenerator, IWorldGenerationManipulator
    {
        /*
        // Simple example: place a house at the center of the chunk.
        int _centerX = WorldDef.ChunkSubDivsX / 2;
        int _centerY = WorldDef.ChunkSubDivsY / 2;
        */
        private readonly List<RelKlotzCoords> _destinations = new();

        public override IWorldGenerationManipulator Manipulator => this;

        public void OnBeforeGeneration(FieldResolver r)
        {
            for (int i = 0; i < 1; i++)
            {
                (int x, int z) = NextRandRelCoordsXZ(8, 8);
                int y = r.GroundStartAtRelPos(x, z);

                if (y < 8 || y >= WorldDef.ChunkSubDivsY - 8)
                    continue;

                RelKlotzCoords coords = new(x, y, z);
                if (!IntersectsAny(coords))
                    _destinations.Add(coords);
            }
        }

        private bool IntersectsAny(RelKlotzCoords coords)
        {
            return false;
        }

        public float OnHeightMapOverride(FieldResolver r, int absX, int absZ)
        {
            if (absX > -15 && absX < 15 && absZ > -15 && absZ < 15)
            {
                absX = 0;
                absZ = 0;
            }

            return r.HeightMap.At(absX, absZ);
        }
    }
}
