using Clotzbergh;
using NUnit.Framework;
using UnityEngine;

public class WorldChunkTests
{
    private static readonly float X_UNIT = WorldDef.ChunkSize.x;
    private static readonly float Y_UNIT = WorldDef.ChunkSize.y;
    private static readonly float Z_UNIT = WorldDef.ChunkSize.z;

    private static readonly float X_UNIT_HALF = X_UNIT / 2;
    private static readonly float Y_UNIT_HALF = Y_UNIT / 2;
    private static readonly float Z_UNIT_HALF = Z_UNIT / 2;

    [Test]
    public void PositionToChunkCoords()
    {
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, 0, 0)), Is.EqualTo(new Vector3Int(0, 0, 0)));

        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(X_UNIT_HALF, 0, 0)), Is.EqualTo(new Vector3Int(0, 0, 0)));
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, Y_UNIT_HALF, 0)), Is.EqualTo(new Vector3Int(0, 0, 0)));
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, 0, Z_UNIT_HALF)), Is.EqualTo(new Vector3Int(0, 0, 0)));

        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(3 * X_UNIT_HALF, 0, 0)), Is.EqualTo(new Vector3Int(1, 0, 0)));
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, 3 * Y_UNIT_HALF, 0)), Is.EqualTo(new Vector3Int(0, 1, 0)));
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, 0, 3 * Z_UNIT_HALF)), Is.EqualTo(new Vector3Int(0, 0, 1)));

        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(-X_UNIT_HALF, 0, 0)), Is.EqualTo(new Vector3Int(-1, 0, 0)));
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, -Y_UNIT_HALF, 0)), Is.EqualTo(new Vector3Int(0, -1, 0)));
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, 0, -Z_UNIT_HALF)), Is.EqualTo(new Vector3Int(0, 0, -1)));

        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(3 * -X_UNIT_HALF, 0, 0)), Is.EqualTo(new Vector3Int(-2, 0, 0)));
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, 3 * -Y_UNIT_HALF, 0)), Is.EqualTo(new Vector3Int(0, -2, 0)));
        Assert.That(WorldChunk.PositionToChunkCoords(new Vector3(0, 0, 3 * -Z_UNIT_HALF)), Is.EqualTo(new Vector3Int(0, 0, -2)));
    }

    [Test]
    public void ChunkCoordsToPosition()
    {
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, 0, 0)), Is.EqualTo(new Vector3(0, 0, 0)));

        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(1, 0, 0)), Is.EqualTo(new Vector3(X_UNIT, 0, 0)));
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, 1, 0)), Is.EqualTo(new Vector3(0, Y_UNIT, 0)));
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, 0, 1)), Is.EqualTo(new Vector3(0, 0, Z_UNIT)));

        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(-1, 0, 0)), Is.EqualTo(new Vector3(-X_UNIT, 0, 0)));
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, -1, 0)), Is.EqualTo(new Vector3(0, -Y_UNIT, 0)));
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, 0, -1)), Is.EqualTo(new Vector3(0, 0, -Z_UNIT)));

        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(2, 0, 0)), Is.EqualTo(new Vector3(X_UNIT * 2, 0, 0)));
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, 2, 0)), Is.EqualTo(new Vector3(0, Y_UNIT * 2, 0)));
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, 0, 2)), Is.EqualTo(new Vector3(0, 0, Z_UNIT * 2)));

        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(-2, 0, 0)), Is.EqualTo(new Vector3(-X_UNIT * 2, 0, 0)));
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, -2, 0)), Is.EqualTo(new Vector3(0, -Y_UNIT * 2, 0)));
        Assert.That(WorldChunk.ChunkCoordsToPosition(new Vector3Int(0, 0, -2)), Is.EqualTo(new Vector3(0, 0, -Z_UNIT * 2)));
    }

    [Test]
    public void DistanceToChunkCenter()
    {
        float expectedDistance = Mathf.Sqrt(X_UNIT * X_UNIT + Y_UNIT * Y_UNIT + Z_UNIT * Z_UNIT) / 2;
        float actualDistance = WorldChunk.DistanceToChunkCenter(new Vector3(0, 0, 0), new Vector3Int(0, 0, 0));

        Assert.AreEqual(expectedDistance, actualDistance);
    }

    [Test]
    public void ChunkDistance()
    {
        Vector3Int coords = new(0, 0, 0);

        Assert.AreEqual(0, WorldChunk.ChunkDistance(coords, new Vector3Int(0, 0, 0)));
        Assert.AreEqual(1, WorldChunk.ChunkDistance(coords, new Vector3Int(1, 0, 0)));

        Assert.AreEqual(100, WorldChunk.ChunkDistance(coords, new Vector3Int(100, 0, 0)));
        Assert.AreEqual(100, WorldChunk.ChunkDistance(coords, new Vector3Int(0, 100, 0)));
        Assert.AreEqual(100, WorldChunk.ChunkDistance(coords, new Vector3Int(0, 0, 100)));

        Assert.AreEqual(100, WorldChunk.ChunkDistance(coords, new Vector3Int(-100, 0, 0)));
        Assert.AreEqual(100, WorldChunk.ChunkDistance(coords, new Vector3Int(0, -100, 0)));
        Assert.AreEqual(100, WorldChunk.ChunkDistance(coords, new Vector3Int(0, 0, -100)));

        coords = new(100, 0, 0);

        Assert.AreEqual(100, WorldChunk.ChunkDistance(coords, new Vector3Int(0, 0, 0)));

        Assert.AreEqual(99, WorldChunk.ChunkDistance(coords, new Vector3Int(1, 0, 0)));

        Assert.AreEqual(0, WorldChunk.ChunkDistance(coords, new Vector3Int(100, 0, 0)));
        Assert.AreEqual(141, WorldChunk.ChunkDistance(coords, new Vector3Int(0, 100, 0)));
        Assert.AreEqual(141, WorldChunk.ChunkDistance(coords, new Vector3Int(0, 0, 100)));

        Assert.AreEqual(200, WorldChunk.ChunkDistance(coords, new Vector3Int(-100, 0, 0)));
        Assert.AreEqual(141, WorldChunk.ChunkDistance(coords, new Vector3Int(0, -100, 0)));
        Assert.AreEqual(141, WorldChunk.ChunkDistance(coords, new Vector3Int(0, 0, -100)));
    }

    [Test]
    public void TrivialBrickPlacement()
    {
        TestBrickPlacement(KlotzType.Brick2x2, 2, 2);
        TestBrickPlacement(KlotzType.Brick1x8, 8, 1);
    }

    public void TestBrickPlacement(KlotzType type, int dimX, int dimZ)
    {
        WorldChunk chunk = new();
        chunk.PlaceKlotz(type, KlotzColor.White, KlotzVariant.Zero, Vector3Int.zero, KlotzDirection.ToPosX);

        SubKlotz k = chunk.Get(0, 0, 0);
        Assert.IsTrue(k.IsRoot);
        Assert.AreEqual(type, k.Type);
        Assert.AreEqual(KlotzColor.White, k.Color, "At root-klotz");
        Assert.AreEqual(KlotzVariant.Zero, k.Variant, "At root-klotz");
        Assert.AreEqual(KlotzDirection.ToPosX, k.Direction, "At root-klotz");

        for (int z = 0; z < dimZ; z++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < dimX; x++)
                {
                    Vector3Int coords = new(x, y, z);
                    if (coords == Vector3Int.zero)
                        continue; // skip root-klotz

                    k = chunk.Get(coords);
                    Assert.IsFalse(k.IsRoot, $"Expected non-root at {coords} ({k.Type})");
                    Assert.AreEqual(KlotzDirection.ToPosX, k.Direction, $"Unexpected direction at {coords}");
                    Assert.AreEqual(x, k.SubKlotzIndexX, $"Unexpected SubKlotzIndexX {coords}");
                    Assert.AreEqual(y, k.SubKlotzIndexY, $"Unexpected SubKlotzIndexY {coords}");
                    Assert.AreEqual(z, k.SubKlotzIndexZ, $"Unexpected SubKlotzIndexZ {coords}");
                }
            }
        }
    }

    [Test]
    public void PatternedBrickPlacement()
    {
        WorldChunk chunk = new();
        KlotzColor[] zColors = new KlotzColor[] {
            KlotzColor.White, KlotzColor.Gray, KlotzColor.Black, KlotzColor.Red,
            KlotzColor.Blue, KlotzColor.Yellow, KlotzColor.Green, KlotzColor.Azure };

        chunk.PlaceKlotz(KlotzType.Brick1x8, zColors[0], KlotzVariant.Zero, new(0, 0, 0), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick1x8, zColors[1], KlotzVariant.Zero, new(7, 0, 1), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick1x8, zColors[2], KlotzVariant.Zero, new(0, 0, 2), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick1x8, zColors[3], KlotzVariant.Zero, new(7, 0, 3), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick1x8, zColors[4], KlotzVariant.Zero, new(0, 0, 4), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick1x8, zColors[5], KlotzVariant.Zero, new(7, 0, 5), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick1x8, zColors[6], KlotzVariant.Zero, new(0, 0, 6), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick1x8, zColors[7], KlotzVariant.Zero, new(7, 0, 7), KlotzDirection.ToNegX);

        for (int z = 0; z < 8; z++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Vector3Int coords = new(x, y, z);
                    bool isZEven = z % 2 == 0;

                    bool expectRoot = (y == 0) && ((x == 0 && isZEven) || (x == 7 && !isZEven));
                    KlotzType expectedType = KlotzType.Brick1x8;
                    KlotzColor expectedColor = zColors[z];
                    KlotzDirection expectedDir = isZEven ? KlotzDirection.ToPosX : KlotzDirection.ToNegX;

                    SubKlotz k = chunk.Get(coords);
                    if (expectRoot)
                    {
                        Assert.IsTrue(k.IsRoot, $"Expected root at {coords}");
                        Assert.AreEqual(expectedType, k.Type);
                        Assert.AreEqual(expectedColor, k.Color);
                    }
                    else
                    {
                        Assert.IsFalse(k.IsRoot, $"Expected non-root at {coords}");
                    }

                    Assert.AreEqual(expectedDir, k.Direction, $"Unexpected direction at {coords}");

                    // --- root ---
                    Vector3Int rootPos = k.RootPos(coords);

                    // root-pos
                    Assert.AreEqual(z % 2 == 0 ? 0 : 7, rootPos.x);
                    Assert.AreEqual(0, rootPos.y);
                    Assert.AreEqual(z, rootPos.z);

                    SubKlotz kRoot = chunk.Get(rootPos);
                    Assert.AreEqual(KlotzType.Brick1x8, kRoot.Type);
                    Assert.AreEqual(zColors[z], kRoot.Color);
                }
            }
        }
    }
}
