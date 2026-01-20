using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace Clotzbergh.Client
{
    /// <summary>
    /// Debug window behavior that displays information about the game client and player selection.
    /// This script is attached to a panel in the UI and renders the debug information.
    /// </summary>
    public class DebugTextRenderer : MonoBehaviour
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
            if (!TryGetComponent<TextMeshProUGUI>(out var textMeshPro))
                return;

            StringBuilder debugText = new();

            debugText.AppendLine($"Frames/s: {_sampledInfo.Frames.PerSecond:F0}");
            debugText.AppendLine($"RecChunks/s: {_sampledInfo.ReceivedChunks.PerSecond:F0}");
            debugText.AppendLine($"RecKB/s: {_sampledInfo.ReceivedBytes.PerSecond / 1024:F0}");
            debugText.AppendLine($"Meshes/s: {_sampledInfo.GeneratedMeshes.PerSecond:F0}");

            if (GameClient != null)
            {
                debugText.AppendLine();
                debugText.AppendLine($"Viewer.Pos: {GameClient.Viewer.position}");
                debugText.AppendLine($"Viewer.ChunkCoords: {WorldChunk.PositionToChunkCoords(GameClient.Viewer.position)}");
                debugText.AppendLine($"ChunkStore.Count: {GameClient.ChunkStore.ChunkCount}");
                debugText.AppendLine($"ChunkStore.Active: {GameClient.ChunkStore.ActiveChunkCount}");
                debugText.AppendLine($"Client.ReceivedChunks: {GameClient.Stats.ReceivedChunks}");
                debugText.AppendLine($"Client.ReceivedMB: {GameClient.Stats.ReceivedBytes / 1024 / 1024}");
            }

            if (PlayerSelection != null)
            {
                debugText.AppendLine();
                debugText.AppendLine($"Viewed.Pos: {PlayerSelection.ViewedPosition}");
                debugText.AppendLine($"Viewed.Chunk.Coord: {PlayerSelection.ViewedChunk?.Coords}");
                debugText.AppendLine($"Viewed.Chunk.SelectionUpdates: {PlayerSelection.ViewedChunk?.SelectionUpdateCounter}");
                debugText.AppendLine($"Viewed.Klotz.Root: {PlayerSelection.ViewedKlotz?.RootCoords}");
                debugText.AppendLine($"Viewed.Klotz.Type: {PlayerSelection.ViewedKlotz?.Type}");
            }

            textMeshPro.SetText(debugText.ToString());
        }
    }
}
