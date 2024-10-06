using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkStore
{
    public Transform ParentObject { get; set; }
    public IAsyncTerrainOps AsyncTerrainOps { get; set; }
    public Material KlotzMat { get; set; }
    private readonly Dictionary<Vector3Int, TerrainChunk> _dict = new();

    /// <summary>
    /// This method is expected to be run on main thread.
    /// </summary>
    public void OnViewerMoved(Vector3 viewerPos)
    {
        var affectedChunks = AllActiveChunks;

        int currentChunkCoordX = (int)(viewerPos.x / WorldChunk.Size.x);
        int currentChunkCoordY = (int)(viewerPos.y / WorldChunk.Size.y);
        int currentChunkCoordZ = (int)(viewerPos.z / WorldChunk.Size.z);

        int chunksVisibleInViewDistX = Mathf.RoundToInt(TerrainChunk.MaxViewDist / WorldChunk.Size.x);
        int chunksVisibleInViewDistY = Mathf.RoundToInt(TerrainChunk.MaxViewDist / WorldChunk.Size.y);
        int chunksVisibleInViewDistZ = Mathf.RoundToInt(TerrainChunk.MaxViewDist / WorldChunk.Size.z);

        for (int zOffset = -chunksVisibleInViewDistZ; zOffset <= chunksVisibleInViewDistY; zOffset++)
        {
            for (int yOffset = -chunksVisibleInViewDistY; yOffset <= chunksVisibleInViewDistY; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDistX; xOffset <= chunksVisibleInViewDistX; xOffset++)
                {
                    Vector3Int viewedChunkCoord = new(
                        currentChunkCoordX + xOffset,
                        currentChunkCoordY + yOffset,
                        currentChunkCoordZ + zOffset);

                    var chunk = GetOrCreate(viewedChunkCoord);
                    if (!affectedChunks.Contains(chunk))
                        affectedChunks.Add(chunk);
                }
            }
        }

        foreach (var chunk in affectedChunks)
        {
            chunk.UpdateLevelOfDetail(viewerPos);
        }
    }

    public void OnWorldChunkReceived(Vector3Int coord, WorldChunk chunk, Vector3 viewerPos)
    {
        GetOrCreate(coord).OnWorldChunkReceived(chunk, viewerPos);
    }

    /// <summary>
    /// Tries to find the <c>TerrainChunk</c> with the given coords. If it cannot be found the <c>TerrainChunk</c> is 
    /// created.
    /// This method shall be the only method that creates <c>TerrainChunk</c>s.
    /// This method is expected to be run on main thread.
    /// </summary>
    private TerrainChunk GetOrCreate(Vector3Int coord)
    {
        // try to find the existing
        if (_dict.TryGetValue(coord, out TerrainChunk thisChunk))
            return thisChunk;

        // nothing found there. we need a new one.
        thisChunk = new TerrainChunk(coord, ParentObject, AsyncTerrainOps, KlotzMat);
        _dict.Add(coord, thisChunk);

        // find the neighbors
        _dict.TryGetValue(new Vector3Int(coord.x - 1, coord.y, coord.z), out TerrainChunk neighborXM1);
        _dict.TryGetValue(new Vector3Int(coord.x + 1, coord.y, coord.z), out TerrainChunk neighborXP1);
        _dict.TryGetValue(new Vector3Int(coord.x, coord.y - 1, coord.z), out TerrainChunk neighborYM1);
        _dict.TryGetValue(new Vector3Int(coord.x, coord.y + 1, coord.z), out TerrainChunk neighborYP1);
        _dict.TryGetValue(new Vector3Int(coord.x, coord.y, coord.z - 1), out TerrainChunk neighborZM1);
        _dict.TryGetValue(new Vector3Int(coord.x, coord.y, coord.z + 1), out TerrainChunk neighborZP1);

        // and link the neighbors
        if (neighborXM1 != null) { thisChunk.NeighborXM1 = neighborXM1; neighborXM1.NeighborXP1 = thisChunk; }
        if (neighborXP1 != null) { thisChunk.NeighborXP1 = neighborXP1; neighborXP1.NeighborXM1 = thisChunk; }
        if (neighborYM1 != null) { thisChunk.NeighborYM1 = neighborYM1; neighborYM1.NeighborYP1 = thisChunk; }
        if (neighborYP1 != null) { thisChunk.NeighborYP1 = neighborYP1; neighborYP1.NeighborYM1 = thisChunk; }
        if (neighborZM1 != null) { thisChunk.NeighborZM1 = neighborZM1; neighborZM1.NeighborZP1 = thisChunk; }
        if (neighborZP1 != null) { thisChunk.NeighborZP1 = neighborZP1; neighborZP1.NeighborZM1 = thisChunk; }

        return thisChunk;
    }

    public List<TerrainChunk> AllActiveChunks
    {
        get
        {
            List<TerrainChunk> chunks = new();
            foreach (var chunk in _dict.Values)
            {
                if (chunk.IsActive)
                    chunks.Add(chunk);
            }

            return chunks;
        }
    }
}
