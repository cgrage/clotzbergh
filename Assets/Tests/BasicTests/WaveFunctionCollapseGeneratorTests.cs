using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class WorldGeneratorTests
{
    public class TestHeightMap : IHeightMap
    {
        public float At(int x, int y)
        {
            return 1000f;
        }
    }

    [Test]
    public void WaveFunctionCollapseGeneratorTest()
    {
        GeneratorTest<WaveFunctionCollapseGenerator>();
    }

    [Test]
    public void MicroBlockWorldGeneratorTest()
    {
        GeneratorTest<MicroBlockWorldGenerator>();
    }

    public void GeneratorTest<T>() where T : ChunkGenerator, new()
    {
        List<Vector3Int> coords = new()
        {
            new(0, 0, 0),
            // new(0, 0, 1),
            // new(0, 0, 2),
            // new(0, 0, 3),
        };

        foreach (var coord in coords)
        {
            WorldChunk chunk = new T().Generate(coord, new TestHeightMap());

            CheckChunkIsFilledCompletely(chunk, out int klotzCount, out int airCount);
            CheckChunkHasNoContradictions(chunk);

            Debug.Log($"Klotz-Count: {klotzCount}, Air-Count: {airCount}");
        }
    }

    private void CheckChunkIsFilledCompletely(WorldChunk chunk, out int klotzCount, out int airCount)
    {
        klotzCount = 0;
        airCount = 0;

        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    Vector3Int pos = new(x, y, z);
                    SubKlotz k = chunk.Get(pos);
                    Assert.IsTrue(k.IsOpaque, $"Not filled at {pos}");

                    if (k.IsRoot)
                    {
                        klotzCount++;
                        if (k.IsAir)
                        {
                            airCount++;
                        }
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
                    Vector3Int typeSize = KlotzKB.KlotzSize(type);

                    for (int zi = 0; zi < typeSize.z; zi++)
                    {
                        for (int yi = 0; yi < typeSize.y; yi++)
                        {
                            for (int xi = 0; xi < typeSize.x; xi++)
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
