using NUnit.Framework;
using Clotzbergh.Server.WorldGeneration;
using Clotzbergh;
using System.Collections.Generic;
using UnityEngine;

public class WorldGeneratorHelperTests
{
    [Test]
    public void UniqueColor_IsDeterministic()
    {
        for (int z = -5; z <= 5; z++)
        {
            for (int y = -5; y <= 5; y++)
            {
                for (int x = -5; x <= 5; x++)
                {
                    var c1 = WorldGenerator.UniqueColor(x, y, z);
                    var c2 = WorldGenerator.UniqueColor(x, y, z);

                    Assert.AreEqual(c1, c2, $"Non-deterministic color at ({x},{y},{z})");
                }
            }
        }
    }

    [Test]
    public void UniqueColor_NeighborsRarelyMatch()
    {
        int matches = 0;
        int comparisons = 0;

        for (int z = -5; z <= 5; z++)
        {
            for (int y = -5; y <= 5; y++)
            {
                for (int x = -5; x <= 5; x++)
                {
                    var center = WorldGenerator.UniqueColor(x, y, z);

                    var neighbors = new[]
                    {
                        WorldGenerator.UniqueColor(x + 1, y, z),
                        WorldGenerator.UniqueColor(x - 1, y, z),
                        WorldGenerator.UniqueColor(x, y + 1, z),
                        WorldGenerator.UniqueColor(x, y - 1, z),
                        WorldGenerator.UniqueColor(x, y, z + 1),
                        WorldGenerator.UniqueColor(x, y, z - 1),
                    };

                    foreach (var n in neighbors)
                    {
                        comparisons++;
                        if (n.Equals(center))
                            matches++;
                    }
                }
            }
        }

        float ratio = (float)matches / comparisons;

        // Allow some collisions, but not many
        Assert.Less(ratio, 0.15f, $"Too many neighbor color collisions: {ratio:P}");
    }

    [Test]
    public void UniqueColor_UsesMultipleColors()
    {
        var used = new HashSet<KlotzColor>();

        for (int z = -10; z <= 10; z++)
        {
            for (int y = -10; y <= 10; y++)
            {
                for (int x = -10; x <= 10; x++)
                {
                    used.Add(WorldGenerator.UniqueColor(x, y, z));
                }
            }
        }

        Assert.Greater(used.Count, 1, "Only one color used");
        Assert.Greater(
            used.Count,
            (int)KlotzColor.Count / 2,
            "Color distribution too narrow");
    }

    [Test]
    public void UniqueColor_NoCenterBridge()
    {
        int matches = 0;
        int comparisons = 0;

        for (int z = -5; z <= 5; z++)
        {
            var c1 = WorldGenerator.UniqueColor(0, -1, z);
            var c2 = WorldGenerator.UniqueColor(-1, -1, z);
            comparisons++;
            if (c2.Equals(c1))
                matches++;
        }

        for (int x = -5; x <= 5; x++)
        {
            var c1 = WorldGenerator.UniqueColor(x, -1, 0);
            var c2 = WorldGenerator.UniqueColor(x, -1, -1);
            comparisons++;
            if (c2.Equals(c1))
                matches++;
        }

        float ratio = (float)matches / comparisons;

        // Allow some collisions, but not many
        Assert.Less(ratio, 0.15f, $"Too many neighbor color collisions: {ratio:P}");
    }

    [Test]
    public void ColorByChunk_NoCenterBridge()
    {
        var c1 = WorldGenerator.ColorByChunk(-1, -1, -1);
        var c2 = WorldGenerator.ColorByChunk(-1, -1, 1);
        var c3 = WorldGenerator.ColorByChunk(1, -1, 1);
        var c4 = WorldGenerator.ColorByChunk(1, -1, -1);

        Assert.AreNotEqual(c1, c2, "Color bridge detected between (-1,-1,-1) and (-1,-1,1)");
        Assert.AreNotEqual(c2, c3, "Color bridge detected between (-1,-1,1) and (1,-1,1)");
        Assert.AreNotEqual(c3, c4, "Color bridge detected between (1,-1,1) and (1,-1,-1)");
        Assert.AreNotEqual(c4, c1, "Color bridge detected between (1,-1,-1) and (-1,-1,-1)");
    }
}

