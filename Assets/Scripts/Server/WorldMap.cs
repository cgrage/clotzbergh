using System;
using UnityEngine;

public class WorldMap
{
    private readonly WorldGenerator _generator = new();
    private WorldChunkUpdate _update;

    public Mesh GeneratePreviewMesh(int dist)
    {
        return _generator.GeneratePreviewMesh(dist);
    }

    public void AddPlayer(PlayerId id, Vector3Int initialCoords)
    {
        _update = new WorldChunkUpdate()
        {
            Coords = initialCoords,
            Version = 1,
            Chunk = _generator.GetChunk(initialCoords),
        };
    }

    public void RemovePlayer(PlayerId id)
    {

    }

    public WorldChunkUpdate GetNextChunkUpdate(PlayerId id)
    {
        WorldChunkUpdate update = _update;
        // _update = null;
        return update;
    }
}