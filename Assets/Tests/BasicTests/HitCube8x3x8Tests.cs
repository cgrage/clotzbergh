using System;
using Clotzbergh;
using Clotzbergh.Server.ChunkGeneration;
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

        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(6, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(5, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(4, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(3, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(2, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(1, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(-1, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(-2, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(-3, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(-4, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(-5, 0, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(-6, 0, 0))));

        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 6, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 5, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 4, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 3, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 2, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 1, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, -1, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, -2, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, -3, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, -4, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, -5, 0))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, -6, 0))));

        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 6))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 5))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 4))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 3))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 2))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 1))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, 0))));
        Assert.IsTrue(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, -1))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, -2))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, -3))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, -4))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, -5))));
        Assert.IsFalse(hc1.Hits(HitCube8x3x8.Draw(KlotzType.Brick2x4, new RelKlotzCoords(0, 0, -6))));
    }

    [Test]
    public void HitCubeTest2()
    {
        // Klotz 1: Plate8x8 at (7, 53, 23)
        RelKlotzCoords k1Pos = new(7, 53, 23);
        KlotzType k1Type = KlotzType.Plate8x8;

        // Klotz 2: Brick1x8 at (0, 51, 27)
        RelKlotzCoords k2Pos = new(0, 51, 27);
        KlotzType k2Type = KlotzType.Brick1x8;

        // Point of interest: Root of Klotz 2 -> (-7, -2, 4)
        RelKlotzCoords relPos = new(k2Pos.ToVector() - k1Pos.ToVector());

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

    public static bool DoIntersectReferenceImpl(RelKlotzCoords posA, KlotzType typeA, KlotzDirection dirA, RelKlotzCoords posB, KlotzType typeB, KlotzDirection dirB)
    {
        // Limitations
        if (dirA != KlotzDirection.ToPosX || dirB != KlotzDirection.ToPosX)
        {
            throw new NotImplementedException();
        }

        KlotzSize sizeA = KlotzKB.Size(typeA);
        KlotzSize sizeB = KlotzKB.Size(typeB);

        return
            posA.X < posB.X + sizeB.X && posA.X + sizeA.X > posB.X &&
            posA.Y < posB.Y + sizeB.Y && posA.Y + sizeA.Y > posB.Y &&
            posA.Z < posB.Z + sizeB.Z && posA.Z + sizeA.Z > posB.Z;
    }
}
