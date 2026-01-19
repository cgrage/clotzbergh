using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Clotzbergh.Server.WorldGeneration;
using Clotzbergh.Server;
using Clotzbergh;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class WorldGeneratorTests
{
    public class PackedHeightMap : IHeightMap
    {
        public float At(int x, int y) { return 1000f; }
    }

    public class EmptyHeightMap : IHeightMap
    {
        public float At(int x, int y) { return -1000f; }
    }

    [Test]
    public void WG01_TrivialWorldGeneratorPackedTest()
    {
        IChunkGenerator gen = new WG01_TrivialWorldGenerator();
        IHeightMap hm = new PackedHeightMap();
        GeneratorTest(gen, hm, false, true);
    }

    [Test]
    public void WG01_TrivialWorldGeneratorEmptyTest()
    {
        IChunkGenerator gen = new WG01_TrivialWorldGenerator();
        IHeightMap hm = new EmptyHeightMap();
        GeneratorTest(gen, hm, true, false);
    }

    [Test]
    public void WG02_MicroBlockWorldGeneratorPackedTest()
    {
        IChunkGenerator gen = new WG02_MicroBlockWorldGenerator();
        IHeightMap hm = new PackedHeightMap();
        GeneratorTest(gen, hm, false, true);
    }

    [Test]
    public void WG02_MicroBlockWorldGeneratorEmptyTest()
    {
        IChunkGenerator gen = new WG02_MicroBlockWorldGenerator();
        IHeightMap hm = new EmptyHeightMap();
        GeneratorTest(gen, hm, true, false);
    }

    [Test]
    public void WG03_WaveFunctionCollapseGeneratorPackedTest()
    {
        IChunkGenerator gen = new WG03_WaveFunctionCollapseGenerator();
        IHeightMap hm = new PackedHeightMap();
        GeneratorTest(gen, hm, false, true);
    }

    [Test]
    public void WG03_WaveFunctionCollapseGeneratorEmptyTest()
    {
        IChunkGenerator gen = new WG03_WaveFunctionCollapseGenerator();
        IHeightMap hm = new EmptyHeightMap();
        GeneratorTest(gen, hm, true, false);
    }

    [Test]
    public void WG04_WaveFunctionCollapseGeneratorV2PackedTest()
    {
        IChunkGenerator gen = new WG04_WaveFunctionCollapseGeneratorV2();
        IHeightMap hm = new PackedHeightMap();
        GeneratorTest(gen, hm, false, true);
    }

    [Test]
    public void WG04_WaveFunctionCollapseGeneratorV2EmptyTest()
    {
        IChunkGenerator gen = new WG04_WaveFunctionCollapseGeneratorV2();
        IHeightMap hm = new EmptyHeightMap();
        GeneratorTest(gen, hm, true, false);
    }

    [Test]
    public void WG05_OpportunisticGeneratorPackedTest()
    {
        IChunkGenerator gen = new WG05_OpportunisticGenerator();
        IHeightMap hm = new PackedHeightMap();
        GeneratorTest(gen, hm, false, true);
    }

    [Test]
    public void WG05_OpportunisticGeneratorEmptyTest()
    {
        IChunkGenerator gen = new WG05_OpportunisticGenerator();
        IHeightMap hm = new EmptyHeightMap();
        GeneratorTest(gen, hm, true, false);
    }

    public void GeneratorTest(IChunkGenerator gen, IHeightMap heightMap, bool testEmpty, bool testPacked)
    {
        Vector3Int coords = new(0, 0, 0);
        List<WorldChunk> chunks = new();
        Stopwatch stopwatch = new();

        stopwatch.Start();
        while (stopwatch.ElapsedMilliseconds < 1000)
        {
            chunks.Add(gen.Generate(coords, heightMap));
            coords += new Vector3Int(1, 0, 0);
        }
        stopwatch.Stop();

        Debug.Log($"Created {chunks.Count} chunks in {stopwatch.ElapsedMilliseconds:.0} ms");

        foreach (WorldChunk chunk in chunks)
        {
            CountChunkContent(chunk, out int klotzCount, out int opaqueCount, out int airCount);

            if (testEmpty)
            {
                Assert.AreEqual(WorldDef.SubKlotzPerChunkCount, klotzCount, "klotzCount mismatch");
                Assert.AreEqual(WorldDef.SubKlotzPerChunkCount, airCount, "airCount mismatch");
                Assert.AreEqual(0, opaqueCount, "opaqueCount mismatch");
            }

            if (testPacked)
            {
                Assert.AreEqual(0, airCount, "airCount mismatch");
                Assert.AreEqual(WorldDef.SubKlotzPerChunkCount, opaqueCount, "opaqueCount mismatch");
            }

            CheckChunkHasNoContradictions(chunk);
            Debug.Log($"Klotz-Count: {klotzCount}, Opaque-Count {opaqueCount}, Air-Count: {airCount}");
        }
    }

    private void CountChunkContent(WorldChunk chunk, out int klotzCount, out int opaqueCount, out int airCount)
    {
        klotzCount = 0;
        opaqueCount = 0;
        airCount = 0;

        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    SubKlotz k = chunk.Get(x, y, z);

                    if (k.IsRoot)
                    {
                        klotzCount++;
                        if (k.IsAir)
                        {
                            airCount++;
                        }
                    }

                    if (k.IsOpaque)
                    {
                        opaqueCount++;
                    }
                }
            }
        }
    }

    private void CheckChunkHasNoContradictions(WorldChunk chunk)
    {
        Vector3Int?[,,] usedBy = new Vector3Int?[WorldDef.ChunkSubDivsX,
            WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];

        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    Vector3Int pos = new(x, y, z);
                    SubKlotz k = chunk.Get(pos);
                    if (!k.IsRoot)
                        continue;

                    KlotzType type = k.Type;
                    KlotzDirection dir = k.Direction;
                    KlotzSize typeSize = KlotzKB.Size(type);

                    for (int zi = 0; zi < typeSize.Z; zi++)
                    {
                        for (int yi = 0; yi < typeSize.Y; yi++)
                        {
                            for (int xi = 0; xi < typeSize.X; xi++)
                            {
                                Vector3Int subIndex = new(xi, yi, zi);
                                Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(pos, subIndex, dir);

                                Vector3Int? use = usedBy[coords.x, coords.y, coords.z];
                                if (use.HasValue)
                                {
                                    Assert.Fail($"Double use at {coords} {chunk.Get(subIndex)} by {use.Value} {chunk.Get(use.Value)} and {pos} {chunk.Get(pos)}");
                                }
                                usedBy[coords.x, coords.y, coords.z] = pos;
                            }
                        }
                    }
                }
            }
        }
    }
}
