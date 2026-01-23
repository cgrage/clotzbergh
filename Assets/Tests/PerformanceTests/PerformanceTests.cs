using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Clotzbergh;
using Clotzbergh.Client;
using Clotzbergh.Client.MeshGeneration;
using Clotzbergh.Server;
using Stopwatch = System.Diagnostics.Stopwatch;

public class PerformanceTests
{
    [Test]
    public void PerformanceTestRadius0() { PerformanceTest(0); }

    [Test]
    public void PerformanceTestRadius1() { PerformanceTest(1); }

    [Test]
    public void PerformanceTestRadius2() { PerformanceTest(2); }

    [Test]
    public void PerformanceTestRadius3() { PerformanceTest(3); }

    [Test]
    public void PerformanceTestRadius4() { PerformanceTest(4); }

    [Test]
    public void PerformanceTestRadius5() { PerformanceTest(5); }

    [Test]
    public void PerformanceTestRadius6() { PerformanceTest(6); }

    public void PerformanceTest(int radius) { PerformanceTest(radius, radius, radius); }

    public void PerformanceTest(int radiusX, int radiusY, int radiusZ)
    {
        try
        {
            WorldChunk[,,] worlds = TimeAndLog("GenerateWorld", () => GenerateWorld(radiusX, radiusY, radiusZ));

            byte[] data = TimeAndLog("SerializeWorld", () => SerializeWorld(worlds));

            worlds = TimeAndLog("DeserializeWorld", () => DeserializeWorld(data));

            ClientChunk[,,] terrains = TimeAndLog("CreateClientChunks", () => CreateClientChunks(worlds));

            VoxelMeshBuilder[,,] meshes = TimeAndLog("GenerateMeshes", () => GenerateMeshes(terrains));

            TimeAndLog("SetMeshes", () => SetMeshes(terrains, meshes));
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
        WorldGenerator generator = new(0);
        WorldChunk[,,] worlds = new WorldChunk[
            2 * radiusX + 1,
            2 * radiusY + 1,
            2 * radiusZ + 1];

        for (int zCoord = -radiusZ; zCoord <= radiusZ; zCoord++)
        {
            for (int yCoord = -radiusY; yCoord <= radiusY; yCoord++)
            {
                for (int xCoord = -radiusX; xCoord <= radiusX; xCoord++)
                {
                    int x = xCoord + radiusX;
                    int y = yCoord + radiusY;
                    int z = zCoord + radiusZ;
                    worlds[x, y, z] = generator.GetChunk(new(xCoord, yCoord, zCoord));
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

    private ClientChunk[,,] CreateClientChunks(WorldChunk[,,] worlds)
    {
        ClientChunk[,,] terrains = new ClientChunk[
           worlds.GetLength(0), worlds.GetLength(1), worlds.GetLength(2)];

        for (int x = 0; x < worlds.GetLength(0); x++)
        {
            for (int y = 0; y < worlds.GetLength(1); y++)
            {
                for (int z = 0; z < worlds.GetLength(2); z++)
                {
                    terrains[x, y, z] = new(new(x, y, z), null, null, null, null);
                    terrains[x, y, z].OnWorldUpdate(1, worlds[x, y, z]);
                }
            }
        }

        return terrains;
    }

    private VoxelMeshBuilder[,,] GenerateMeshes(ClientChunk[,,] terrains)
    {
        VoxelMeshBuilder[,,] meshes = new VoxelMeshBuilder[
            terrains.GetLength(0), terrains.GetLength(1), terrains.GetLength(2)];

        for (int x = 0; x < terrains.GetLength(0); x++)
        {
            for (int y = 0; y < terrains.GetLength(1); y++)
            {
                for (int z = 0; z < terrains.GetLength(2); z++)
                {
                    meshes[x, y, z] = MeshGenerator.GenerateTerrainMesh(terrains[x, y, z], 0);
                }
            }
        }

        return meshes;
    }

    private ClientChunk[,,] SetMeshes(ClientChunk[,,] terrains, VoxelMeshBuilder[,,] meshes)
    {
        for (int x = 0; x < terrains.GetLength(0); x++)
        {
            for (int y = 0; y < terrains.GetLength(1); y++)
            {
                for (int z = 0; z < terrains.GetLength(2); z++)
                {
                    terrains[x, y, z].OnMeshUpdate(meshes[x, y, z], 0, 0);
                }
            }
        }

        return terrains;
    }
}
