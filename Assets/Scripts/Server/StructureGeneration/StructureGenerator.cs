using System;
using Clotzbergh.Server.ChunkGeneration;
using UnityEngine;

namespace Clotzbergh.Server.StructureGeneration
{
    public abstract class StructureGenerator : SingleUseGenerator, IHeightMapOverride
    {
        // public abstract void PopulateStructures(WorldChunk chunk);
        public abstract float HeightMapOverride(IHeightMap heightMap, int absX, int absZ);
    }

    public class NoStructureGenerator : StructureGenerator
    {
        public override float HeightMapOverride(IHeightMap heightMap, int absX, int absZ)
        {
            return heightMap.At(absX, absZ);
        }
    }

    public class SimpleCentralHouseGenerator : StructureGenerator
    {
        /*
        // Simple example: place a house at the center of the chunk.
        int _centerX = WorldDef.ChunkSubDivsX / 2;
        int _centerY = WorldDef.ChunkSubDivsY / 2;
        */

        public override float HeightMapOverride(IHeightMap heightMap, int absX, int absZ)
        {
            if (absX > -15 && absX < 15 && absZ > -15 && absZ < 15)
            {
                absX = 0;
                absZ = 0;
            }

            return heightMap.At(absX, absZ);
        }
    }
}
