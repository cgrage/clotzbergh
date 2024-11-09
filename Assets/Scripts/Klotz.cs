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

public readonly struct KlotzVariant
{
    public const int MaxValue = 127;

    private readonly byte _value;

    public static readonly KlotzVariant Zero = new();

    public KlotzVariant(byte value) { this._value = value; }

    // Explicit cast to int
    public static explicit operator uint(KlotzVariant variant) { return variant._value; }

    // Explicit cast from uint
    public static explicit operator KlotzVariant(uint value) { return new KlotzVariant((byte)value); }
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
/// 
/// </summary>
public enum SubKlotzKind
{
    Root = 0,
    NonRoot = 1,
}

/// <summary>
/// The base voxel for the terrain.
/// 
/// Root Block:
/// Orientation (N/E/S/W)  ->  2 bit
/// Variant (0..127)       ->  7 bit
/// Color (0..63)          ->  6 bit
/// Type (0..255)          ->  8 bit
/// Kind (=Root)           ->  1 bit
/// --------------------------------
/// Sum                       24 bit
/// 
/// Non-Root Block:
/// SubKlotzIndexZ (0..15) ->  4 bit
/// SubKlotzIndexY (0..15) ->  4 bit
/// SubKlotzIndexX (0..15) ->  4 bit
/// Orientation (N/E/S/W)  ->  2 bit
/// IsOpaque (0=false)     ->  1 bit
/// Kind (=NonRoot)        ->  1 bit
/// --------------------------------
/// Sum                       16 bit
/// 
/// TODO: Rename to Kloxel?
/// 
/// </summary>
public readonly struct SubKlotz
{
    private readonly uint _rawBits;

    public SubKlotz(uint u32)
    {
        _rawBits = u32;
    }

    public SubKlotz(KlotzType type, KlotzColor color, KlotzVariant variant, KlotzDirection dir)
    {
        _rawBits =
            ((uint)dir & 0x3) << 22 |
            ((uint)variant & 0x7f) << 15 |
            ((uint)color & 0x3f) << 9 |
            ((uint)type & 0xff) << 1 |
            (uint)SubKlotzKind.Root;
    }

    public SubKlotz(KlotzType type, KlotzDirection dir, int subIdxX, int subIdxY, int subIdxZ)
    {
        bool isOpaque = KlotzKB.IsSubKlotzOpaque(type, subIdxX, subIdxY, subIdxZ);

        _rawBits =
            ((uint)subIdxZ & 0xf) << 12 |
            ((uint)subIdxY & 0xf) << 8 |
            ((uint)subIdxX & 0xf) << 4 |
            ((uint)dir & 0x3) << 2 |
            (isOpaque ? 1u : 0u) << 1 |
            ((uint)SubKlotzKind.NonRoot & 0x1) << 0;
    }

    public readonly bool IsRoot
    {
        get { return ((SubKlotzKind)(_rawBits & 0x1)) == SubKlotzKind.Root; }
    }

    public readonly KlotzType Type
    {
        get
        {
#if DO_CHECKS
            if (!IsRoot) throw new InvalidOperationException($"Type of NonRoot-Block requested. raw=0x{_rawBits:x}");
#endif
            return (KlotzType)((_rawBits >> 1) & 0xff);
        }
    }

    public readonly KlotzColor Color
    {
        get
        {
#if DO_CHECKS
            if (!IsRoot) throw new InvalidOperationException($"Color of NonRoot-Block requested. raw=0x{_rawBits:x}");
#endif
            return (KlotzColor)((_rawBits >> 9) & 0x3f);
        }
    }

    public readonly KlotzVariant Variant
    {
        get
        {
#if DO_CHECKS
            if (!IsRoot) throw new InvalidOperationException($"Variant of NonRoot-Block requested. raw=0x{_rawBits:x}");
#endif
            return (KlotzVariant)((_rawBits >> 15) & 0x7f);
        }
    }

    public readonly KlotzDirection Direction
    {
        get
        {
#if DO_CHECKS
            if (IsRoot) return (KlotzDirection)((_rawBits >> 22) & 0x3);
#endif
            return (KlotzDirection)((_rawBits >> 2) & 0x3);
        }
    }

    public readonly int SubKlotzIndexX
    {
        get
        {
            if (IsRoot) return 0;
            return (int)((_rawBits >> 4) & 0x7);
        }
    }

    public readonly int SubKlotzIndexY
    {
        get
        {
            if (IsRoot) return 0;
            return (int)((_rawBits >> 8) & 0x7);
        }
    }

    public readonly int SubKlotzIndexZ
    {
        get
        {
            if (IsRoot) return 0;
            return (int)((_rawBits >> 12) & 0x7);
        }
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
        get
        {
            if (IsRoot) return KlotzKB.IsSubKlotzOpaque(Type, 0, 0, 0);
            else return ((_rawBits >> 1) & 0x1) != 0;
        }
    }

    public readonly bool IsRootAndNotAir
    {
        get { return IsRoot && !IsAir; }
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
        byte byte0 = r.ReadByte();
        byte byte1 = r.ReadByte();
        byte byte2 = ((byte0 & 0x1) == 0) ? r.ReadByte() : (byte)0;

        return new SubKlotz(
            (((uint)byte2) << 16) |
            (((uint)byte1) << 8) |
            (((uint)byte0) << 0));
    }

    public readonly void Serialize(BinaryWriter w)
    {
        w.Write((byte)((_rawBits >> 0) & 0xff));
        w.Write((byte)((_rawBits >> 8) & 0xff));

        if (IsRoot)
        {
            w.Write((byte)((_rawBits >> 16) & 0xff));
        }
    }

    public Klotz ToKlotz(int x, int y, int z)
    {
        return new Klotz(x, y, z, Type, Color, Variant, Direction);
    }

    public override string ToString()
    {
        return $"0x{_rawBits:x}";
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
/// raw bits 1:
/// Coords-X (0..31)       ->  5 bit
/// Coords-Y (0..79)       ->  7 bit
/// Coords-Z (0..31)       ->  5 bit
/// Color (0..63)          ->  6 bit
/// Variant (0..127)       ->  7 bit
/// Orientation (N/E/S/W)  ->  2 bit
/// --------------------------------
/// Sum                       32 bit
/// 
/// raw bits 2:
/// Type (0..255)          ->  8 bit
/// --------------------------------
/// Sum                        8 bit
/// 
/// </summary>
public readonly struct Klotz
{
    private readonly uint rawBits1;
    private readonly byte rawBits2;

    public Klotz(uint u32, byte u8)
    {
        rawBits1 = u32;
        rawBits2 = u8;
    }

    public Klotz(int x, int y, int z, KlotzType type, KlotzColor color, KlotzVariant variant, KlotzDirection dir)
    {
        rawBits1 =
            ((uint)x & 0x1f) << 27 |
            ((uint)y & 0x7f) << 20 |
            ((uint)z & 0x1f) << 15 |
            ((uint)color & 0x3f) << 9 |
            ((uint)variant & 0x7f) << 2 |
            ((uint)dir & 0x3) << 0;

        rawBits2 = (byte)type;
    }

    public readonly KlotzType Type
    {
        get { return (KlotzType)rawBits2; }
    }

    public readonly KlotzColor Color
    {
        get { return (KlotzColor)((rawBits1 >> 9) & 0x3f); }
    }

    public readonly KlotzVariant Variant
    {
        get { return (KlotzVariant)((rawBits1 >> 2) & 0x7f); }
    }

    public readonly KlotzDirection Direction
    {
        get { return (KlotzDirection)((rawBits1 >> 0) & 0x3); }
    }

    public readonly int CoordsX
    {
        get { return (int)((rawBits1 >> 27) & 0x1f); }
    }

    public readonly int CoordsY
    {
        get { return (int)((rawBits1 >> 20) & 0x7f); }
    }

    public readonly int CoordsZ
    {
        get { return (int)((rawBits1 >> 15) & 0x1f); }
    }

    public readonly Vector3Int Coords
    {
        get { return new(CoordsX, CoordsY, CoordsZ); }
    }

    public static Klotz Deserialize(BinaryReader r)
    {
        return new Klotz(r.ReadUInt32(), r.ReadByte());
    }

    public readonly void Serialize(BinaryWriter w)
    {
        w.Write(rawBits1);
        w.Write(rawBits2);
    }
}

public class KlotzWorldData
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
