using Stopwatch = System.Diagnostics.Stopwatch;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using System;

public class PerformanceTests
{
    [Test]
    public void PerformanceTestRadius0()
    {
        PerformanceTest(0, 0, 0);
    }

    [Test]
    public void PerformanceTestRadius1()
    {
        PerformanceTest(1, 1, 1);
    }

    public void PerformanceTest(int radiusX, int radiusY, int radiusZ)
    {
        try
        {
            WorldChunk[,,] worlds = TimeAndLog("GenerateWorld", () => GenerateWorld(radiusX, radiusY, radiusZ));

            byte[] data = TimeAndLog("SerializeWorld", () => SerializeWorld(worlds));

            worlds = TimeAndLog("DeserializeWorld", () => DeserializeWorld(data));

            TerrainChunk[,,] terrains = TimeAndLog("CreateTerrainChunks", () => CreateTerrainChunks(radiusX, radiusY, radiusZ));

            TimeAndLog("InjectWorldIntoTerrain", () => { InjectWorldIntoTerrain(terrains, worlds); return true; });

            TimeAndLog("GenerateAndSetMeshes", () => { GenerateAndSetMeshes(terrains); return true; });
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            throw;
        }
    }

    private T TimeAndLog<T>(string taskName, Func<T> task)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        T result = task();
        stopwatch.Stop();
        Debug.Log($"{taskName}: {stopwatch.ElapsedMilliseconds} ms");
        return result;
    }

    private WorldChunk[,,] GenerateWorld(int radiusX, int radiusY, int radiusZ)
    {
        WorldGenerator generator = new();
        WorldChunk[,,] worlds = new WorldChunk[
            2 * radiusX + 1,
            2 * radiusY + 1,
            2 * radiusZ + 1];

        for (int z = -radiusZ; z <= radiusZ; z++)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = radiusX; x <= radiusX; x++)
                {
                    worlds[x, y, z] = generator.GetChunk(new(x, y, z));
                }
            }
        }

        return worlds;
    }

    private byte[] SerializeWorld(WorldChunk[,,] worlds)
    {
        using MemoryStream ws = new();
        using BinaryWriter w = new(ws);

        w.Write(worlds.GetLength(0));
        w.Write(worlds.GetLength(1));
        w.Write(worlds.GetLength(2));

        for (int x = 0; x < worlds.GetLength(0); x++)
        {
            for (int y = 0; y < worlds.GetLength(1); y++)
            {
                for (int z = 0; z < worlds.GetLength(2); z++)
                {
                    worlds[x, y, z].Serialize(w);
                }
            }
        }

        return ws.ToArray();
    }

    private WorldChunk[,,] DeserializeWorld(byte[] data)
    {
        using MemoryStream rs = new(data);
        using BinaryReader r = new(rs);

        WorldChunk[,,] worlds = new WorldChunk[
            r.ReadInt32(), r.ReadInt32(), r.ReadInt32()];

        for (int x = 0; x < worlds.GetLength(0); x++)
        {
            for (int y = 0; y < worlds.GetLength(1); y++)
            {
                for (int z = 0; z < worlds.GetLength(2); z++)
                {
                    worlds[x, y, z] = WorldChunk.Deserialize(r);
                }
            }
        }

        // we should be at the end of the data
        Assert.AreEqual(data.Length, rs.Position);

        return worlds;
    }

    private TerrainChunk[,,] CreateTerrainChunks(int radiusX, int radiusY, int radiusZ)
    {
        TerrainChunk[,,] terrain = new TerrainChunk[
            2 * radiusX + 1,
            2 * radiusY + 1,
            2 * radiusZ + 1];

        for (int x = 0; x < terrain.GetLength(0); x++)
        {
            for (int y = 0; y < terrain.GetLength(1); y++)
            {
                for (int z = 0; z < terrain.GetLength(2); z++)
                {
                    terrain[x, y, z] = new(new(x, y, z), null, null, null);
                }
            }
        }

        return terrain;
    }

    private void InjectWorldIntoTerrain(TerrainChunk[,,] terrains, WorldChunk[,,] worlds)
    {
        for (int x = 0; x < worlds.GetLength(0); x++)
        {
            for (int y = 0; y < worlds.GetLength(1); y++)
            {
                for (int z = 0; z < worlds.GetLength(2); z++)
                {
                    terrains[x, y, z].OnWorldChunkReceived(worlds[x, y, z], 0);
                }
            }
        }
    }

    private void GenerateAndSetMeshes(TerrainChunk[,,] terrains)
    {
        MeshGenerator meshGen = new();

        for (int x = 0; x < terrains.GetLength(0); x++)
        {
            for (int y = 0; y < terrains.GetLength(1); y++)
            {
                for (int z = 0; z < terrains.GetLength(2); z++)
                {
                    MeshBuilder b = meshGen.GenerateTerrainMesh(terrains[x, y, z], 0);
                    terrains[x, y, z].OnMeshDataReceived(b, 0, 0);
                }
            }
        }
    }
}
