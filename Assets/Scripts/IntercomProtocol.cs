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
        /// The player applied a tool to the klotz at Target. Carries the tool rather than the
        /// klotzes it hits, so the server works out the affected region itself.
        ///
        /// Anchor is the klotz the level tools clear down to - the one first aimed at when the
        /// button went down, so a drag levels to one plane. It is a position rather than a
        /// height for the same reason the tool is not a region: the server looks it up in its
        /// own world and takes nothing on the client's word. Equal to Target for every other
        /// tool, and while not dragging.
        /// </summary>
        public class ApplyToolCommand : Command
        {
            const CodeValue CommandCode = CodeValue.ApplyTool;

            public KlotzAddress Target;
            public KlotzAddress Anchor;
            public SelectionTool Tool;

            /// <summary>
            /// Per-client counter, increasing with every use. The server echoes the highest one
            /// it has processed back in <see cref="ServerStatusUpdate.LastProcessedToolSequence"/>,
            /// which tells the client which of its predicted changes are already reflected in
            /// the chunk data it receives.
            /// </summary>
            public ulong Sequence;

            public ApplyToolCommand(KlotzAddress target, KlotzAddress anchor, SelectionTool tool, ulong sequence)
                : base(CommandCode)
            {
                Target = target;
                Anchor = anchor;
                Tool = tool;
                Sequence = sequence;
            }

            public ApplyToolCommand(BinaryReader r) : base(CommandCode)
            {
                Target = ReadAddress(r);
                Anchor = ReadAddress(r);
                Tool = (SelectionTool)r.ReadByte();
                Sequence = r.ReadUInt64();
            }

            protected override void Serialize(BinaryWriter w)
            {
                WriteAddress(w, Target);
                WriteAddress(w, Anchor);
                w.Write((byte)Tool);
                w.Write(Sequence);
            }

            private static KlotzAddress ReadAddress(BinaryReader r)
            {
                ChunkCoords chunk = new(r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
                RelKlotzCoords inner = new(r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
                return new KlotzAddress(chunk, inner);
            }

            private static void WriteAddress(BinaryWriter w, KlotzAddress address)
            {
                w.Write(address.Chunk.X);
                w.Write(address.Chunk.Y);
                w.Write(address.Chunk.Z);
                w.Write(address.Inner.X);
                w.Write(address.Inner.Y);
                w.Write(address.Inner.Z);
            }
        }
    }
}
