using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Clotzbergh
{
    public static class IntercomProtocol
    {
        public abstract class Command
        {
            public enum CodeValue : byte
            {
                ClientStatus = 1,
                ServerStatus,
                ChuckData,
                TakeKlotz,
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
                    CodeValue.ClientStatus => new ClientStatusCommand(reader),
                    CodeValue.ServerStatus => new ServerStatusCommand(reader),
                    CodeValue.ChuckData => new ChunkDataCommand(reader),
                    CodeValue.TakeKlotz => new TakeKlotzCommand(reader),
                    _ => throw new IOException("Invalid command"),
                };
            }
        }

        public class ServerStatusCommand : Command
        {
            const CodeValue CommandCode = CodeValue.ServerStatus;

            public ServerStatusUpdate Update { get; private set; }

            public ServerStatusCommand(ServerStatusUpdate update) : base(CommandCode)
            {
                Update = update;
            }

            public ServerStatusCommand(BinaryReader r) : base(CommandCode)
            {
                Update = ServerStatusUpdate.Deserialize(r);
            }

            protected override void Serialize(BinaryWriter w)
            {
                Update.Serialize(w);
            }
        }

        public class ClientStatusCommand : Command
        {
            const CodeValue CommandCode = CodeValue.ClientStatus;

            public Vector3 Position { get; private set; }

            public ClientStatusCommand(Vector3 position) : base(CommandCode)
            {
                Position = position;
            }

            public ClientStatusCommand(BinaryReader reader) : base(CommandCode)
            {
                Position = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle());
            }

            protected override void Serialize(BinaryWriter w)
            {
                w.Write(Position.x);
                w.Write(Position.y);
                w.Write(Position.z);
            }
        }

        public class ChunkDataCommand : Command
        {
            const CodeValue CommandCode = CodeValue.ChuckData;

            public Vector3Int Coord;
            public ulong Version;
            public WorldChunk Chunk;

            public ChunkDataCommand(Vector3Int coord, ulong version, WorldChunk chunk) : base(CommandCode)
            {
                Coord = coord;
                Version = version;
                Chunk = chunk;
            }

            public ChunkDataCommand(BinaryReader r) : base(CommandCode)
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

        public class TakeKlotzCommand : Command
        {
            const CodeValue CommandCode = CodeValue.TakeKlotz;

            public Vector3Int ChunkCoord;
            public Vector3Int InnerChunkCoord;

            public TakeKlotzCommand(Vector3Int coord, Vector3Int innerChunkCoord) : base(CommandCode)
            {
                ChunkCoord = coord;
                InnerChunkCoord = innerChunkCoord;
            }

            public TakeKlotzCommand(BinaryReader r) : base(CommandCode)
            {
                ChunkCoord = new Vector3Int(
                    r.ReadInt32(),
                    r.ReadInt32(),
                    r.ReadInt32());
                InnerChunkCoord = new Vector3Int(
                    r.ReadInt32(),
                    r.ReadInt32(),
                    r.ReadInt32());
            }

            protected override void Serialize(BinaryWriter w)
            {
                w.Write(ChunkCoord.x);
                w.Write(ChunkCoord.y);
                w.Write(ChunkCoord.z);
                w.Write(InnerChunkCoord.x);
                w.Write(InnerChunkCoord.y);
                w.Write(InnerChunkCoord.z);
            }
        }
    }
}
