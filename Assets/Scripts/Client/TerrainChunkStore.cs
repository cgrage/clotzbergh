using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkStore
{
    public Transform ParentObject { get; set; }
    public IAsyncTerrainOps AsyncTerrainOps { get; set; }
    public Material KlotzMat { get; set; }
    private readonly Dictionary<Vector3Int, TerrainChunk> _dict = new();

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

    private TerrainChunk GetOrCreate(Vector3Int coord)
    {
        if (_dict.TryGetValue(coord, out TerrainChunk entry))
            return entry;

        entry = new TerrainChunk(coord, ParentObject, AsyncTerrainOps, KlotzMat);
        _dict.Add(coord, entry);

        return entry;
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
