using System;
using System.IO;
using UnityEngine;

public class WorldChunk
{
    public const int KlotzCountX = 32; // factors: 1, 2, 4,    8,     16,     32
    public const int KlotzCountY = 80; // factors: 1, 2, 4, 5, 8, 10, 16, 20,     40, 80
    public const int KlotzCountZ = 32; // factors: 1, 2, 4,    8,     16,     32
    //                             common factors: 1, 2, 4,    8,     16

    public const int BorderSize = 1;

    public const int KlotzCountRawX = KlotzCountX + 2 * BorderSize;
    public const int KlotzCountRawY = KlotzCountY + 2 * BorderSize;
    public const int KlotzCountRawZ = KlotzCountZ + 2 * BorderSize;

    public static readonly Vector3Int KlotzCount = new(KlotzCountX, KlotzCountY, KlotzCountZ);
    public static readonly Vector3 Size = new(Klotz.Size.x * KlotzCountX, Klotz.Size.y * KlotzCountY, Klotz.Size.z * KlotzCountZ);

    private readonly Klotz[,,] _dataRaw;

    private WorldChunk()
    {
        _dataRaw = new Klotz[KlotzCountRawX, KlotzCountRawY, KlotzCountRawZ];
    }

    private void FloodFill(KlotzType t, int toHeight = KlotzCountY)
    {
        toHeight += BorderSize;

        for (int z = 0; z < KlotzCountRawZ; z++)
        {
            for (int y = 0; y < toHeight; y++)
            {
                for (int x = 0; x < KlotzCountRawX; x++)
                {
                    _dataRaw[x, y, z].Type = t;
                }
            }
        }
    }

    private void CoreFill(KlotzType t)
    {
        Vector3Int center = new(KlotzCountRawX / 2, KlotzCountRawY / 2, KlotzCountRawZ / 2);

        for (int z = 0; z < KlotzCountRawZ; z++)
        {
            for (int y = 0; y < KlotzCountRawY; y++)
            {
                for (int x = 0; x < KlotzCountRawX; x++)
                {
                    bool inCore =
                         x > KlotzCountRawX / 4 && x < 3 * KlotzCountRawX / 4 &&
                         y > KlotzCountRawY / 4 && y < 3 * KlotzCountRawY / 4 &&
                         z > KlotzCountRawZ / 4 && z < 3 * KlotzCountRawZ / 4;

                    if (inCore) _dataRaw[x, y, z].Type = t;
                }
            }
        }
    }

    public Klotz GetRaw(int x, int y, int z) { return _dataRaw[x, y, z]; }

    public void SetRaw(int x, int y, int z, Klotz t) { _dataRaw[x, y, z] = t; }

    public Klotz Get(int x, int y, int z)
    {
        if (x < -BorderSize || x >= KlotzCountX + BorderSize ||
            y < -BorderSize || y >= KlotzCountY + BorderSize ||
            z < -BorderSize || z >= KlotzCountZ + BorderSize)
            throw new ArgumentOutOfRangeException();

        return _dataRaw[x + BorderSize, y + BorderSize, z + BorderSize];
    }

    public void Set(int x, int y, int z, Klotz t)
    {
        if (x < -BorderSize || x >= KlotzCountX + BorderSize ||
            y < -BorderSize || y >= KlotzCountY + BorderSize ||
            z < -BorderSize || z >= KlotzCountZ + BorderSize)
            throw new ArgumentOutOfRangeException();

        _dataRaw[x + BorderSize, y + BorderSize, z + BorderSize] = t;
    }

    public static WorldChunk CreateEmpty()
    {
        return new WorldChunk();
    }

    public static WorldChunk CreateFloodFilled(KlotzType t, int toHeight = KlotzCountY)
    {
        WorldChunk chunk = new();
        chunk.FloodFill(t, toHeight);
        return chunk;
    }

    public static WorldChunk CreateCoreFilled(KlotzType t)
    {
        WorldChunk chunk = new();
        chunk.CoreFill(t);
        return chunk;
    }

    public void Serialize(BinaryWriter w)
    {
        for (int z = 0; z < KlotzCountRawZ; z++)
        {
            for (int y = 0; y < KlotzCountRawY; y++)
            {
                for (int x = 0; x < KlotzCountRawX; x++)
                {
                    ushort u = (ushort)_dataRaw[x, y, z].Type;
                    w.Write((ushort)_dataRaw[x, y, z].Type);
                }
            }
        }
    }

    public static WorldChunk Deserialize(BinaryReader r)
    {
        WorldChunk chunk = new();
        for (int z = 0; z < KlotzCountRawZ; z++)
        {
            for (int y = 0; y < KlotzCountRawY; y++)
            {
                for (int x = 0; x < KlotzCountRawX; x++)
                {
                    chunk._dataRaw[x, y, z].Type = (KlotzType)r.ReadUInt16();
                }
            }
        }

        return chunk;
    }
}
