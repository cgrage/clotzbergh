using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldChunk
{
    public const int ChunkWidth = 32;

    public const int ChunkHeight = 80;

    public const int ChunkDepth = 32;

    public static readonly Vector3Int ChunkSizeInt = new(ChunkWidth, ChunkHeight, ChunkDepth);
    public static readonly Vector3 ChunkSize = new(ChunkWidth, ChunkHeight, ChunkDepth);
    public static readonly Vector3 Size = new(Klotz.Size.x * ChunkWidth, Klotz.Size.y * ChunkHeight, Klotz.Size.z * ChunkDepth);

    private readonly Klotz[,,] _world;

    private WorldChunk()
    {
        _world = new Klotz[ChunkWidth, ChunkHeight, ChunkDepth];
    }

    private void Fill(KlotzType t)
    {
        for (int z = 0; z < ChunkDepth; z++)
        {
            for (int y = 0; y < ChunkHeight; y++)
            {
                for (int x = 0; x < ChunkWidth; x++)
                {
                    _world[x, y, z].Type = t;
                }
            }
        }
    }

    private static WorldChunk CreateEmpty()
    {
        return new WorldChunk();
    }
}
