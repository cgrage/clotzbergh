using System.Collections;
using UnityEngine;

namespace Clotzbergh.Client
{
    /// <summary>
    /// Debug window behavior that displays information about the game client and player selection.
    /// This script is attached to a panel in the UI and renders the debug information.
    /// </summary>
    public class DebugWindowBehavior : MonoBehaviour
    {
        public GameClient GameClient;
        public PlayerSelection PlayerSelection;

        private readonly SampledInfo _sampledInfo = new();

        private class SampledCounter
        {
            public const float SampleInterval = 1f; // Interval in seconds for sampling

            private float _timeSinceLastSample = 0f;

            private long _lastValue = 0;
            private int _delta = 0;
            private float _deltaPerSecond = 0f;

            public long LastValue => _lastValue;
            public int Delta => _delta;
            public float PerSecond => _deltaPerSecond;

            public bool Advance(float deltaTime)
            {
                _timeSinceLastSample += deltaTime;
                return _timeSinceLastSample >= SampleInterval;
            }

            public void Sample(long currentValue)
            {
                if (_timeSinceLastSample >= SampleInterval)
                {
                    _delta = (int)(currentValue - _lastValue);
                    _deltaPerSecond = _delta / SampleInterval;

                    _lastValue = currentValue;
                    _timeSinceLastSample -= SampleInterval;
                }

            }
        }

        private class SampledInfo
        {
            public SampledCounter Frames { get; private set; } = new();
            public SampledCounter ReceivedBytes { get; private set; } = new();
            public SampledCounter ReceivedChunks { get; private set; } = new();
            public SampledCounter GeneratedMeshes { get; private set; } = new();

            public void Update(GameClient gameClient, float deltaTime)
            {
                if (Frames.Advance(deltaTime)) Frames.Sample(gameClient.Stats.RenderedFrames);
                if (ReceivedBytes.Advance(deltaTime)) ReceivedBytes.Sample(gameClient.Stats.ReceivedBytes);
                if (ReceivedChunks.Advance(deltaTime)) ReceivedChunks.Sample(gameClient.Stats.ReceivedChunks);
                if (GeneratedMeshes.Advance(deltaTime)) GeneratedMeshes.Sample(MeshGeneration.MeshGenerator.MeshGenerationCount);
            }
        }

        void Update()
        {
            if (GameClient == null)
                return;

            _sampledInfo.Update(GameClient, Time.deltaTime);
        }

        void OnGUI()
        {
            GUIStyle style = new() { fontSize = 12 };
            style.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);

            if (!TryGetComponent<RectTransform>(out var rectTransform))
                return;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            Vector2 guiTopLeft = new(topLeft.x, Screen.height - topLeft.y);  // Y is inverted in GUI

            {
                Vector2 location = guiTopLeft + new Vector2(10, 10);
                RenderSampledInfo(_sampledInfo, location, style);
            }

            if (GameClient != null)
            {
                Vector2 location = guiTopLeft + new Vector2(10, 80);
                RenderGameClientInfo(GameClient, location, style);
            }

            if (PlayerSelection != null)
            {
                Vector2 location = guiTopLeft + new Vector2(10, 200);
                RenderPlayerSelectionInfo(PlayerSelection, location, style);
            }
        }

        private void RenderSampledInfo(SampledInfo sampledInfo, Vector2 location, GUIStyle style)
        {
            GUI.Label(new Rect(location, new Vector2(270, 200)),
                $"Frames/s: {sampledInfo.Frames.PerSecond:F0}\n" +
                $"RecChunks/s: {sampledInfo.ReceivedChunks.PerSecond:F0}\n" +
                $"RecKB/s: {sampledInfo.ReceivedBytes.PerSecond / 1024:F0}\n" +
                $"Meshes/s: {sampledInfo.GeneratedMeshes.PerSecond:F0}\n",
                style);
        }

        private static void RenderGameClientInfo(GameClient gameClient, Vector2 location, GUIStyle style)
        {
            Vector3Int viewerChunkCoords = WorldChunk.PositionToChunkCoords(gameClient.Viewer.position);
            GUI.Label(new Rect(location, new Vector2(270, 200)),
                $"Pos: {gameClient.Viewer.position}\n" +
                $"Coord: {viewerChunkCoords}\n" +
                $"ChkCount: {gameClient.ChunkStore.ChunkCount}\n" +
                $"ActCount: {gameClient.ChunkStore.ActiveChunkCount}\n" +
                $"RecChunks: {gameClient.Stats.ReceivedChunks}\n" +
                $"RecMB: {gameClient.Stats.ReceivedBytes / 1024 / 1024}\n",
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
