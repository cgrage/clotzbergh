using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Server
{
    public readonly struct ClientId : IEquatable<ClientId>
    {
        private readonly int _value;

        public ClientId(int value)
        {
            _value = value;
        }

        public int Value => _value;

        public override string ToString() => _value.ToString();

        // Implement IEquatable<ClientId>
        public bool Equals(ClientId other) => _value == other._value;
        public override bool Equals(object obj) => obj is ClientId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();

        // Comparison operators
        public static bool operator ==(ClientId left, ClientId right) => left.Equals(right);
        public static bool operator !=(ClientId left, ClientId right) => !left.Equals(right);

        // Explicit conversion from int to ClientId
        public static explicit operator ClientId(int value) => new(value);
    }

    public class ConnectionData
    {
        // public IClientOps Handler { get; set; }
    }

    public class WorldChunkUpdate
    {
        public ChunkCoords Coords { get; set; }
        public ulong Version { get; set; }
        public WorldChunk Chunk { get; set; }
    }

    public class ClientWorldMapState
    {
        public string PlayerName { get; private set; }
        public Vector3 PlayerPosition { get; set; }
        public ChunkCoords PlayerChunkCoords { get; set; }
        public ulong SeenClientListVersion { get; set; }

        private readonly Dictionary<ChunkCoords, PlayerChunkData> _chunkData = new();
        private List<PlayerChunkData> _sortedChunks = new();

        public ClientWorldMapState(string name)
        {
            PlayerName = name;
        }

        public void ResetChunkPriority(ChunkCoords newCoords)
        {
            int intLoadDist = (int)WorldDef.ChunkLoadDistance;

            int xStart = Math.Max(newCoords.X - intLoadDist, WorldDef.Limits.MinCoordsX);
            int xEnd = Math.Min(newCoords.X + intLoadDist, WorldDef.Limits.MaxCoordsX);
            int yStart = Math.Max(newCoords.Y - intLoadDist, WorldDef.Limits.MinCoordsY);
            int yEnd = Math.Min(newCoords.Y + intLoadDist, WorldDef.Limits.MaxCoordsY);
            int zStart = Math.Max(newCoords.Z - intLoadDist, WorldDef.Limits.MinCoordsZ);
            int zEnd = Math.Min(newCoords.Z + intLoadDist, WorldDef.Limits.MaxCoordsZ);

            for (int z = zStart; z <= zEnd; z++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int x = xStart; x <= xEnd; x++)
                    {
                        ChunkCoords chunkCoords = new(x, y, z);

                        float dist = ChunkCoords.Distance(newCoords, chunkCoords);
                        if (dist > WorldDef.ChunkLoadDistance)
                            continue;

                        if (_chunkData.TryGetValue(chunkCoords, out PlayerChunkData thisChunk))
                        {
                            thisChunk.Priority = (int)dist;
                        }
                        else
                        {
                            thisChunk = new PlayerChunkData()
                            {
                                Coords = chunkCoords,
                                Priority = (int)dist,
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
        public ChunkCoords? GetNextAndSetUpdated(Func<ChunkCoords, ulong> worldVersionFunc)
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
        public ChunkCoords Coords { get; set; }
        public int Priority { get; set; }
        public ulong SentOutVersion { get; set; }
    }
}
