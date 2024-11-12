using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct PlayerId
{
    private readonly int _value;

    public PlayerId(int value)
    {
        _value = value;
    }

    public int Value => _value;

    public override string ToString() => _value.ToString();

    // public static implicit operator int(PlayerId id) => id._value;
    public static explicit operator PlayerId(int value) => new(value);
}

public class ConnectionData
{
    // public IClientOps Handler { get; set; }
}

public class WorldChunkUpdate
{
    public Vector3Int Coords { get; set; }
    public ulong Version { get; set; }
    public WorldChunk Chunk { get; set; }
}

public class PlayerWorldMapState
{
    public Vector3 PlayerLocation { get; set; }
    public Vector3Int PlayerChunkLocation { get; set; }
    public readonly Dictionary<Vector3Int, PlayerChunkData> _chunkData = new();
    private List<PlayerChunkData> _sortedChunks = new();

    public PlayerWorldMapState()
    {

    }

    public void ResetChunkPriority(Vector3Int newCoords)
    {
        int loadDist = WorldDef.ChunkLoadDistance;

        int xStart = Math.Max(newCoords.x - loadDist, WorldDef.Limits.MinCoordsX);
        int xEnd = Math.Min(newCoords.x + loadDist, WorldDef.Limits.MaxCoordsX);
        int yStart = Math.Max(newCoords.y - loadDist, WorldDef.Limits.MinCoordsY);
        int yEnd = Math.Min(newCoords.y + loadDist, WorldDef.Limits.MaxCoordsY);
        int zStart = Math.Max(newCoords.z - loadDist, WorldDef.Limits.MinCoordsZ);
        int zEnd = Math.Min(newCoords.z + loadDist, WorldDef.Limits.MaxCoordsZ);

        for (int z = zStart; z <= zEnd; z++)
        {
            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    Vector3Int chunkCoords = new(x, y, z);

                    int dist = WorldChunk.ChunkDistance(newCoords, chunkCoords);
                    if (dist > loadDist)
                        continue;

                    if (_chunkData.TryGetValue(chunkCoords, out PlayerChunkData thisChunk))
                    {
                        thisChunk.Priority = dist;
                    }
                    else
                    {
                        thisChunk = new PlayerChunkData()
                        {
                            Coords = chunkCoords,
                            Priority = dist,
                            SentOutVersion = 0,
                        };
                        _chunkData.Add(chunkCoords, thisChunk);
                    }
                }
            }
        }

        List<PlayerChunkData> sorted = new(_chunkData.Values);
        sorted.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _sortedChunks = sorted;
    }

    /// <summary>
    /// Called by <c>ClientUpdaterThread</c>
    /// </summary>
    public Vector3Int? GetNextAndSetUpdated(Func<Vector3Int, ulong> worldVersionFunc)
    {
        foreach (var chunk in _sortedChunks)
        {
            ulong worldVersion = worldVersionFunc(chunk.Coords);
            if (chunk.SentOutVersion < worldVersion)
            {
                chunk.SentOutVersion = worldVersion;
                return chunk.Coords;
            }
        }

        return null;
    }
}

public class PlayerChunkData
{
    public Vector3Int Coords { get; set; }
    public int Priority { get; set; }
    public ulong SentOutVersion { get; set; }
}
