using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class WorldMap
{
    private readonly WorldGenerator _generator = new();
    private readonly Dictionary<Vector3Int, WorldChunkState> _worldState = new();
    private readonly Dictionary<ClientId, ClientWorldMapState> _clientStates = new();

    private readonly CancellationTokenSource _runCancelTS = new();
    private readonly List<Thread> _generatorThreads = new();
    private readonly BlockingCollection<GeneratorThreadArgs> _generationRequestQueue = new();
    private ulong _clientListVersion = 0;

    private class GeneratorThreadArgs
    {
        public Vector3Int coords;
    }

    private class WorldChunkState
    {
        public ulong Version { get; set; }
        public WorldChunk Chunk { get; set; }
    }

    public int GeneratorThreadCount = 4;

    public Mesh GeneratePreviewMesh(int dist)
    {
        return _generator.GeneratePreviewMesh(dist);
    }

    private WorldChunkState GetWorldState(Vector3Int coords, bool requestIfNotPresent = false)
    {
        lock (_worldState)
        {
            if (_worldState.TryGetValue(coords, out WorldChunkState chunkState))
                return chunkState;

            if (requestIfNotPresent)
            {
                _generationRequestQueue.Add(new() { coords = coords });
                _worldState.Add(coords, new()
                {
                    Version = 0,
                    Chunk = null,
                });
            }

            return chunkState;
        }
    }

    public ulong GetWorldChunkStateVersion(Vector3Int coords)
    {
        WorldChunkState state = GetWorldState(coords, true);
        if (state == null)
            return 0;

        return state.Version;
    }

    private ClientWorldMapState GetClientState(ClientId id)
    {
        lock (_clientStates)
        {
            return _clientStates[id];
        }
    }

    public void AddClient(ClientId id)
    {
        // Debug.Log($"ServerMap: AddClient ${id}");

        lock (_clientStates)
        {
            bool added = _clientStates.TryAdd(id, new($"Player {id.Value}")
            {
                PlayerPosition = Vector3.zero,
                PlayerChunkCoords = Vector3Int.zero,
            });

            if (!added)
                return;

            _clientListVersion++;
        }
    }

    public void PlayerMoved(ClientId id, Vector3 newPosition)
    {
        // Debug.Log($"ServerMap: PlayerMoved ${id} ${newCoords}");
        ClientWorldMapState state = GetClientState(id);

        Vector3Int newChunkCoords = WorldChunk.PositionToChunkCoords(newPosition);
        bool movedChunk = state.PlayerChunkCoords != newChunkCoords;

        state.PlayerPosition = newPosition;
        state.PlayerChunkCoords = newChunkCoords;

        if (movedChunk)
        {
            state.ResetChunkPriority(newChunkCoords);
        }
    }

    public void RemoveClient(ClientId id)
    {
        lock (_clientStates)
        {
            _clientStates.Remove(id);
            _clientListVersion--;
        }
    }

    /// <summary>
    /// Called by <c>ClientUpdaterThread</c>
    /// </summary>
    public WorldChunkUpdate GetNextChunkUpdate(ClientId id)
    {
        ClientWorldMapState state = GetClientState(id);

        Vector3Int? next = state.GetNextAndSetUpdated(GetWorldChunkStateVersion);
        if (!next.HasValue)
            return null;

        WorldChunkState worldState = GetWorldState(next.Value);
        if (worldState == null || worldState.Version == 0)
            return null;

        return new()
        {
            Coords = next.Value,
            Version = worldState.Version,
            Chunk = worldState.Chunk,
        };
    }

    public ServerStatusUpdate GetNextServerStatus(ClientId id)
    {
        ClientWorldMapState state = GetClientState(id);
        ServerStatusUpdate update = new();

        lock (_clientStates)
        {
            update.PlayerListVersion = _clientListVersion;
            update.PlayerPositions = _clientStates.Values.Select(clientState => clientState.PlayerPosition).ToArray();

            if (state.SeenClientListVersion != _clientListVersion)
            {
                update.PlayerListUpdate = new PlayerListUpdate()
                {
                    PlayerNames = _clientStates.Values.Select(clientState => clientState.PlayerName).ToArray(),
                };

                state.SeenClientListVersion = _clientListVersion;
            }
        }

        return update;
    }

    public void PlayerTakeKlotz(ClientId id, Vector3Int chunkCoords, Vector3Int innerChunkCoords)
    {
        // Debug.Log($"ServerMap: PlayerTakeKlotz ${id} ${chunkCoords} ${innerChunkCoords}");
        WorldChunkState worldState = GetWorldState(chunkCoords);
        if (worldState == null)
        {
            // this should not happen..
            return;
        }

        worldState.Chunk.RemoveKlotz(innerChunkCoords);
        worldState.Version++;
    }

    public void StartGeneratorThreads()
    {
        Debug.LogFormat("World generator threads starting...");

        for (int i = 0; i < GeneratorThreadCount; i++)
        {
            var thread = new Thread(GeneratorThreadMain) { Name = $"GeneratorThread{i}" };
            _generatorThreads.Add(thread);
            thread.Start();
        }

        Debug.LogFormat("World generator threads started");
    }

    public void StopMainThreads()
    {
        Debug.LogFormat("World generator threads stopping...");
        _runCancelTS.Cancel();

        foreach (var thread in _generatorThreads)
        {
            if (!thread.Join(TimeSpan.FromSeconds(1)))
                thread.Abort();
        }

        Debug.LogFormat("World generator threads stopped");
    }

    void GeneratorThreadMain()
    {
        try
        {
            while (!_runCancelTS.Token.IsCancellationRequested)
            {
                GeneratorThreadArgs args = _generationRequestQueue.Take(_runCancelTS.Token);
                WorldChunk chunk = _generator.GetChunk(args.coords);

                lock (_worldState)
                {
                    WorldChunkState state = _worldState[args.coords];
                    if (state.Version != 0)
                        throw new ArgumentException("World chunk was already created by someone else");

                    state.Version = 1;
                    state.Chunk = chunk;
                }
            }
        }
        catch (Exception ex)
        {
            if (_runCancelTS.Token.IsCancellationRequested || ex is ThreadAbortException)
            {
                Debug.LogFormat($"GeneratorThread stopped with exception ({ex.GetType().Name}).");
            }
            else
            {
                Debug.LogException(ex);
                Debug.LogFormat("GeneratorThread stopped on exception (see above).");
            }
        }
    }
}
