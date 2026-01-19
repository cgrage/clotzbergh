using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using Clotzbergh.Client.MeshGeneration;

namespace Clotzbergh.Client
{
    public interface IClientSideOps
    {
        void RequestMeshCalc(ClientChunk owner, WorldChunk world, int lod, ulong worldLocalVersion);
        void TakeKlotz(ChunkCoords chunkCoords, RelKlotzCoords innerChunkCoords);
    }

    public class Statistics
    {
        public long RenderedFrames { get; set; }
        public long ReceivedBytes { get; set; }
        public long ReceivedChunks { get; set; }
    }

    public class GameClient : MonoBehaviour, IClientSideOps
    {
        public string Hostname = "localhost";
        public int Port = 3000;
        public int MeshThreadCount = 4;
        public Transform Viewer;
        public Material Material;
        public PlayerSelection Selection;
        public GameObject DebugPanel;

        private Thread _connectionThread;

        private readonly List<Thread> _meshThreads = new();
        private readonly BlockingCollection<MeshRequest> _meshRequestQueue = new();
        private ChunkCoords _viewerChunkCoords = ChunkCoords.Invalid;
        private bool _isConnected = false;

        /// <summary>
        /// Used during <c>Update()</c> do decide if we were connected at last <c>Update()</c>
        /// </summary>
        private bool _wasConnected = false;
        private float _timeSinceLastClientStatus = 0f;
        private GameObject[] _allPlayers = new GameObject[0];

        private readonly CancellationTokenSource _runCancelTS = new();
        private readonly ClientChunkStore _chunkStore = new();
        private readonly BlockingCollection<Action<WebSocket>> _connectionThreadActionQueue = new();
        private readonly ConcurrentQueue<Action> _mainThreadActionQueue = new();
        private readonly Statistics _statistics = new();

        public Statistics Stats => _statistics; // for debug UI
        public ClientChunkStore ChunkStore => _chunkStore; // for debug UI

        /// <summary>
        /// Called by Unity
        /// </summary>
        void Start()
        {
            _chunkStore.ParentObject = transform;
            _chunkStore.AsyncTerrainOps = this;
            _chunkStore.KlotzMat = Material;
            _chunkStore.Selection = Selection;

            _connectionThread = new Thread(ConnectionThreadMain) { Name = "ConnectionThread" };
            _connectionThread.Start();

            for (int i = 0; i < MeshThreadCount; i++)
            {
                var thread = new Thread(MeshThreadMain) { Name = $"MeshTread{i}" };
                _meshThreads.Add(thread);
                thread.Start();
            }
        }

        void Update()
        {
            _timeSinceLastClientStatus += Time.deltaTime;
            _statistics.RenderedFrames++;

            if (_isConnected && !_wasConnected)
            {
                _mainThreadActionQueue.Clear();
                OnConnected();
            }

            if (!_isConnected && _wasConnected)
            {
                OnDisconnected();
            }

            if (_isConnected)
            {
                while (_mainThreadActionQueue.TryDequeue(out Action action))
                {
                    action();
                }

                ChunkCoords newCoords = WorldChunk.PositionToChunkCoords(Viewer.position);
                if (newCoords != _viewerChunkCoords)
                {
                    // Debug.Log($"Viewer moved to chunk ${newCoords}");
                    _viewerChunkCoords = newCoords;
                    _chunkStore.OnViewerMoved(newCoords);
                }

                if (_timeSinceLastClientStatus >= 0.1f) // 0.1s
                {
                    SendClientStatus(Viewer.position);
                    _timeSinceLastClientStatus = 0;
                }

                // update after "on moved" so that world can be requested on first frame
                _chunkStore.OnUpdate();
            }

            _wasConnected = _isConnected;

            if (Input.GetKeyDown(KeyCode.F11))
            {
                MeshGenerator.DoStudsAndHoles = !MeshGenerator.DoStudsAndHoles;
            }

            if (Input.GetKeyDown(KeyCode.F12))
            {
                DebugPanel.SetActive(!DebugPanel.activeSelf);
            }
        }

