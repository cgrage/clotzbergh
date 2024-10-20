using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class KlotzTests
{
    [Test]
    public void KlotzCreation()
    {
        SubKlotz klotz;

        klotz = new(KlotzType.Air, 0, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.AreEqual(KlotzType.Air, klotz.Type);

        klotz = new(KlotzType.Plate1x1, 0, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.AreEqual(KlotzType.Plate1x1, klotz.Type);

        klotz = new(KlotzType.Brick4x2, 0, KlotzDirection.ToNegZ, 1, 2, 3);
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

        subKlotz = new(KlotzType.Air, 0, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.IsFalse(subKlotz.IsOpaque);

        subKlotz = new(KlotzType.Plate1x1, 0, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.IsTrue(subKlotz.IsOpaque);

        subKlotz = new(KlotzType.Brick4x2, 0, KlotzDirection.ToPosX, 0, 0, 0);
        Assert.IsTrue(subKlotz.IsOpaque);
    }

    [Test]
    public void SubKlotzSerialization()
    {
        var type = KlotzType.Brick4x2;
        var color = KlotzColor.Yellow;
        var direction = KlotzDirection.ToPosZ;
        var indexX = 3;
        var indexY = 4;
        var indexZ = 5;

        SubKlotz orig = new(type, color, direction, indexX, indexY, indexZ);
        SubKlotz copy;
        byte[] bytes;

        using (MemoryStream memoryStream = new())
        {
            using (BinaryWriter writer = new(memoryStream))
            {
                orig.Serialize(writer);
            }

            bytes = memoryStream.ToArray();
        }

        Debug.Log($"Bytes: {string.Join(" ", bytes.Select(b => $"0x{b:X2} "))}");

        using (MemoryStream memoryStream = new(bytes))
        {
            using (BinaryReader reader = new(memoryStream))
            {
                copy = SubKlotz.Deserialize(reader);
            }
        }

        Assert.AreEqual(type, copy.Type);
        Assert.AreEqual(color, copy.Color);
        Assert.AreEqual(direction, copy.Direction);
        Assert.AreEqual(indexX, copy.SubKlotzIndexX);
        Assert.AreEqual(indexY, copy.SubKlotzIndexY);
        Assert.AreEqual(indexZ, copy.SubKlotzIndexZ);
    }
}
