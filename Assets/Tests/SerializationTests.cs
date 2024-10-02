using System.IO;
using NUnit.Framework;

public class SerializationTests
{
    [Test]
    public void SerializeWorldChunk()
    {
        WorldChunk orig = WorldChunk.CreateCoreFilled();
        WorldChunk copy;

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
                copy = WorldChunk.Deserialize(r);
            }
        }

        for (int z = 0; z < WorldChunk.KlotzCountRawZ; z++)
        {
            for (int y = 0; y < WorldChunk.KlotzCountRawY; y++)
            {
                for (int x = 0; x < WorldChunk.KlotzCountRawX; x++)
                {
                    Klotz k1 = orig.GetRaw(x, y, z);
                    Klotz k2 = copy.GetRaw(x, y, z);

                    Assert.AreEqual(k1.Type, k2.Type);
                    Assert.AreEqual(k1.Direction, k2.Direction);
                    Assert.AreEqual(k1.SubKlotzIndexX, k2.SubKlotzIndexX);
                    Assert.AreEqual(k1.SubKlotzIndexY, k2.SubKlotzIndexY);
                    Assert.AreEqual(k1.SubKlotzIndexZ, k2.SubKlotzIndexZ);
                }
            }
        }
    }
}