        /// <summary>
        /// Started on the connection thread
        /// </summary>
        void ConnectionThreadMain()
        {
            string url = string.Format("ws://{0}:{1}/intercom", Hostname, Port);
            using var ws = new WebSocket(url);
            ws.OnMessage += OnDataReceivedAsync;

            try
            {
                Debug.LogFormat("Connecting to {0}...", url);
                ws.Connect();

                if (ws.ReadyState != WebSocketState.Open)
                    throw new Exception("Connect Failed");

                _isConnected = true;

                while (!_runCancelTS.Token.IsCancellationRequested)
                {
                    var action = _connectionThreadActionQueue.Take(_runCancelTS.Token);
                    action(ws);
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;

                if (_runCancelTS.Token.IsCancellationRequested || ex is ThreadAbortException)
                {
                    Debug.LogFormat($"ConnectionThread stopped with exception ({ex.GetType().Name}).");
                }
                else
                {
                    Debug.LogException(ex);
                    Debug.LogFormat("ConnectionThread stopped on exception (see above).");
                }
            }
        }

        void MeshThreadMain()
        {
            try
            {
                while (!_runCancelTS.Token.IsCancellationRequested)
                {
                    MeshRequest request = _meshRequestQueue.Take(_runCancelTS.Token);
                    VoxelMeshBuilder meshData = MeshGenerator.GenerateTerrainMesh(request.Owner, request.Lod);

                    ToMainThread(() => { request.Owner.OnMeshUpdate(meshData, request.Lod, request.WorldLocalVersion); });
                }
            }
            catch (Exception ex)
            {
                if (_runCancelTS.Token.IsCancellationRequested || ex is ThreadAbortException)
                {
                    Debug.LogFormat($"MeshThread stopped with exception ({ex.GetType().Name}).");
                }
                else
                {
                    Debug.LogException(ex);
                    Debug.LogFormat("MeshThread stopped on exception (see above).");
                }
            }
        }

        void OnConnected()
        {
            Debug.LogFormat("TerrainClient connected");
        }

        void OnDisconnected()
        {
            Debug.LogFormat("TerrainClient disconnected");
        }

        private void ToMainThread(Action action)
        {
            _mainThreadActionQueue.Enqueue(action);
        }

        /// <summary>
        /// Called on Thread Pool Worker
        /// </summary>
        void OnDataReceivedAsync(object sender, MessageEventArgs e)
        {
            try { OnDataReceivedAsync(e.RawData); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        /// <summary>
        /// 
        /// </summary>
        void OnDataReceivedAsync(byte[] data)
        {
            _statistics.ReceivedBytes += data.Length;
            var cmd = IntercomProtocol.Command.FromBytes(data);
            // Debug.LogFormat($"Client: Cmd '{cmd.Code}', {data.Length} bytes");

            switch (cmd.Code)
            {
                case IntercomProtocol.Command.CodeValue.ServerStatus:
                    var statusCmd = (IntercomProtocol.ServerStatusCommand)cmd;
                    ToMainThread(() =>
                    {
                        UpdatePlayerPositions(statusCmd.Update);
                    });
                    break;

                case IntercomProtocol.Command.CodeValue.ChuckData:
                    var chunkDataCmd = (IntercomProtocol.ChunkDataCommand)cmd;
                    ToMainThread(() =>
                    {
                        _statistics.ReceivedChunks++;
                        _chunkStore.OnWorldChunkReceived(
                            chunkDataCmd.Coords,
                            chunkDataCmd.Version,
                            chunkDataCmd.Chunk);
                    });
                    break;
            }
        }

        /// <summary>
        /// Called by Unity
        /// </summary>
        void OnDestroy()
        {
            Debug.LogFormat("Closing client...");
            _runCancelTS.Cancel();

            if (_connectionThread != null && _connectionThread.IsAlive)
            {
                if (!_connectionThread.Join(TimeSpan.FromSeconds(3)))
                    _connectionThread.Abort();
            }

            foreach (var thread in _meshThreads)
            {
                if (!thread.Join(TimeSpan.FromSeconds(1)))
                    thread.Abort();
            }

            Debug.LogFormat("Closed client");
        }

        private void SendClientStatus(Vector3 newPosition)
        {
            _connectionThreadActionQueue.Add((ws) =>
            {
                IntercomProtocol.ClientStatusCommand cmd = new(newPosition);
                ws.Send(cmd.ToBytes());
            });
        }

        private void UpdatePlayerPositions(ServerStatusUpdate update)
        {
            int posCount = update.PlayerPositions.Length;
            // Debug.Log($"UpdatePlayerPositions ({posCount})");

            if (update.PlayerList == null)
            {
                if (_allPlayers.Length != posCount)
                {
                    Debug.LogWarning($"Got update for {posCount} but only know {_allPlayers.Length} players");
                    return;
                }
            }
            else
            {
                // delete existing players
                for (int i = 0; i < _allPlayers.Length; i++)
                {
                    GameObject.Destroy(_allPlayers[i]);
                }

                GameObject playerModel = GameObject.Find("PlayerModel");

                // add new players
                _allPlayers = new GameObject[update.PlayerList.Length];
                for (int i = 0; i < _allPlayers.Length; i++)
                {
                    _allPlayers[i] = Instantiate(playerModel, update.PlayerPositions[i], Quaternion.identity);
                    _allPlayers[i].name = update.PlayerList[i].Name;
                    _allPlayers[i].SetActive(!update.PlayerList[i].Flags.HasFlag(PlayerFlags.IsYou));
                }
            }

            for (int i = 0; i < update.PlayerPositions.Length; i++)
            {
                _allPlayers[i].transform.position = update.PlayerPositions[i];
            }
        }

        private readonly struct MeshRequest
        {
            private readonly ClientChunk owner;
            private readonly int lod;
            private readonly ulong worldLocalVersion;

            public MeshRequest(ClientChunk owner, int lod, ulong worldLocalVersion)
            {
                this.owner = owner;
                this.lod = lod;
                this.worldLocalVersion = worldLocalVersion;
            }

            public readonly ClientChunk Owner => owner;
            public readonly int Lod => lod;
            public readonly ulong WorldLocalVersion => worldLocalVersion;
        }

        void IClientSideOps.RequestMeshCalc(ClientChunk owner, WorldChunk world, int lod, ulong worldLocalVersion)
        {
            _meshRequestQueue.Add(new MeshRequest(owner, lod, worldLocalVersion));
        }

        void IClientSideOps.TakeKlotz(ChunkCoords chunkCoords, RelKlotzCoords innerChunkCoords)
        {
            _connectionThreadActionQueue.Add((ws) =>
            {
                // Debug.Log($"Client: TakeKlotz {chunkCoords}.{innerChunkCoords}");
                IntercomProtocol.TakeKlotzCommand cmd = new(chunkCoords, innerChunkCoords);
                ws.Send(cmd.ToBytes());
            });
        }
    }
}
