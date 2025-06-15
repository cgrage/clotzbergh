using UnityEngine;

namespace Clotzbergh.Client
{
    /// <summary>
    /// Debug window behavior that displays information about the game client and player selection.
    /// This script is attached to a panel in the UI and renders the debug information.
    /// </summary>
    public class DebugWindowUpdate : MonoBehaviour
    {
        public GameClient GameClient;
        public PlayerSelection PlayerSelection;

        void OnGUI()
        {
            GUIStyle style = new() { fontSize = 16 };
            style.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);

            if (!TryGetComponent<RectTransform>(out var rectTransform))
                return;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            Vector2 guiTopLeft = new(topLeft.x, Screen.height - topLeft.y);  // Y is inverted in GUI

            if (GameClient != null)
            {
                Vector2 location = guiTopLeft + new Vector2(10, 10);
                RenderGameClientInfo(GameClient, location, style);
            }

            if (PlayerSelection != null)
            {
                Vector2 location = guiTopLeft + new Vector2(10, 200);
                RenderPlayerSelectionInfo(PlayerSelection, location, style);
            }
        }

        private static void RenderGameClientInfo(GameClient gameClient, Vector2 location, GUIStyle style)
        {
            Vector3Int viewerChunkCoords = WorldChunk.PositionToChunkCoords(gameClient.Viewer.position);
            GUI.Label(new Rect(location, new Vector2(270, 200)),
                $"Pos: {gameClient.Viewer.position}\n" +
                $"Coord: {viewerChunkCoords}\n" +
                $"Chk Count: {gameClient.ChunkStore.ChunkCount}\n" +
                $"Act Count: {gameClient.ChunkStore.ActiveChunkCount}\n" +
                $"Rec.Chunks (total): {gameClient.Stats.ReceivedChunks}\n" +
                $"Rec.Chunks (1/s): {gameClient.Stats.ReceivedChunksPerSecLastSec}\n" +
                $"Rec.MByte (total): {gameClient.Stats.ReceivedBytes / 1024 / 1024}\n" +
                $"Rec.KByte (1/s): {gameClient.Stats.ReceivedBytesPerSecLastSec / 1024}",
                style);
        }

        private static void RenderPlayerSelectionInfo(PlayerSelection playerSelection, Vector2 location, GUIStyle style)
        {
            GUI.Label(new Rect(location, new Vector2(270, 200)),
                $"Hit: {playerSelection.ViewedPosition}\n" +
                $"Type: {playerSelection.ViewedKlotz?.type}\n",
                style);
        }
    }
}
