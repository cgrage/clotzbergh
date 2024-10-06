using NUnit.Framework;
using UnityEngine;

public class TerrainGenerationTests
{
    [Test]
    public void GenerateSomeTerrain()
    {
        Vector2Int rangeX = new(-10, 10);
        Vector2Int rangeY = new(-3, 3);
        Vector2Int rangeZ = new(-10, 10);

        WorldGenerator world = new();

        for (int z = rangeZ.x; z < rangeZ.y; z++)
        {
            for (int y = rangeY.x; y < rangeY.y; y++)
            {
                for (int x = rangeX.x; x < rangeX.y; x++)
                {
                    world.GetChunk(new(x, y, z));
                }
            }
        }
    }

    [Test]
    public void GenerateTerrainAndMesh()
    {
        Vector2Int rangeX = new(-10, 10);
        Vector2Int rangeY = new(-3, 3);
        Vector2Int rangeZ = new(-10, 10);

        WorldGenerator world = new();
        MeshGenerator mesh = new();

        for (int z = rangeZ.x; z < rangeZ.y; z++)
        {
            for (int y = rangeY.x; y < rangeY.y; y++)
            {
                for (int x = rangeX.x; x < rangeX.y; x++)
                {
                    Vector3Int coords = new(x, y, z);
                    TerrainChunk terrainChunk = new(coords, null, null, null);
                    WorldChunk worldChunk = world.GetChunk(coords);

                    terrainChunk.OnWorldChunkReceived(worldChunk, Vector3.zero);
                    mesh.GenerateTerrainMesh(terrainChunk, 0);
                }
            }
        }
    }
}
