using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkStore
{
    public Transform ParentObject { get; set; }
    public IAsyncTerrainOps AsyncTerrainOps { get; set; }
    public Material KlotzMat { get; set; }

    private readonly Dictionary<Vector3Int, TerrainChunk> _dict = new();

    // _activeChunks is sorted by their priority
    private readonly List<TerrainChunk> _activeChunks = new();

    public int ChunkCount { get => _dict.Count; }
    public int ActiveChunkCount { get => _activeChunks.Count; }

    /// <summary>
    /// 
    /// </summary>
    public void OnUpdate()
    {
        int reqCount = 0;

        foreach (var chunk in _activeChunks)
        {
            if (reqCount < 5 && chunk.RequestWorldIfNeeded())
                reqCount++;

            chunk.RequestMeshUpdatesIfNeeded();
        }
    }

    /// <summary>
    /// This method is expected to be run on main thread.
    /// </summary>
    public void OnViewerMoved(Vector3Int newCoords)
    {
        int loadDist = WorldDef.ChunkLoadDistance;

        // HashSet<TerrainChunk> killList = new(_activeChunks);

        for (int z = newCoords.z - loadDist; z <= newCoords.z + loadDist; z++)
        {
            for (int y = newCoords.y - loadDist; y <= newCoords.y + loadDist; y++)
            {
                for (int x = newCoords.x - loadDist; x <= newCoords.x + loadDist; x++)
                {
                    Vector3Int chunkCoords = new(x, y, z);
                    int dist = WorldChunk.ChunkDistance(newCoords, chunkCoords);

                    if (dist <= loadDist)
                    {
                        var chunk = GetOrCreate(chunkCoords);
                        chunk.OnViewerMoved(dist);

                        if (chunk.IsActive)
                        {
                            if (!_activeChunks.Contains(chunk))
                                _activeChunks.Add(chunk);
                        }
                        else
                        {
                            _activeChunks.Remove(chunk);
                        }

                        // killList.Remove(chunk);
                    }
                }
            }
        }

        _activeChunks.Sort((a, b) => a.LoadPriority.CompareTo(b.LoadPriority));

        // foreach (var chunk in killList)
        // {
        //     _dict.Remove(chunk.Coords);
        //     _activeChunks.Remove(chunk);
        //
        //     chunk.CleanUp();
        // }
    }

    public void OnWorldChunkReceived(Vector3Int coords, WorldChunk chunk)
    {
        GetOrCreate(coords).OnWorldUpdate(chunk);
    }

    /// <summary>
    /// Tries to find the <c>TerrainChunk</c> with the given coords. If it cannot be found the <c>TerrainChunk</c> is 
    /// created.
    /// This method shall be the only method that creates <c>TerrainChunk</c>s.
    /// This method is expected to be run on main thread.
    /// </summary>
    private TerrainChunk GetOrCreate(Vector3Int coords)
    {
        // try to find the existing
        if (_dict.TryGetValue(coords, out TerrainChunk thisChunk))
            return thisChunk;

        // nothing found there. we need a new one.
        // Debug.Log($"Create new terrain chunk ${coords}");

        thisChunk = new TerrainChunk(coords, ParentObject, AsyncTerrainOps, KlotzMat);
        _dict.Add(coords, thisChunk);

        // find the neighbors
        _dict.TryGetValue(new Vector3Int(coords.x - 1, coords.y, coords.z), out TerrainChunk neighborXM1);
        _dict.TryGetValue(new Vector3Int(coords.x + 1, coords.y, coords.z), out TerrainChunk neighborXP1);
        _dict.TryGetValue(new Vector3Int(coords.x, coords.y - 1, coords.z), out TerrainChunk neighborYM1);
        _dict.TryGetValue(new Vector3Int(coords.x, coords.y + 1, coords.z), out TerrainChunk neighborYP1);
        _dict.TryGetValue(new Vector3Int(coords.x, coords.y, coords.z - 1), out TerrainChunk neighborZM1);
        _dict.TryGetValue(new Vector3Int(coords.x, coords.y, coords.z + 1), out TerrainChunk neighborZP1);

        // and link the neighbors
        if (neighborXM1 != null) { thisChunk.NeighborXM1 = neighborXM1; neighborXM1.NeighborXP1 = thisChunk; }
        if (neighborXP1 != null) { thisChunk.NeighborXP1 = neighborXP1; neighborXP1.NeighborXM1 = thisChunk; }
        if (neighborYM1 != null) { thisChunk.NeighborYM1 = neighborYM1; neighborYM1.NeighborYP1 = thisChunk; }
        if (neighborYP1 != null) { thisChunk.NeighborYP1 = neighborYP1; neighborYP1.NeighborYM1 = thisChunk; }
        if (neighborZM1 != null) { thisChunk.NeighborZM1 = neighborZM1; neighborZM1.NeighborZP1 = thisChunk; }
        if (neighborZP1 != null) { thisChunk.NeighborZP1 = neighborZP1; neighborZP1.NeighborZM1 = thisChunk; }

        return thisChunk;
    }
}
