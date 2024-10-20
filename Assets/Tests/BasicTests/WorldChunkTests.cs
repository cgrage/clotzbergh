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

}
