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
    public const int SubDivsX = 32;
    public const int SubDivsY = 80;
    public const int SubDivsZ = 32;

    public static readonly Vector3Int SubDivs = new(SubDivsX, SubDivsY, SubDivsZ);

    /// <summary>
    /// 32 * 0.36 = 80 * 0.144 = 11,52
    /// </summary>
    public static readonly Vector3 Size = new(SubKlotz.Size.x * SubDivsX, SubKlotz.Size.y * SubDivsY, SubKlotz.Size.z * SubDivsZ);

    private readonly SubKlotz[,,] _klotzData;

    private WorldChunk()
    {
        _klotzData = new SubKlotz[SubDivsX, SubDivsY, SubDivsZ];
    }

    private void FloodFill(int toHeight = SubDivsY)
    {
        for (int z = 0; z < SubDivsZ; z++)
        {
            for (int y = 0; y < toHeight; y++)
            {
                for (int x = 0; x < SubDivsX; x++)
                {
                    _klotzData[x, y, z] = new SubKlotz(
                        KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
                }
            }
        }
    }

    private void CoreFill()
    {
        for (int z = 0; z < SubDivsZ; z++)
        {
            for (int y = 0; y < SubDivsY; y++)
            {
                for (int x = 0; x < SubDivsX; x++)
                {
                    bool inCore =
                         x > SubDivsX / 4 && x < 3 * SubDivsX / 4 &&
                         y > SubDivsY / 4 && y < 3 * SubDivsY / 4 &&
                         z > SubDivsZ / 4 && z < 3 * SubDivsZ / 4;

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

    public static WorldChunk CreateFloodFilled(int toHeight = SubDivsY)
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
        for (int z = 0; z < SubDivsZ; z++)
        {
            for (int y = 0; y < SubDivsY; y++)
            {
                for (int x = 0; x < SubDivsX; x++)
                {
                    _klotzData[x, y, z].Serialize(w);
                }
            }
        }
    }

    public static WorldChunk Deserialize(BinaryReader r)
    {
        WorldChunk chunk = new();
        for (int z = 0; z < SubDivsZ; z++)
        {
            for (int y = 0; y < SubDivsY; y++)
            {
                for (int x = 0; x < SubDivsX; x++)
                {
                    chunk._klotzData[x, y, z] = SubKlotz.Deserialize(r);
                }
            }
        }

        return chunk;
    }

    public static Vector3Int PositionToChunkCoords(Vector3 position)
    {
        return new(
            Mathf.FloorToInt(position.x / Size.x),
            Mathf.FloorToInt(position.y / Size.y),
            Mathf.FloorToInt(position.z / Size.z));
    }

    public static Vector3 ChunkCoordsToPosition(Vector3Int coords)
    {
        return Vector3.Scale(coords, Size);
    }

    public static float DistanceToChunkCenter(Vector3 position, Vector3Int chunkCoords)
    {
        Vector3 chunkPosition = ChunkCoordsToPosition(chunkCoords);
        Vector3 chunkCenter = chunkPosition + Size / 2;
        return Vector3.Distance(position, chunkCenter);
    }

    public static int ChunkDistance(Vector3 position, Vector3Int chunkCoords)
    {
        Vector3Int posCoords = PositionToChunkCoords(position);
        return (int)Vector3Int.Distance(posCoords, chunkCoords);
    }
}
