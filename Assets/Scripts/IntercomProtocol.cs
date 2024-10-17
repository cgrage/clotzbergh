using System.IO;
using System.IO.Compression;
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
            using MemoryStream memoryStream = new();
            using (GZipStream gzipStream = new(memoryStream, CompressionMode.Compress))
            using (BinaryWriter writer = new(gzipStream))
            {
                writer.Write((byte)Code);
                Serialize(writer);
            }

            return memoryStream.ToArray();
        }

        protected abstract void Serialize(BinaryWriter w);

        public static Command FromBytes(byte[] data)
        {
            using MemoryStream memoryStream = new(data);
            using GZipStream gzipStream = new(memoryStream, CompressionMode.Decompress);
            using BinaryReader reader = new(gzipStream);

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
        public ulong Version;
        public WorldChunk Chunk;

        public ChunkDataCommand(Vector3Int coord, ulong version, WorldChunk chunk) : base(CodeValue.ChuckData)
        {
            Coord = coord;
            Version = version;
            Chunk = chunk;
        }

        public ChunkDataCommand(BinaryReader r) : base(CodeValue.ChuckData)
        {
            Coord = new Vector3Int(
                r.ReadInt32(),
                r.ReadInt32(),
                r.ReadInt32());
            Version = r.ReadUInt64();
            Chunk = WorldChunk.Deserialize(r);
        }

        protected override void Serialize(BinaryWriter w)
        {
            w.Write(Coord.x);
            w.Write(Coord.y);
            w.Write(Coord.z);
            w.Write(Version);
            Chunk.Serialize(w);
        }
    }
}
