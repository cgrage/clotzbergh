using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkStore
{

    public Transform ParentObject { get; set; }
    private readonly Dictionary<Vector3Int, TerrainChunk> _dict = new();

    public void ConsiderLoading(Vector3Int chunkCoord, Vector3 viewerPos)
    {
        if (_dict.TryGetValue(chunkCoord, out TerrainChunk entry))
        {
            // entry.UpdateTerrainChunk();
        }
        else
        {
            _dict.Add(chunkCoord, new TerrainChunk(chunkCoord, /*chunkSize, detailLevels,*/ ParentObject));
        }
    }
}
