using System.IO;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class WorldChunk
{
    private readonly SubKlotz[,,] _klotzData;

    private WorldChunk()
    {
        _klotzData = new SubKlotz[
            WorldDef.ChunkSubDivsX,
            WorldDef.ChunkSubDivsY,
            WorldDef.ChunkSubDivsZ];
    }

    private void FloodFill(int toHeight = WorldDef.ChunkSubDivsY)
    {
        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < toHeight; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    _klotzData[x, y, z] = new SubKlotz(
                        KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
                }
            }
        }
    }

    private void CoreFill()
    {
        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    bool inCore =
                         x > WorldDef.ChunkSubDivsX / 4 && x < 3 * WorldDef.ChunkSubDivsX / 4 &&
                         y > WorldDef.ChunkSubDivsY / 4 && y < 3 * WorldDef.ChunkSubDivsY / 4 &&
                         z > WorldDef.ChunkSubDivsZ / 4 && z < 3 * WorldDef.ChunkSubDivsZ / 4;

                    if (inCore) _klotzData[x, y, z] = new SubKlotz(
                        KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
                }
            }
        }
    }

    public SubKlotz Get(int x, int y, int z) { return _klotzData[x, y, z]; }
    public SubKlotz Get(Vector3Int coords) { return _klotzData[coords.x, coords.y, coords.z]; }

    public void Set(int x, int y, int z, SubKlotz t) { _klotzData[x, y, z] = t; }
    public void Set(Vector3Int coords, SubKlotz t) { _klotzData[coords.x, coords.y, coords.z] = t; }

    public static WorldChunk CreateEmpty()
    {
        return new WorldChunk();
    }

    public static WorldChunk CreateFloodFilled(int toHeight = WorldDef.ChunkSubDivsY)
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
        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    _klotzData[x, y, z].Serialize(w);
                }
            }
        }
    }

    public static WorldChunk Deserialize(BinaryReader r)
    {
        WorldChunk chunk = new();
        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
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
            Mathf.FloorToInt(position.x / WorldDef.ChunkSize.x),
            Mathf.FloorToInt(position.y / WorldDef.ChunkSize.y),
            Mathf.FloorToInt(position.z / WorldDef.ChunkSize.z));
    }

    public static Vector3 ChunkCoordsToPosition(Vector3Int coords)
    {
        return Vector3.Scale(coords, WorldDef.ChunkSize);
    }

    public static float DistanceToChunkCenter(Vector3 position, Vector3Int chunkCoords)
    {
        Vector3 chunkPosition = ChunkCoordsToPosition(chunkCoords);
        Vector3 chunkCenter = chunkPosition + WorldDef.ChunkSize / 2;
        return Vector3.Distance(position, chunkCenter);
    }

    public static int ChunkDistance(Vector3Int a, Vector3Int b)
    {
        return (int)Vector3Int.Distance(a, b);
    }
}
