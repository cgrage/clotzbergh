using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum KlotzType : ushort
{
    Air, Plate1x1
}

public struct Klotz
{
    // [Notes]
    //
    // Reality:
    // - P = 8mm, h = 3.2mm
    // - Minifig height: 40mm
    // - Scale: 1:45 ==> 40mm -> 1.8m

    private const float P = 0.008f;
    private const float h = 0.0032f;
    private const float ScaleInv = 45;

    public static readonly Vector3 Size = new(P * ScaleInv, h * ScaleInv, P * ScaleInv);

    public KlotzType Type { get; set; }

    public Klotz(KlotzType type)
    {
        Type = type;
    }
}
