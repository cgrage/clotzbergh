using System;
using System.IO;
using UnityEngine;

/// <summary>
/// The current chunk size is 32x80x32
/// 
/// Factorization:
/// fact 32: 1, 2, 4, -, 8, --, 16, --, 32, --, --
/// fact 80: 1, 2, 4, 5, 8, 10, 16, 20, --, 40, 80
/// common:  1, 2, 4, -, 8, --, 16, --, --, --, --
/// 
/// 32*32*80 = 81.920 voxels per chunk
/// 
/// </summary>
public class WorldChunk
{
    public const int KlotzCountX = 32;
    public const int KlotzCountY = 80;
    public const int KlotzCountZ = 32;

    public static readonly Vector3Int KlotzCount = new(KlotzCountX, KlotzCountY, KlotzCountZ);

    /// <summary>
    /// 32 * 0.36 = 80 * 0.144 = 11,52
    /// </summary>
    public static readonly Vector3 Size = new(SubKlotz.Size.x * KlotzCountX, SubKlotz.Size.y * KlotzCountY, SubKlotz.Size.z * KlotzCountZ);

    private readonly SubKlotz[,,] _klotzData;

    private WorldChunk()
    {
        _klotzData = new SubKlotz[KlotzCountX, KlotzCountY, KlotzCountZ];
    }

    private void FloodFill(int toHeight = KlotzCountY)
    {
        for (int z = 0; z < KlotzCountZ; z++)
        {
            for (int y = 0; y < toHeight; y++)
            {
                for (int x = 0; x < KlotzCountX; x++)
                {
                    _klotzData[x, y, z] = new SubKlotz(
                        KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
                }
            }
        }
    }

    private void CoreFill()
    {
        for (int z = 0; z < KlotzCountZ; z++)
        {
            for (int y = 0; y < KlotzCountY; y++)
            {
                for (int x = 0; x < KlotzCountX; x++)
                {
                    bool inCore =
                         x > KlotzCountX / 4 && x < 3 * KlotzCountX / 4 &&
                         y > KlotzCountY / 4 && y < 3 * KlotzCountY / 4 &&
                         z > KlotzCountZ / 4 && z < 3 * KlotzCountZ / 4;

                    if (inCore) _klotzData[x, y, z] = new SubKlotz(
                        KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
                }
            }
        }
    }

    public SubKlotz Get(int x, int y, int z) { return _klotzData[x, y, z]; }

    public void Set(int x, int y, int z, SubKlotz t) { _klotzData[x, y, z] = t; }

    public static WorldChunk CreateEmpty()
    {
        return new WorldChunk();
    }

    public static WorldChunk CreateFloodFilled(int toHeight = KlotzCountY)
    {
        WorldChunk chunk = new();
        chunk.FloodFill(toHeight);
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
        for (int z = 0; z < KlotzCountZ; z++)
        {
            for (int y = 0; y < KlotzCountY; y++)
            {
                for (int x = 0; x < KlotzCountX; x++)
                {
                    _klotzData[x, y, z].Serialize(w);
                }
            }
        }
    }

    public static WorldChunk Deserialize(BinaryReader r)
    {
        WorldChunk chunk = new();
        for (int z = 0; z < KlotzCountZ; z++)
        {
            for (int y = 0; y < KlotzCountY; y++)
            {
                for (int x = 0; x < KlotzCountX; x++)
                {
                    chunk._klotzData[x, y, z] = SubKlotz.Deserialize(r);
                }
            }
        }

        return chunk;
    }
}
