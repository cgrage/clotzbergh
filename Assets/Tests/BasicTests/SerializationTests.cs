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

        WorldChunk copy = SerializeDeserialize(orig);

        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    SubKlotz k1 = orig.Get(x, y, z);
                    SubKlotz k2 = copy.Get(x, y, z);

                    Assert.AreEqual(k1.Type, k2.Type);
                    Assert.AreEqual(k1.Direction, k2.Direction);
                    Assert.AreEqual(k1.SubKlotzIndexX, k2.SubKlotzIndexX);
                    Assert.AreEqual(k1.SubKlotzIndexY, k2.SubKlotzIndexY);
                    Assert.AreEqual(k1.SubKlotzIndexZ, k2.SubKlotzIndexZ);
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
