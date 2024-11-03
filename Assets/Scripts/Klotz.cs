using System;
using System.IO;
using UnityEngine;

public enum KlotzType
{
    Air = 0,
    Plate1x1, Plate1x2, Plate1x3, Plate1x4, Plate1x6, Plate1x8,
    Plate2x2, Plate2x3, Plate2x4, Plate2x6, Plate2x8,
    Plate4x4, Plate4x6, Plate4x8,
    Plate6x6, Plate6x8,
    Plate8x8,
    // CornerPlate1x2x2, CornerPlate2x4x4,
    /*
    Tile1x1, Tile1x2, Tile1x3, Tile1x4, Tile1x6, Tile1x8,
    Tile2x2, Tile2x3, Tile2x4,
    // CornerTile1x2x2,
    */
    Brick1x1, Brick1x2, Brick1x3, Brick1x4, Brick1x6, Brick1x8,
    Brick2x2, Brick2x3, Brick2x4, Brick2x6, Brick2x8,
    Brick4x6,
    // CornerBrick1x2x2,
}

public enum KlotzDirection
{
    ToPosX = 0,
    ToPosZ = 1,
    ToNegX = 2,
    ToNegZ = 3,
}

public enum KlotzColor
{
    White = 0,
    Gray = 1,
    Black = 2,
    Red = 3,
    Blue = 4,
    Yellow = 5,
    Green = 6,
    NextFree = 7,
    Maximum = 31
}

public enum KlotzSide
{
    Left = 0,
    Right = 1,
    Bottom = 2,
    Top = 3,
    Back = 4,
    Front = 5
}

public static class KlotzKB
{
    /// <summary>
    /// Return the size of the Klotz when placed in <c>ToPosX</c> direction.
    /// </summary>
    public static Vector3Int KlotzSize(KlotzType t)
    {
        return t switch
        {
            KlotzType.Plate1x1 => new(1, 1, 1),
            KlotzType.Plate1x2 => new(2, 1, 1),
            KlotzType.Plate1x3 => new(3, 1, 1),
            KlotzType.Plate1x4 => new(4, 1, 1),
            KlotzType.Plate1x6 => new(6, 1, 1),
            KlotzType.Plate1x8 => new(8, 1, 1),
            KlotzType.Plate2x2 => new(2, 1, 2),
            KlotzType.Plate2x3 => new(3, 1, 2),
            KlotzType.Plate2x4 => new(4, 1, 2),
            KlotzType.Plate2x6 => new(6, 1, 2),
            KlotzType.Plate2x8 => new(8, 1, 2),
            KlotzType.Plate4x4 => new(4, 1, 4),
            KlotzType.Plate4x6 => new(6, 1, 4),
            KlotzType.Plate4x8 => new(8, 1, 4),
            KlotzType.Plate6x6 => new(6, 1, 6),
            KlotzType.Plate6x8 => new(8, 1, 6),
            KlotzType.Plate8x8 => new(8, 1, 8),
            KlotzType.Brick1x1 => new(1, 3, 1),
            KlotzType.Brick1x2 => new(2, 3, 1),
            KlotzType.Brick1x3 => new(3, 3, 1),
            KlotzType.Brick1x4 => new(4, 3, 1),
            KlotzType.Brick1x6 => new(6, 3, 1),
            KlotzType.Brick1x8 => new(8, 3, 1),
            KlotzType.Brick2x2 => new(2, 3, 2),
            KlotzType.Brick2x3 => new(3, 3, 2),
            KlotzType.Brick2x4 => new(4, 3, 2),
            KlotzType.Brick2x6 => new(6, 3, 2),
            KlotzType.Brick2x8 => new(8, 3, 2),
            KlotzType.Brick4x6 => new(6, 3, 4),

            KlotzType.Air => Vector3Int.zero,
            _ => throw new Exception($"Unknown size for type {t}")
        };
    }

    public static bool IsSubKlotzOpaque(KlotzType t, int subIdxX, int subIdxY, int subIdxZ)
    {
        return t switch
        {
            KlotzType.Air => false,
            _ => true
        };
    }

