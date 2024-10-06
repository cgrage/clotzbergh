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
            KlotzType.Brick4x2 => new(4, 6, 2),
            _ => Vector3Int.zero,
        };
    }

    /// <summary>
    /// Return the size of the Klotz when placed in direction <c>d</c>.
    /// </summary>
    public static Vector3Int KlotzSize(KlotzType t, KlotzDirection d)
    {
        Vector3Int result = KlotzSize(t);
        if (d == KlotzDirection.ToPosZ || d == KlotzDirection.ToNegZ)
            (result.z, result.x) = (result.x, result.z);
        return result;
    }

    public static bool IsSubKlotzOpaque(KlotzType t, int subIdxX, int subIdxY, int subIdxZ)
    {
        return t switch
        {
            KlotzType.Plate1x1 => true,
            KlotzType.Brick4x2 => true,
            _ => false,
        };
    }
}

/// <summary>
/// 
/// [Sizes and scale in real world]
/// Reality:
/// - P = 8mm, h = 3.2mm
/// - Figure height: 40mm
/// - Scale: 1:45 ==> 40mm -> 1.8m
/// 
/// [Data]
/// KlotzType              -> 10 bit
/// Orientation (N/E/S/W)  ->  2 bit
/// SubKlotzIndexX (0..15) ->  4 bit
/// SubKlotzIndexY (0..15) ->  4 bit
/// SubKlotzIndexZ (0..15) ->  4 bit
/// --------------------------------
/// Sum                       24 bit
/// 
/// </summary>
public struct SubKlotz
{
    private const float P = 0.008f;
    private const float h = 0.0032f;
    private const float ScaleInv = 45;

    /// <summary>
    /// This is calculated from constants.
    /// 
    /// X/Z: P * ScaleInv = 0.0080 * 45 = 0.360
    /// Y:   h * ScaleInv = 0.0032 * 45 = 0.144
    /// 
    /// So the result is { 0.36, 0.144, 0.36 }
    /// </summary>
    public static readonly Vector3 Size = new(P * ScaleInv, h * ScaleInv, P * ScaleInv);

    public readonly KlotzType Type
    {
        get { return (KlotzType)(raw24bit >> 14); }
    }

    public readonly KlotzDirection Direction
    {
        get { return (KlotzDirection)((raw24bit >> 12) & 0x3); }
    }

    public readonly int SubKlotzIndexX
    {
        get { return (int)((raw24bit >> 8) & 0xf); }
    }

    public readonly int SubKlotzIndexY
    {
        get { return (int)((raw24bit >> 4) & 0xf); }
    }

    public readonly int SubKlotzIndexZ
    {
        get { return (int)((raw24bit >> 0) & 0xf); }
    }

    private readonly uint raw24bit;

    public readonly bool IsOpaque
    {
        get { return KlotzKB.IsSubKlotzOpaque(Type, SubKlotzIndexX, SubKlotzIndexY, SubKlotzIndexZ); }
    }

    public SubKlotz(KlotzType type, KlotzDirection dir, int subIdxX, int subIdxY, int subIdxZ)
    {
        raw24bit =
            ((uint)type & 0x3ff) << 14 |
            ((uint)dir & 0x3) << 12 |
            ((uint)subIdxX & 0xf) << 8 |
            ((uint)subIdxY & 0xf) << 4 |
            ((uint)subIdxZ & 0xf) << 0;
    }

    public SubKlotz(byte b0, byte b1, byte b2)
    {
        raw24bit = (uint)b0 << 16 | (uint)b1 << 8 | b2;
    }

    public static SubKlotz Deserialize(BinaryReader r)
    {
        return new SubKlotz(r.ReadByte(), r.ReadByte(), r.ReadByte());
    }

    public readonly void Serialize(BinaryWriter w)
    {
        w.Write((byte)(raw24bit >> 16));
        w.Write((byte)(raw24bit >> 8));
        w.Write((byte)(raw24bit >> 0));
    }
}
