using System.IO;
using UnityEngine;

namespace Clotzbergh
{
    public enum PlayerFlags
    {
        IsYou = 1,
    }

    public class PlayerInfo
    {
        public string Name { get; set; }
        public PlayerFlags Flags { get; set; }
    }

    public class ServerStatusUpdate
    {
        public Vector3[] PlayerPositions { get; set; }
        public PlayerInfo[] PlayerList { get; set; }

        /// <summary>
        /// The highest <see cref="IntercomProtocol.TakeKlotzCommand.Sequence"/> this client sent
        /// that the server has processed. Chunk data the client receives from now on reflects
        /// every take up to and including this one.
        /// </summary>
        public ulong LastProcessedTakeSequence { get; set; }

        public void Serialize(BinaryWriter w)
        {
            w.Write(LastProcessedTakeSequence);
            w.Write(PlayerPositions.Length);
            foreach (var pos in PlayerPositions)
            {
                w.Write(pos.x);
                w.Write(pos.y);
                w.Write(pos.z);
            }
            if (PlayerList != null)
            {
                w.Write((byte)1);
                foreach (var player in PlayerList)
                {
                    w.Write(player.Name);
                    w.Write((byte)player.Flags);
                }
            }
            else
            {
                w.Write((byte)0);
            }
        }

        public static ServerStatusUpdate Deserialize(BinaryReader r)
        {
            ulong lastProcessedTakeSequence = r.ReadUInt64();
            int playerCount = r.ReadInt32();
            Vector3[] playerPositions = new Vector3[playerCount];
            PlayerInfo[] playerList = null;
            for (int i = 0; i < playerCount; i++)
            {
                playerPositions[i] = new(
                    r.ReadSingle(),
                    r.ReadSingle(),
                    r.ReadSingle());
            }
            byte flags = r.ReadByte();
            if (flags > 0)
            {
                playerList = new PlayerInfo[playerCount];
                for (int i = 0; i < playerCount; i++)
                {
                    playerList[i] = new PlayerInfo()
                    {
                        Name = r.ReadString(),
                        Flags = (PlayerFlags)r.ReadByte()
                    };
                }
            }

            return new ServerStatusUpdate()
            {
                PlayerPositions = playerPositions,
                PlayerList = playerList,
                LastProcessedTakeSequence = lastProcessedTakeSequence,
            };
        }
    }
}