    public static readonly KlotzType[] AllGroundTypes = {
        KlotzType.Plate1x1, KlotzType.Plate1x2, KlotzType.Plate1x3, KlotzType.Plate1x4, KlotzType.Plate1x6, KlotzType.Plate1x8,
        KlotzType.Plate2x2, KlotzType.Plate2x3, KlotzType.Plate2x4, KlotzType.Plate2x6, KlotzType.Plate2x8,
        KlotzType.Plate4x4, KlotzType.Plate4x6, KlotzType.Plate4x8,
        KlotzType.Plate6x6, KlotzType.Plate6x8,
        KlotzType.Plate8x8,
        KlotzType.Brick1x1, KlotzType.Brick1x2, KlotzType.Brick1x3, KlotzType.Brick1x4, KlotzType.Brick1x6, KlotzType.Brick1x8,
        KlotzType.Brick2x2, KlotzType.Brick2x3, KlotzType.Brick2x4, KlotzType.Brick2x6, KlotzType.Brick2x8,
        KlotzType.Brick4x6,
    };
}

/// <summary>
/// The base voxel for the terrain.
/// 
/// KlotzType (0..255)    ->  8 bit
/// Color (0..31)         ->  5 bit
/// Orientation (N/E/S/W) ->  2 bit
/// SubKlotzIndexX (0..7) ->  3 bit
/// SubKlotzIndexY (0..7) ->  3 bit
/// SubKlotzIndexZ (0..7) ->  3 bit
/// --------------------------------
/// Sum                       24 bit
/// 
/// TODO: Rename to Kloxel?
/// 
/// </summary>
public readonly struct SubKlotz
{
    private readonly uint rawBits;

    public SubKlotz(uint u32)
    {
        rawBits = u32;
    }

    public SubKlotz(KlotzType type, KlotzColor color, KlotzDirection dir, int subIdxX, int subIdxY, int subIdxZ)
    {
        rawBits = (uint)(
            ((int)type & 0xff) << 16 |
            ((int)color & 0x1f) << 11 |
            ((int)dir & 0x3) << 9 |
            (subIdxX & 0x7) << 6 |
            (subIdxY & 0x7) << 3 |
            (subIdxZ & 0x7) << 0);
    }

    public readonly KlotzType Type
    {
        // bits: xxxxxxxx0000000000000000
        get { return (KlotzType)(rawBits >> 16); }
    }

    public readonly KlotzColor Color
    {
        // bits: 00000000xxxxx00000000000
        get { return (KlotzColor)((rawBits >> 11) & 0x1f); }
    }

    public readonly KlotzDirection Direction
    {
        // bits: 0000000000000xx000000000
        get { return (KlotzDirection)((rawBits >> 9) & 0x3); }
    }

    public readonly int SubKlotzIndexX
    {
        // bits: 000000000000000xxx000000
        get { return (int)((rawBits >> 6) & 0x7); }
    }

    public readonly int SubKlotzIndexY
    {
        // bits: 000000000000000000xxx000
        get { return (int)((rawBits >> 3) & 0x7); }
    }

    public readonly int SubKlotzIndexZ
    {
        // bits: 000000000000000000000xxx
        get { return (int)((rawBits >> 0) & 0x7); }
    }

    public readonly Vector3Int SubKlotzIndex
    {
        get { return new(SubKlotzIndexX, SubKlotzIndexY, SubKlotzIndexZ); }
    }

    public readonly bool IsAir
    {
        get { return Type == KlotzType.Air; }
    }

    public readonly bool IsOpaque
    {
        get { return KlotzKB.IsSubKlotzOpaque(Type, SubKlotzIndexX, SubKlotzIndexY, SubKlotzIndexZ); }
    }

    public readonly bool IsRootSubKlotz
    {
        get { return (rawBits & 0x1ff) == 0; }
    }

    /// <summary>
    /// Calculates the position of the RootSubKlotz based on the position of this SubKlotz.
    /// </summary>
    public readonly Vector3Int RootPos(Vector3Int myPos)
    {
        return TranslateCoordsWithSubIndexToRootCoord(myPos, SubKlotzIndex, Direction);
    }

    public static SubKlotz Deserialize(BinaryReader r)
    {
        return new SubKlotz(
            (((uint)r.ReadByte()) << 16) |
            (((uint)r.ReadByte()) << 8) |
            (((uint)r.ReadByte()) << 0));
    }

    public readonly void Serialize(BinaryWriter w)
    {
        w.Write((byte)((rawBits >> 16) & 0xff));
        w.Write((byte)((rawBits >> 08) & 0xff));
        w.Write((byte)((rawBits >> 00) & 0xff));
    }

    public static Vector3Int TranslateSubIndexToCoords(Vector3Int rootCoords, Vector3Int subIndex, KlotzDirection dir)
    {
        return dir switch
        {
            KlotzDirection.ToPosX => new(rootCoords.x + subIndex.x, rootCoords.y + subIndex.y, rootCoords.z + subIndex.z),
            KlotzDirection.ToNegX => new(rootCoords.x - subIndex.x, rootCoords.y + subIndex.y, rootCoords.z - subIndex.z),
            KlotzDirection.ToPosZ => new(rootCoords.x - subIndex.z, rootCoords.y + subIndex.y, rootCoords.z + subIndex.x),
            KlotzDirection.ToNegZ => new(rootCoords.x + subIndex.z, rootCoords.y + subIndex.y, rootCoords.z - subIndex.x),
            _ => throw new ArgumentException("Invalid direction")
        };
    }

    public static Vector3Int TranslateCoordsWithSubIndexToRootCoord(Vector3Int coord, Vector3Int subIndex, KlotzDirection dir)
    {
        return dir switch
        {
            KlotzDirection.ToPosX => new(coord.x - subIndex.x, coord.y - subIndex.y, coord.z - subIndex.z),
            KlotzDirection.ToNegX => new(coord.x + subIndex.x, coord.y - subIndex.y, coord.z + subIndex.z),
            KlotzDirection.ToPosZ => new(coord.x + subIndex.z, coord.y - subIndex.y, coord.z - subIndex.x),
            KlotzDirection.ToNegZ => new(coord.x - subIndex.z, coord.y - subIndex.y, coord.z + subIndex.x),
            _ => throw new ArgumentException("Invalid direction")
        };
    }

    public static Vector3 TranslateSubKlotzCoordToWorldLocation(Vector3Int rootCoords, KlotzDirection dir)
    {
        return dir switch
        {
            KlotzDirection.ToPosX => Vector3.Scale(rootCoords + new Vector3Int(0, 0, 0), WorldDef.SubKlotzSize),
            KlotzDirection.ToPosZ => Vector3.Scale(rootCoords + new Vector3Int(1, 0, 0), WorldDef.SubKlotzSize),
            KlotzDirection.ToNegX => Vector3.Scale(rootCoords + new Vector3Int(1, 0, 1), WorldDef.SubKlotzSize),
            KlotzDirection.ToNegZ => Vector3.Scale(rootCoords + new Vector3Int(0, 0, 1), WorldDef.SubKlotzSize),
            _ => throw new ArgumentException("Invalid direction")
        };
    }

    public static Quaternion KlotzDirectionToQuaternion(KlotzDirection dir)
    {
        return dir switch
        {
            KlotzDirection.ToPosX => Quaternion.AngleAxis(0, Vector3.up),
            KlotzDirection.ToPosZ => Quaternion.AngleAxis(-90, Vector3.up),
            KlotzDirection.ToNegX => Quaternion.AngleAxis(-180, Vector3.up),
            KlotzDirection.ToNegZ => Quaternion.AngleAxis(-270, Vector3.up),
            _ => throw new ArgumentException("Invalid direction")
        };
    }
}

/// <summary>
/// 
/// </summary>
public class Klotz
{
    /// <summary>
    /// 
    /// </summary>
    public Vector3Int rootCoords;

    /// <summary>
    /// 
    /// </summary>
    public Vector3 worldPosition;

    /// <summary>
    /// 
    /// </summary>
    public Vector3 worldSize;

    /// <summary>
    /// 
    /// </summary>
    public Quaternion worldRotation;

    /// <summary>
    /// 
    /// </summary>
    public KlotzType type;

    /// <summary>
    /// 
    /// </summary>
    public bool isFreeToTake;
}
