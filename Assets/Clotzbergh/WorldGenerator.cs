using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class WorldGenerator
{
    public WorldChunk GetChunk(Vector3Int chunkCoords)
    {
        return WorldChunk.CreateCoreFilled(KlotzType.Plate1x1);

        /*

        Vector2Int where = new(
            chunkCoords.x * WorldChunk.KlotzCountX - WorldChunk.BorderSize,
            chunkCoords.z * WorldChunk.KlotzCountZ - WorldChunk.BorderSize);

        float[,] worldHeight = HeightNoise.Generate(WorldChunk.KlotzCountRawX, WorldChunk.KlotzCountRawZ, where);
        var chunk = WorldChunk.CreateEmpty();

        for (int z = 0; z < WorldChunk.KlotzCountZ; z++)
        {
            for (int y = 0; y < WorldChunk.KlotzCountY; y++)
            {
                for (int x = 0; x < WorldChunk.KlotzCountX; x++)
                {
                    chunk
                }
            }
        }

        return chunk;
        */
    }
}
