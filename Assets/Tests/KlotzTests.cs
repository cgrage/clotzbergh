using NUnit.Framework;
using UnityEngine;

public class KlotzTests
{
    [Test]
    public void KlotzCreation()
    {
        SubKlotz klotz;

        klotz = new(KlotzType.Air, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.AreEqual(KlotzType.Air, klotz.Type);

        klotz = new(KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.AreEqual(KlotzType.Plate1x1, klotz.Type);

        klotz = new(KlotzType.Brick4x2, KlotzDirection.ToNegZ, 1, 2, 3);
        Assert.AreEqual(KlotzType.Brick4x2, klotz.Type);
        Assert.AreEqual(KlotzDirection.ToNegZ, klotz.Direction);
        Assert.AreEqual(1, klotz.SubKlotzIndexX);
        Assert.AreEqual(2, klotz.SubKlotzIndexY);
        Assert.AreEqual(3, klotz.SubKlotzIndexZ);
    }

    [Test]
    public void SubKlotzBasics()
    {
        SubKlotz subKlotz;

        subKlotz = new(KlotzType.Air, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.IsTrue(subKlotz.IsClear);

        subKlotz = new(KlotzType.Plate1x1, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.IsFalse(subKlotz.IsClear);

        subKlotz = new(KlotzType.Brick4x2, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.IsFalse(subKlotz.IsClear);
    }

    [Test]
    public void KlotzBasics()
    {
        SubKlotz subKlotz;
        Klotz klotz;

        subKlotz = new(KlotzType.Brick4x2, KlotzDirection.ToPosX, 0, 0, 0);
        klotz = subKlotz.ToKlotz(Vector3Int.zero, Vector3Int.zero);

        Assert.AreEqual(WorldDef.SubKlotzSize.x * 4, klotz.size.x);
        Assert.AreEqual(WorldDef.SubKlotzSize.y * 3, klotz.size.y);
        Assert.AreEqual(WorldDef.SubKlotzSize.z * 2, klotz.size.z);
    }
}
