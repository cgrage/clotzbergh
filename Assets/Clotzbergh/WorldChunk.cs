using System.IO;
using UnityEngine;

public class WorldChunk
{
    public const int ChunkWidth = 32;

    public const int ChunkHeight = 80;

    public const int ChunkDepth = 32;

    public const int BorderSize = 1;

    private const int ChunkWidthWithBorder = ChunkWidth + 2 * BorderSize;
    private const int ChunkHeightWithBorder = ChunkHeight + 2 * BorderSize;
    private const int ChunkDepthWithBorder = ChunkDepth + 2 * BorderSize;

    public static readonly Vector3Int ChunkSizeInt = new(ChunkWidth, ChunkHeight, ChunkDepth);
    public static readonly Vector3 ChunkSize = new(ChunkWidth, ChunkHeight, ChunkDepth);
    public static readonly Vector3 Size = new(Klotz.Size.x * ChunkWidth, Klotz.Size.y * ChunkHeight, Klotz.Size.z * ChunkDepth);

    private readonly Klotz[,,] _dataWithBorder;

    private WorldChunk()
    {
        _dataWithBorder = new Klotz[ChunkWidthWithBorder, ChunkHeightWithBorder, ChunkDepthWithBorder];
    }

    private void Fill(KlotzType t, int toHeight = ChunkHeight)
    {
        toHeight += BorderSize;

        for (int z = 0; z < ChunkDepthWithBorder; z++)
        {
            for (int y = 0; y < toHeight; y++)
            {
                for (int x = 0; x < ChunkWidthWithBorder; x++)
                {
                    _dataWithBorder[x, y, z].Type = t;
                }
            }
        }
    }

    public static WorldChunk CreateEmpty()
    {
        return new WorldChunk();
    }

    public static WorldChunk CreateFilled(KlotzType t, int toHeight = ChunkHeight)
    {
        WorldChunk chunk = new();
        chunk.Fill(t, toHeight);
        return chunk;
    }

    public void Serialize(BinaryWriter w)
    {
        for (int z = 0; z < ChunkDepthWithBorder; z++)
        {
            for (int y = 0; y < ChunkHeightWithBorder; y++)
            {
                for (int x = 0; x < ChunkWidthWithBorder; x++)
                {
                    ushort u = (ushort)_dataWithBorder[x, y, z].Type;
                    w.Write((ushort)_dataWithBorder[x, y, z].Type);
                }
            }
        }
    }

    public static WorldChunk Deserialize(BinaryReader r)
    {
        WorldChunk chunk = new();
        for (int z = 0; z < ChunkDepthWithBorder; z++)
        {
            for (int y = 0; y < ChunkHeightWithBorder; y++)
            {
                for (int x = 0; x < ChunkWidthWithBorder; x++)
                {
                    chunk._dataWithBorder[x, y, z].Type = (KlotzType)r.ReadUInt16();
                }
            }
        }

        return chunk;
    }
}
