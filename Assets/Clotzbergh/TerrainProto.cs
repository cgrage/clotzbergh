using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.CodeEditor;
using UnityEngine;

public static class TerrainProto
{
    public abstract class Command
    {
        public enum CodeValue : byte
        {
            GetChunk = 1,
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
                CodeValue.GetChunk => new GetChunkCommand(reader),
                CodeValue.ChuckData => new ChunkDataCommand(reader),
                _ => throw new IOException("Invalid command"),
            };
        }
    }

    public class GetChunkCommand : Command
    {
        public Vector3Int Coord { get; private set; }

        public GetChunkCommand(Vector3Int coord) : base(CodeValue.GetChunk)
        {
            Coord = coord;
        }

        public GetChunkCommand(BinaryReader reader) : base(CodeValue.GetChunk)
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
