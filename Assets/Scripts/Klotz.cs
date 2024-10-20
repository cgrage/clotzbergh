using System;
using System.IO;
using UnityEngine;

public enum KlotzType
{
    Air = 0,
    Plate1x1,
    Brick4x2,
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
            KlotzType.Brick4x2 => new(4, 3, 2),
            _ => Vector3Int.zero,
        };
    }

    public static bool IsSubKlotzClear(KlotzType t, int subIdxX, int subIdxY, int subIdxZ)
    {
        return t switch
        {
            KlotzType.Plate1x1 => false,
            KlotzType.Brick4x2 => false,
            _ => true,
        };
    }

    public static bool IsSubKlotzOpaque(KlotzType t, int subIdxX, int subIdxY, int subIdxZ)
    {
        return !IsSubKlotzClear(t, subIdxX, subIdxY, subIdxZ);
    }
}

public enum SubKlotzKind
{
    Root = 0,
    NonRoot = 1,
}

/// <summary>
/// The base voxel for the terrain.
/// 
/// Root Block:
/// SubKlotzKind
/// Kind (=Root)           ->  1 bit
/// Type (0..255)          ->  8 bit
/// Color (0..31)          ->  5 bit
/// Orientation (N/E/S/W)  ->  2 bit
/// --------------------------------
/// Sum                       16 bit
/// 
/// Non-Root Block:
/// Kind (=NonRoot)        ->  1 bit
/// IsOpaque (0=false)     ->  1 bit
/// Reserved               ->  2 bit
/// SubKlotzIndexX (0..15) ->  4 bit
/// SubKlotzIndexY (0..15) ->  4 bit
/// SubKlotzIndexZ (0..15) ->  4 bit
/// --------------------------------
/// Sum                       16 bit
/// 
/// </summary>
public readonly struct SubKlotz
{
    private readonly uint rawBits;

    public SubKlotz(uint raw)
    {
        rawBits = raw;
    }

    public SubKlotz(KlotzType type, KlotzColor color, KlotzDirection dir)
    {
        rawBits = (uint)(
            ((int)SubKlotzKind.Root & 0x1) << 15 |
            ((int)type & 0xff) << 7 |
            ((int)color & 0x1f) << 2 |
            ((int)dir & 0x3) << 0);
    }

    public SubKlotz(bool isOpaque, int subIdxX, int subIdxY, int subIdxZ)
    {
        rawBits = (uint)(
            ((int)SubKlotzKind.NonRoot & 0x1) << 15 |
            (isOpaque ? 1 : 0) << 14 |
            (subIdxX & 0xf) << 8 |
            (subIdxY & 0xf) << 4 |
            (subIdxZ & 0xf) << 0);
    }

    public SubKlotz(KlotzType type, int subIdxX, int subIdxY, int subIdxZ)
        : this(KlotzKB.IsSubKlotzOpaque(type, subIdxX, subIdxY, subIdxZ), subIdxX, subIdxY, subIdxZ) { }

    public readonly bool IsRootSubKlotz
    {
        get { return (rawBits & 0x8000) == 0; }
    }

    public readonly KlotzType Type
    {
        // bits: 0xxxxxxxx0000000
        get
        {
            if (!IsRootSubKlotz) throw new InvalidOperationException("Type of NonRoot-Block requested");
            return (KlotzType)((rawBits >> 7) & 0xff);
        }
    }

    public readonly KlotzColor Color
    {
        // bits: 000000000xxxxx00
        get
        {
            if (!IsRootSubKlotz) throw new InvalidOperationException("Color of NonRoot-Block requested");
            return (KlotzColor)((rawBits >> 2) & 0x1f);
        }
    }

    public readonly KlotzDirection Direction
    {
        // bits: 00000000000000xx
        get
        {
            if (!IsRootSubKlotz) throw new InvalidOperationException("Direction of NonRoot-Block requested");
            return (KlotzDirection)((rawBits >> 0) & 0x3);
        }
    }

    public readonly int SubKlotzIndexX
    {
        // bits: 0000xxxx00000000
        get
        {
            if (IsRootSubKlotz) throw new InvalidOperationException("SubKlotzIndexX of Root-Block requested");
            return (int)((rawBits >> 8) & 0xf);
        }
    }

    public readonly int SubKlotzIndexY
    {
        // bits: 00000000xxxx0000
        get
        {
            if (IsRootSubKlotz) throw new InvalidOperationException("SubKlotzIndexY of Root-Block requested");
            return (int)((rawBits >> 4) & 0xf);
        }
    }

    public readonly int SubKlotzIndexZ
    {
        // bits: 000000000000xxxx
        get
        {
            if (IsRootSubKlotz) throw new InvalidOperationException("SubKlotzIndexZ of Root-Block requested");
            return (int)((rawBits >> 0) & 0xf);
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

    public readonly bool IsClear
    {
        get { return !IsOpaque; }
    }

    public readonly bool IsOpaque
    {

        get
        {
            if (IsRootSubKlotz) return KlotzKB.IsSubKlotzOpaque(Type, 0, 0, 0);
            else return (rawBits & 0x4000) != 0;
        }
    }

    /// <summary>
    /// Calculates the position of the RootSubKlotz based on the position of this SubKlotz.
    /// </summary>
    public readonly Vector3Int CalcRootCoords(Vector3Int myPos)
    {
        if (IsRootSubKlotz)
            return myPos;

        return myPos - SubKlotzIndex;
    }

    public static SubKlotz Deserialize(BinaryReader r)
    {
        return new SubKlotz(r.ReadUInt16());
    }

    public readonly void Serialize(BinaryWriter w)
    {
        w.Write((ushort)rawBits);
    }

    public static Vector3Int TranslateSubIndexToRealCoord(Vector3Int rootCoords, Vector3Int subIndex, KlotzDirection dir)
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
