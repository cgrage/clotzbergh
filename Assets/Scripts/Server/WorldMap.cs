using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldMap
{
    private readonly WorldGenerator _generator = WorldGenerator.Default;
    private readonly Dictionary<Vector3Int, WorldChunkState> _worldState = new();
    private readonly ConcurrentDictionary<PlayerId, PlayerWorldMapState> _playerStates = new();

    private readonly CancellationTokenSource _runCancelTS = new();
    private readonly List<Thread> _generatorThreads = new();
    private readonly BlockingCollection<GeneratorThreadArgs> _generationRequestQueue = new();

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

    public void AddPlayer(PlayerId id)
    {
        // Debug.Log($"ServerMap: AddPlayer ${id}");
        _playerStates.TryAdd(id, new()
        {
            PlayerLocation = Vector3Int.zero,
        });
    }

    public void PlayerMoved(PlayerId id, Vector3Int newCoords)
    {
        // Debug.Log($"ServerMap: PlayerMoved ${id} ${newCoords}");
        _playerStates.TryGetValue(id, out PlayerWorldMapState state);

        state.PlayerLocation = newCoords;
        state.ResetChunkPriority(newCoords);
    }

    public void RemovePlayer(PlayerId id)
    {
        _playerStates.TryRemove(id, out _);
    }

    public WorldChunkUpdate GetNextChunkUpdate(PlayerId id)
    {
        _playerStates.TryGetValue(id, out PlayerWorldMapState playerState);

        Vector3Int? next = playerState.GetNextAndSetUpdated(GetWorldChunkStateVersion);
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

    public void PlayerTakeKlotz(PlayerId id, Vector3Int chunkCoords, Vector3Int innerChunkCoords)
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
        catch (OperationCanceledException) { /* see also: Expection anti-pattern */ }
    }
}
