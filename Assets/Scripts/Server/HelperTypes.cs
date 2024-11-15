using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public readonly struct ClientId
{
    private readonly int _value;

    public ClientId(int value)
    {
        _value = value;
    }

    public int Value => _value;

    public override string ToString() => _value.ToString();

    // public static implicit operator int(PlayerId id) => id._value;
    public static explicit operator ClientId(int value) => new(value);
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

public class ClientWorldMapState
{
    public string PlayerName { get; private set; }
    public Vector3 PlayerPosition { get; set; }
    public Vector3Int PlayerChunkCoords { get; set; }
    public ulong SeenClientListVersion { get; set; }

    private readonly Dictionary<Vector3Int, PlayerChunkData> _chunkData = new();
    private List<PlayerChunkData> _sortedChunks = new();

    public ClientWorldMapState(string name)
    {
        PlayerName = name;
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

public class PlayerListUpdate
{
    public string[] PlayerNames { get; set; }
}

public class ServerStatusUpdate
{
    public ulong PlayerListVersion { get; set; }
    public Vector3[] PlayerPositions { get; set; }
    public PlayerListUpdate PlayerListUpdate { get; set; }

    public void Serialize(BinaryWriter w)
    {
        w.Write(PlayerListVersion);
        w.Write(PlayerPositions.Length);
        foreach (var pos in PlayerPositions)
        {
            w.Write(pos.x);
            w.Write(pos.y);
            w.Write(pos.z);
        }
        if (PlayerListUpdate != null)
        {
            w.Write((byte)1);
            foreach (var name in PlayerListUpdate.PlayerNames)
            {
                w.Write(name);
            }
        }
        else
        {
            w.Write((byte)0);
        }
    }

    public static ServerStatusUpdate Deserialize(BinaryReader reader)
    {
        ulong playerListVersion = reader.ReadUInt64();
        int playerCount = reader.ReadInt32();
        Vector3[] playerPositions = new Vector3[playerCount];
        PlayerListUpdate playerListUpdate = null;
        for (int i = 0; i < playerCount; i++)
        {
            playerPositions[i] = new(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
        }
        byte flags = reader.ReadByte();
        if (flags > 0)
        {
            string[] playerNames = new string[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                playerNames[i] = reader.ReadString();
            }

            playerListUpdate = new()
            {
                PlayerNames = playerNames,
            };
        }

        return new ServerStatusUpdate()
        {
            PlayerListVersion = playerListVersion,
            PlayerPositions = playerPositions,
            PlayerListUpdate = playerListUpdate,
        };
    }
}
