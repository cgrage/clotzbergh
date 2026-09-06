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
                ChunkData,
                ApplyTool,
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
                    CodeValue.ChunkData => new ChunkDataCommand(reader),
                    CodeValue.ApplyTool => new ApplyToolCommand(reader),
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
            const CodeValue CommandCode = CodeValue.ChunkData;

            public ChunkCoords Coords;
            public ulong Version;
            public WorldChunk Chunk;

            public ChunkDataCommand(ChunkCoords coords, ulong version, WorldChunk chunk) : base(CommandCode)
            {
                Coords = coords;
                Version = version;
                Chunk = chunk;
            }

            public ChunkDataCommand(BinaryReader r) : base(CommandCode)
            {
                Coords = new ChunkCoords(
                    r.ReadInt32(),
                    r.ReadInt32(),
                    r.ReadInt32());
                Version = r.ReadUInt64();
                Chunk = WorldChunk.Deserialize(r);
            }

            protected override void Serialize(BinaryWriter w)
            {
                w.Write(Coords.X);
                w.Write(Coords.Y);
                w.Write(Coords.Z);
                w.Write(Version);
                Chunk.Serialize(w);
            }
        }

        /// <summary>
        /// The player applied a tool to the klotz at the given position. Carries the tool rather
        /// than the klotzes it hits, so the server works out the affected region itself.
        /// </summary>
        public class ApplyToolCommand : Command
        {
            const CodeValue CommandCode = CodeValue.ApplyTool;

            public ChunkCoords ChunkCoords;
            public RelKlotzCoords InnerChunkCoord;
            public SelectionTool Tool;

            /// <summary>
            /// Per-client counter, increasing with every use. The server echoes the highest one
            /// it has processed back in <see cref="ServerStatusUpdate.LastProcessedToolSequence"/>,
            /// which tells the client which of its predicted changes are already reflected in
            /// the chunk data it receives.
            /// </summary>
            public ulong Sequence;

            public ApplyToolCommand(ChunkCoords coords, RelKlotzCoords innerChunkCoord, SelectionTool tool, ulong sequence)
                : base(CommandCode)
            {
                ChunkCoords = coords;
                InnerChunkCoord = innerChunkCoord;
                Tool = tool;
                Sequence = sequence;
            }

            public ApplyToolCommand(BinaryReader r) : base(CommandCode)
            {
                ChunkCoords = new ChunkCoords(
                    r.ReadInt32(),
                    r.ReadInt32(),
                    r.ReadInt32());
                InnerChunkCoord = new RelKlotzCoords(
                    r.ReadInt32(),
                    r.ReadInt32(),
                    r.ReadInt32());
                Tool = (SelectionTool)r.ReadByte();
                Sequence = r.ReadUInt64();
            }

            protected override void Serialize(BinaryWriter w)
            {
                w.Write(ChunkCoords.X);
                w.Write(ChunkCoords.Y);
                w.Write(ChunkCoords.Z);
                w.Write(InnerChunkCoord.X);
                w.Write(InnerChunkCoord.Y);
                w.Write(InnerChunkCoord.Z);
                w.Write((byte)Tool);
                w.Write(Sequence);
            }
        }
    }
}
