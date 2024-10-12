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

    public static bool IsSubKlotzClear(KlotzType t, int subIdxX, int subIdxY, int subIdxZ)
    {
        return t switch
        {
            KlotzType.Plate1x1 => false,
            KlotzType.Brick4x2 => false,
            _ => true,
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
/// KlotzType             ->  5 bit
/// Orientation (N/E/S/W) ->  2 bit
/// SubKlotzIndexX (0..7) ->  3 bit
/// SubKlotzIndexY (0..7) ->  3 bit
/// SubKlotzIndexZ (0..7) ->  3 bit
/// --------------------------------
/// Sum                       16 bit
/// 
/// </summary>
public readonly struct SubKlotz
{
    public readonly KlotzType Type
    {
        // bits: xxxxx00000000000
        get { return (KlotzType)(raw16bit >> 11); }
    }

    public readonly KlotzDirection Direction
    {
        // bits: 00000xx000000000
        get { return (KlotzDirection)((raw16bit >> 9) & 0x3); }
    }

    public readonly int SubKlotzIndexX
    {
        // bits: 0000000xxx000000
        get { return (raw16bit >> 6) & 0x7; }
    }

    public readonly int SubKlotzIndexY
    {
        // bits: 0000000000xxx000
        get { return (raw16bit >> 3) & 0x7; }
    }

    public readonly int SubKlotzIndexZ
    {
        // bits: 0000000000000xxx
        get { return (raw16bit >> 0) & 0x7; }
    }

    private readonly ushort raw16bit;

    public readonly bool IsClear
    {
        get { return KlotzKB.IsSubKlotzClear(Type, SubKlotzIndexX, SubKlotzIndexY, SubKlotzIndexZ); }
    }

    public readonly bool IsRootSubKlotz
    {
        get { return (raw16bit & 0x1ff) == 0; }
    }

    /// <summary>
    /// Calculates the position of the RootSubKlotz based on the position of this SubKlotz.
    /// </summary>
    public readonly Vector3Int RootPos(int x, int y, int z)
    {
        return new(x - SubKlotzIndexX, y - SubKlotzIndexY, z - SubKlotzIndexZ);
    }

    public SubKlotz(KlotzType type, KlotzDirection dir, int subIdxX, int subIdxY, int subIdxZ)
    {
        raw16bit = (ushort)(
            ((int)type & 0x1f) << 11 |
            ((int)dir & 0x3) << 9 |
            (subIdxX & 0x7) << 6 |
            (subIdxY & 0x7) << 3 |
            (subIdxZ & 0x7) << 0);
    }

    public SubKlotz(ushort u16)
    {
        raw16bit = u16;
    }

    public static SubKlotz Deserialize(BinaryReader r)
    {
        return new SubKlotz(r.ReadUInt16());
    }

    public readonly void Serialize(BinaryWriter w)
    {
        w.Write(raw16bit);
    }
}
