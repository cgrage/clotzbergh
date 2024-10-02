using System;
using System.IO;
using UnityEngine;

public class WorldChunk
{
    public const int KlotzCountX = 32;
    public const int KlotzCountY = 80;
    public const int KlotzCountZ = 32;

    // factors of 32:  1, 2, 4,    8,     16,     32
    // factors of 80:  1, 2, 4, 5, 8, 10, 16, 20,     40, 80
    // --------------  -------------------------------------
    // common factors: 1, 2, 4,    8,     16

    public const int BorderSize = 1;

    public const int KlotzCountRawX = KlotzCountX + 2 * BorderSize;
    public const int KlotzCountRawY = KlotzCountY + 2 * BorderSize;
    public const int KlotzCountRawZ = KlotzCountZ + 2 * BorderSize;

    public static readonly Vector3Int KlotzCount = new(KlotzCountX, KlotzCountY, KlotzCountZ);

    /// <summary>
    /// 32 * 0.36 = 80 * 0.144 = 11,52
    /// </summary>
    public static readonly Vector3 Size = new(Klotz.Size.x * KlotzCountX, Klotz.Size.y * KlotzCountY, Klotz.Size.z * KlotzCountZ);

    private readonly Klotz[,,] _dataRaw;

    private WorldChunk()
    {
        _dataRaw = new Klotz[KlotzCountRawX, KlotzCountRawY, KlotzCountRawZ];
    }

    private void FloodFill(int toHeight = KlotzCountY, bool inclNextUpper = true)
    {
        toHeight += BorderSize;
        if (inclNextUpper) toHeight += BorderSize;

        for (int z = 0; z < KlotzCountRawZ; z++)
        {
            for (int y = 0; y < toHeight; y++)
            {
                for (int x = 0; x < KlotzCountRawX; x++)
                {
                    _dataRaw[x, y, z] = new Klotz(
                        KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
                }
            }
        }
    }

    private void CoreFill()
    {
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

                    if (inCore) _dataRaw[x, y, z] = new Klotz(
                        KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
                }
            }
        }
    }

    public Klotz GetRaw(int rawx, int rawy, int rawz) { return _dataRaw[rawx, rawy, rawz]; }

    public void SetRaw(int rawx, int rawy, int rawz, Klotz t) { _dataRaw[rawx, rawy, rawz] = t; }

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

    public static WorldChunk CreateFloodFilled(int toHeight = KlotzCountY, bool inclNextUpper = true)
    {
        WorldChunk chunk = new();
        chunk.FloodFill(toHeight, inclNextUpper);
        return chunk;
    }

    public static WorldChunk CreateCoreFilled()
    {
        WorldChunk chunk = new();
        chunk.CoreFill();
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
                    w.Write(_dataRaw[x, y, z].RawByte0);
                    w.Write(_dataRaw[x, y, z].RawByte1);
                    w.Write(_dataRaw[x, y, z].RawByte2);
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
                    byte b0 = r.ReadByte();
                    byte b1 = r.ReadByte();
                    byte b2 = r.ReadByte();
                    chunk._dataRaw[x, y, z] = new Klotz(b0, b1, b2);
                }
            }
        }

        return chunk;
    }
}
