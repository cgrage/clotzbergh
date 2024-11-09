using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class KlotzTests
{
    [Test]
    public void SubKlotzCreation()
    {
        SubKlotz klotz;

        klotz = new(KlotzType.Air, 0, KlotzVariant.Zero, KlotzDirection.ToPosX);
        Assert.AreEqual(true, klotz.IsRoot);
        Assert.AreEqual(KlotzType.Air, klotz.Type);

        klotz = new(KlotzType.Plate1x1, 0, KlotzVariant.Zero, KlotzDirection.ToPosX);
        Assert.AreEqual(true, klotz.IsRoot);
        Assert.AreEqual(KlotzType.Plate1x1, klotz.Type);

        klotz = new(KlotzType.Brick2x4, KlotzDirection.ToNegZ, 1, 2, 3);
        Assert.AreEqual(false, klotz.IsRoot);
        Assert.AreEqual(KlotzDirection.ToNegZ, klotz.Direction);
        Assert.AreEqual(1, klotz.SubKlotzIndexX);
        Assert.AreEqual(2, klotz.SubKlotzIndexY);
        Assert.AreEqual(3, klotz.SubKlotzIndexZ);
    }

    [Test]
    public void SubKlotzBasics()
    {
        SubKlotz subKlotz;

        subKlotz = new(KlotzType.Air, 0, KlotzVariant.Zero, KlotzDirection.ToPosX);
        Assert.AreEqual(true, subKlotz.IsRoot);
        Assert.AreEqual(KlotzType.Air, subKlotz.Type);
        Assert.AreEqual(false, subKlotz.IsOpaque);

        subKlotz = new(KlotzType.Plate1x1, KlotzColor.Black, (KlotzVariant)111, KlotzDirection.ToPosX);
        Assert.AreEqual(true, subKlotz.IsRoot);
        Assert.AreEqual(KlotzType.Plate1x1, subKlotz.Type);
        Assert.AreEqual(KlotzColor.Black, subKlotz.Color);
        Assert.AreEqual((KlotzVariant)111, subKlotz.Variant);
        Assert.AreEqual(true, subKlotz.IsOpaque);

        subKlotz = new(KlotzType.Brick2x4, KlotzColor.Red, (KlotzVariant)127, KlotzDirection.ToPosX);
        Assert.AreEqual(true, subKlotz.IsRoot);
        Assert.AreEqual(KlotzType.Brick2x4, subKlotz.Type);
        Assert.AreEqual(KlotzColor.Red, subKlotz.Color);
        Assert.AreEqual((KlotzVariant)127, subKlotz.Variant);
        Assert.AreEqual(true, subKlotz.IsOpaque);
    }

    [Test]
    public void SubKlotzSerializationRoot()
    {
        var type = KlotzType.Brick2x4;
        var color = KlotzColor.Yellow;
        var variant = (KlotzVariant)103;
        var direction = KlotzDirection.ToPosZ;

        SubKlotz orig = new(type, color, variant, direction);
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
        Assert.AreEqual(variant, copy.Variant);
        Assert.AreEqual(direction, copy.Direction);
    }

    [Test]
    public void SubKlotzSerializationNonRoot()
    {
        var type = KlotzType.Brick2x4;
        var direction = KlotzDirection.ToPosZ;
        var indexX = 3;
        var indexY = 4;
        var indexZ = 5;

        SubKlotz orig = new(type, direction, indexX, indexY, indexZ);
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

        Assert.AreEqual(direction, copy.Direction);
        Assert.AreEqual(true, copy.IsOpaque);
        Assert.AreEqual(indexX, copy.SubKlotzIndexX);
        Assert.AreEqual(indexY, copy.SubKlotzIndexY);
        Assert.AreEqual(indexZ, copy.SubKlotzIndexZ);
    }

    [Test]
    public void KlotzCreation()
    {
        Klotz klotz;

        klotz = new(10, 20, 30, KlotzType.Brick1x6, KlotzColor.Black, (KlotzVariant)11, KlotzDirection.ToPosZ);
        Assert.AreEqual(10, klotz.CoordsX);
        Assert.AreEqual(20, klotz.CoordsY);
        Assert.AreEqual(30, klotz.CoordsZ);
        Assert.AreEqual(KlotzType.Brick1x6, klotz.Type);
        Assert.AreEqual(KlotzColor.Black, klotz.Color);
        Assert.AreEqual((KlotzVariant)11, klotz.Variant);
        Assert.AreEqual(KlotzDirection.ToPosZ, klotz.Direction);
    }

    [Test]
    public void KlotzSerialization()
    {
        var coordsX = 3;
        var coordsY = 4;
        var coordsZ = 5;
        var type = KlotzType.Brick2x4;
        var color = KlotzColor.Yellow;
        var variant = (KlotzVariant)127;
        var direction = KlotzDirection.ToPosZ;

        Klotz orig = new(coordsX, coordsY, coordsZ, type, color, variant, direction);
        Klotz copy;
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
                copy = Klotz.Deserialize(reader);
                Assert.AreEqual(memoryStream.Position, memoryStream.Length, "Stream at end");
            }
        }

        Assert.AreEqual(coordsX, copy.CoordsX);
        Assert.AreEqual(coordsY, copy.CoordsY);
        Assert.AreEqual(coordsZ, copy.CoordsZ);
        Assert.AreEqual(type, copy.Type);
        Assert.AreEqual(color, copy.Color);
        Assert.AreEqual(variant, copy.Variant);
        Assert.AreEqual(direction, copy.Direction);
    }
}
