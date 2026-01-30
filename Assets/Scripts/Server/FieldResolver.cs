using System;
using Clotzbergh.Server.ChunkGeneration;
using Clotzbergh.Server.StructureGeneration;
using UnityEngine;
using UnityEngine.UIElements;

namespace Clotzbergh.Server
{
    public interface IWorldGenerationManipulator
    {
        void OnBeforeGeneration(FieldResolver r);
        float OnHeightMapOverride(FieldResolver r, int absX, int absZ);
    }

    public class FieldResolver
    {
        public ChunkCoords Coords { get; }

        public IHeightMap HeightMap { get; }

        private IWorldGenerationManipulator Manipulator { get; set; }

        public FieldResolver(ChunkCoords coords, IHeightMap heightMap)
        {
            Coords = coords;
            HeightMap = heightMap;
        }

        public void RunOnBeforeGeneration()
        {
            if (Manipulator != null)
            {
                Manipulator.OnBeforeGeneration(this);
            }
        }

        public void AddManipulator(IWorldGenerationManipulator manipulator)
        {
            if (Manipulator != null)
            {
                throw new InvalidOperationException(
                    "Implementation limitation: There can currently " +
                    "only be one WorldGenerationManipulator at a time");

            }

            Manipulator = manipulator;
        }

        public int GroundStartAtRelPos(int x, int z)
        {
            return Mathf.RoundToInt(HeightAtRelPos(x, z) / WorldDef.SubKlotzSize.y);
        }

        protected float HeightAtRelPos(int x, int z)
        {
            int absX = Coords.X * WorldDef.ChunkSubDivsX + x;
            int absZ = Coords.Z * WorldDef.ChunkSubDivsZ + z;

            if (Manipulator != null)
            {
                return Manipulator.OnHeightMapOverride(this, absX, absZ);
            }
            else
            {
                return HeightMap.At(absX, absZ);
            }
        }
    }
}
