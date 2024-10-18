using System.Collections.Concurrent;
using UnityEngine;

public class WorldMap
{
    private readonly WorldGenerator _generator = new();
    private readonly ConcurrentDictionary<Vector3Int, WorldChunkState> _worldState = new();
    private readonly ConcurrentDictionary<PlayerId, PlayerWorldMapState> _playerStates = new();

    public Mesh GeneratePreviewMesh(int dist)
    {
        return _generator.GeneratePreviewMesh(dist);
    }

    public WorldChunkState GetWorldState(Vector3Int coords)
    {
        // TODO LOCKING!

        if (_worldState.TryGetValue(coords, out WorldChunkState chunkState))
            return chunkState;

        chunkState = new()
        {
            Version = 1,
            Chunk = _generator.GetChunk(coords),
        };

        _worldState.TryAdd(coords, chunkState);
        return chunkState;
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

        Vector3Int? next = playerState.GetNextToUpdate((at) => GetWorldState(at).Version);
        if (!next.HasValue)
            return null;

        Vector3Int coords = next.Value;
        playerState.SetSentOutVersion(coords, 1);

        WorldChunkState worldState = GetWorldState(coords);

        return new()
        {
            Coords = coords,
            Version = worldState.Version,
            Chunk = worldState.Chunk,
        };
    }
}
