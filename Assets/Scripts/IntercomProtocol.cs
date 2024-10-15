using System.IO;
using UnityEngine;

public static class IntercomProtocol
{
    public abstract class Command
    {
        public enum CodeValue : byte
        {
            PlayerPosUpdate = 1,
            ChuckData = 2,
        }

        public CodeValue Code { get; private set; }

        protected Command(CodeValue code)
        {
            Code = code;
        }

        public byte[] ToBytes()
        {
            byte[] byteArray;

            using (MemoryStream memoryStream = new())
            {
                using (BinaryWriter writer = new(memoryStream))
                {
                    writer.Write((byte)Code);
                    Serialize(writer);
                }

                byteArray = memoryStream.ToArray();
            }

            return byteArray;
        }

        protected abstract void Serialize(BinaryWriter w);

        public static Command FromBytes(byte[] data)
        {
            using MemoryStream memoryStream = new(data);
            using BinaryReader reader = new(memoryStream);

            CodeValue code = (CodeValue)reader.ReadByte();
            return code switch
            {
                CodeValue.PlayerPosUpdate => new PlayerPosUpdateCommand(reader),
                CodeValue.ChuckData => new ChunkDataCommand(reader),
                _ => throw new IOException("Invalid command"),
            };
        }
    }

    public class PlayerPosUpdateCommand : Command
    {
        public Vector3Int Coord { get; private set; }

        public PlayerPosUpdateCommand(Vector3Int coord) : base(CodeValue.PlayerPosUpdate)
        {
            Coord = coord;
        }

        public PlayerPosUpdateCommand(BinaryReader reader) : base(CodeValue.PlayerPosUpdate)
        {
            Coord = new Vector3Int(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32());
        }

        protected override void Serialize(BinaryWriter w)
        {
            w.Write(Coord.x);
            w.Write(Coord.y);
            w.Write(Coord.z);
        }
    }

    public class ChunkDataCommand : Command
    {
        public Vector3Int Coord;
        public WorldChunk Chunk;

        public ChunkDataCommand(Vector3Int coord, WorldChunk chunk) : base(CodeValue.ChuckData)
        {
            Coord = coord;
            Chunk = chunk;
        }

        public ChunkDataCommand(BinaryReader r) : base(CodeValue.ChuckData)
        {
            Coord = new Vector3Int(
                r.ReadInt32(),
                r.ReadInt32(),
                r.ReadInt32());
            Chunk = WorldChunk.Deserialize(r);
        }

        protected override void Serialize(BinaryWriter w)
        {
            w.Write(Coord.x);
            w.Write(Coord.y);
            w.Write(Coord.z);
            Chunk.Serialize(w);
        }
    }
}
