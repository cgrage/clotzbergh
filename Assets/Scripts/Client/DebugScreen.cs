using UnityEngine;

namespace Clotzbergh.Client
{
    public class DebugScreen : MonoBehaviour
    {
        public GameClient GameClient;

        void OnGUI()
        {
            if (GameClient == null || GameClient.Viewer == null)
                return;

            GUIStyle style = new() { fontSize = 16 };
            style.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);

            Vector3Int viewerChunkCoords = WorldChunk.PositionToChunkCoords(GameClient.Viewer.position);
            GUI.Label(new Rect(Screen.width - 250 - 25, Screen.height - 250 - 25, 270, 200),
                $"Pos: {GameClient.Viewer.position}\n" +
                $"Coord: {viewerChunkCoords}\n" +
                $"Chk Count: {GameClient.ChunkStore.ChunkCount}\n" +
                $"Act Count: {GameClient.ChunkStore.ActiveChunkCount}\n" +
                $"Rec.Chunks (total): {GameClient.Stats.ReceivedChunks}\n" +
                $"Rec.Chunks (1/s): {GameClient.Stats.ReceivedChunksPerSecLastSec}\n" +
                $"Rec.MByte (total): {GameClient.Stats.ReceivedBytes / 1024 / 1024}\n" +
                $"Rec.KByte (1/s): {GameClient.Stats.ReceivedBytesPerSecLastSec / 1024}",
                style);
        }
    }
}
