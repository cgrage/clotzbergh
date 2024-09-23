using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class TerrainProto
{
    public class Command
    {

    }

    public class GetChunkCommand : Command
    {
        public Vector3Int coord;
    }

    public class ChunkDataCommand : Command
    {
        public WorldChunk chunk;
    }

    public static Command ParseMessage(string message)
    {
        int eofw = message.IndexOf(' ');
        if (eofw < 0)
            throw new ArgumentException("Failed to parse message");

        string cmd = message[..eofw];
        string arg = message[(eofw + 1)..];

        if (cmd == "getch")
        {
            var parts = arg.Split(',');
            return new GetChunkCommand()
            {
                coord = new Vector3Int(
                    x: int.Parse(parts[0]),
                    y: int.Parse(parts[1]),
                    z: int.Parse(parts[2]))
            };
        }
        else if (cmd == "chunk")
        {
            return new ChunkDataCommand()
            {
                chunk = WorldChunk.FromBase64String(arg)
            };
        }
        else
        {
            return null;
        }
    }

    public static string BuildGetChunkCommand(Vector3Int coord)
    {
        return string.Format("getch {0},{1},{2}", coord.x, coord.y, coord.z);
    }

    public static string BuildChunkDataCommand(WorldChunk chunk)
    {
        return string.Format("chunk {0}", chunk.ToBase64String());
    }
}
