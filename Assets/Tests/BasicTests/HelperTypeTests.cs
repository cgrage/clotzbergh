using System;
using Clotzbergh;
using Clotzbergh.Server.WorldGeneration;
using NUnit.Framework;
using UnityEngine;

public class HelperTypeTests
{
    [Test]
    public void HitCubeBaseTest()
    {
        HitCube8x3x8 hc1, hc2;

        hc1 = new HitCube8x3x8();
        hc2 = HitCube8x3x8.Empty;

        Assert.IsFalse(hc1.Hits(hc2));
        Assert.IsFalse(hc2.Hits(hc1));

        hc1 = HitCube8x3x8.FromType(KlotzType.Plate1x1);
        hc2 = HitCube8x3x8.FromType(KlotzType.Brick1x1);

        Assert.IsTrue(hc1.Hits(hc2));
        Assert.IsTrue(hc2.Hits(hc1));
    }

    [Test]
    public void HitCubeTest()
    {
        HitCube8x3x8 hc1 = HitCube8x3x8.FromType(KlotzType.Brick2x4);

        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(6, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(5, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(4, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(3, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(2, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(1, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(-1, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(-2, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(-3, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(-4, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(-5, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(-6, 0, 0))));

        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 6, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 5, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 4, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 3, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 2, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 1, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, -1, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, -2, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, -3, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, -4, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, -5, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, -6, 0))));

        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 6))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 5))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 4))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 3))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 2))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 1))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, -1))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, -2))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, -3))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, -4))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, -5))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new Vector3Int(0, 0, -6))));
    }

    [Test]
    public void HitCubeTest2()
    {
        // Klotz 1: Plate8x8 at (7, 53, 23)
        Vector3Int k1Pos = new(7, 53, 23);
        KlotzType k1Type = KlotzType.Plate8x8;

        // Klotz 2: Brick1x8 at (0, 51, 27)
        Vector3Int k2Pos = new(0, 51, 27);
        KlotzType k2Type = KlotzType.Brick1x8;

        // Point of interest: Root of Klotz 2 -> (-7, -2, 4)
        Vector3Int relPos = k2Pos - k1Pos;

        HitCube8x3x8 k2hc = HitCube8x3x8.FromType(k2Type);
        HitCube8x3x8 k1hc = HitCube8x3x8.Draw(k1Type, relPos);

        Debug.Log($"{k2Type} at {k2Pos}:");
        Debug.Log($"{k2hc}");
        Debug.Log($"{k1Type} from {relPos}:");
        Debug.Log($"{k1hc}");

        bool doIntersect = DoIntersectReferenceImpl(
            k1Pos, k1Type, KlotzDirection.ToPosX,
            k2Pos, k2Type, KlotzDirection.ToPosX);

        Assert.AreEqual(doIntersect, k2hc.Hits(k1hc));
    }

    public static bool DoIntersectReferenceImpl(Vector3Int posA, KlotzType typeA, KlotzDirection dirA, Vector3Int posB, KlotzType typeB, KlotzDirection dirB)
    {
        // Limitations
        if (dirA != KlotzDirection.ToPosX || dirB != KlotzDirection.ToPosX)
        {
            throw new NotImplementedException();
        }

        Vector3Int sizeA = KlotzKB.KlotzSize(typeA);
        Vector3Int sizeB = KlotzKB.KlotzSize(typeB);

        return
            posA.x < posB.x + sizeB.x && posA.x + sizeA.x > posB.x &&
            posA.y < posB.y + sizeB.y && posA.y + sizeA.y > posB.y &&
            posA.z < posB.z + sizeB.z && posA.z + sizeA.z > posB.z;
    }
}
