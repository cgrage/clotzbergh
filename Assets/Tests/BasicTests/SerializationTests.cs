using System.IO;
using NUnit.Framework;
using UnityEngine;

public class SerializationTests
{
    [Test]
    public void SerializeWorldChunk()
    {
        SerializeWorldChunk(0, 100);
        SerializeWorldChunk(5, 95);
        SerializeWorldChunk(10, 90);
        SerializeWorldChunk(25, 75);
        SerializeWorldChunk(50, 50);
    }

    public void SerializeWorldChunk(int startPercent, int endPercent)
    {
        WorldChunk orig = new();
        orig.CoreFill(startPercent, endPercent);
        orig.PlaceKlotz(KlotzType.Plate2x8, KlotzColor.Red, (KlotzVariant)127, new Vector3Int(10, 10, 10), KlotzDirection.ToNegZ);
        orig.PlaceKlotz(KlotzType.Plate2x8, KlotzColor.Red, (KlotzVariant)14, new Vector3Int(20, 20, 20), KlotzDirection.ToNegX);

        WorldChunk copy = SerializeDeserialize(orig);

        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    SubKlotz kOrig = orig.Get(x, y, z);
                    SubKlotz kCopy = copy.Get(x, y, z);

                    Assert.AreEqual(kOrig.IsRoot, kCopy.IsRoot, $"copy differs at ({x}, {y}, {z}) | {kOrig} != {kCopy}");

                    if (kOrig.IsRoot)
                    {
                        Assert.AreEqual(kOrig.Type, kCopy.Type, $"copy differs at ({x}, {y}, {z})");
                        Assert.AreEqual(kOrig.Color, kCopy.Color, $"copy differs at ({x}, {y}, {z})");
                        Assert.AreEqual(kOrig.Variant, kCopy.Variant, $"copy differs at ({x}, {y}, {z})");
                        Assert.AreEqual(kOrig.Direction, kCopy.Direction, $"copy differs at ({x}, {y}, {z})");
                    }
                    else
                    {
                        Assert.AreEqual(kOrig.Direction, kCopy.Direction, $"copy differs at ({x}, {y}, {z})");
                        Assert.AreEqual(kOrig.IsOpaque, kCopy.IsOpaque, $"copy differs at ({x}, {y}, {z})");
                        Assert.AreEqual(kOrig.SubKlotzIndexX, kCopy.SubKlotzIndexX, $"copy differs at ({x}, {y}, {z})");
                        Assert.AreEqual(kOrig.SubKlotzIndexY, kCopy.SubKlotzIndexY, $"copy differs at ({x}, {y}, {z})");
                        Assert.AreEqual(kOrig.SubKlotzIndexZ, kCopy.SubKlotzIndexZ, $"copy differs at ({x}, {y}, {z})");
                    }
                }
            }
        }
    }

    public WorldChunk SerializeDeserialize(WorldChunk orig)
    {
        byte[] data;

        using MemoryStream ws = new();
        {
            using (BinaryWriter w = new(ws))
            {
                orig.Serialize(w);
            }

            data = ws.ToArray();
        }

        using MemoryStream rs = new(data);
        {
            using BinaryReader r = new(rs);
            {
                uint bits = r.ReadUInt32();
                bool asList = (bits & (1 << 31)) != 0;
                int klotzCount = (int)(bits & ~(1 << 31));
                Debug.Log($"Introspect: klotzCount={klotzCount}, asList={asList}, size={data.Length}");

                rs.Position = 0; // reset
                return WorldChunk.Deserialize(r);
            }
        }
    }
}
