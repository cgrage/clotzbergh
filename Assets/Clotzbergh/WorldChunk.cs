using System.IO;
using UnityEngine;

public class WorldChunk
{
    public const int KlotzCountX = 32;

    public const int KlotzCountY = 80;

    public const int KlotzCountZ = 32;

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

    private void Fill(KlotzType t, int toHeight = KlotzCountY)
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

    public KlotzType GetRaw(int x, int y, int z) { return _dataRaw[x, y, z].Type; }

    public void SetRaw(int x, int y, int z, KlotzType t) { _dataRaw[x, y, z].Type = t; }

    public KlotzType Get(int x, int y, int z) { return _dataRaw[x + BorderSize, y + BorderSize, z + BorderSize].Type; }

    public void Set(int x, int y, int z, KlotzType t) { _dataRaw[x + BorderSize, y + BorderSize, z + BorderSize].Type = t; }

    public static WorldChunk CreateEmpty()
    {
        return new WorldChunk();
    }

    public static WorldChunk CreateFilled(KlotzType t, int toHeight = KlotzCountY)
    {
        WorldChunk chunk = new();
        chunk.Fill(t, toHeight);
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
