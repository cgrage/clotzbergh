using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using UnityEngine;
using Clotzbergh.Server.WorldGeneration;

namespace Clotzbergh.Server
{
    public enum WorldType
    {
        FlatMicroBlocks,
        FlatRegular,
        HillyMicroBlocks,
        HillyRegular,
    }

    public class WorldMap
    {
        private readonly string _worldName;
        private readonly int _worldSeed;
        private readonly WorldGenerator _generator;
        private readonly Dictionary<Vector3Int, WorldChunkState> _worldState;
        private readonly Dictionary<ClientId, ClientWorldMapState> _clientStates;
        private readonly string _chunkDataPath;
        private readonly CancellationTokenSource _runCancelTS;
        private readonly List<Thread> _loaderThreads;
        private readonly List<Thread> _saverThreads;
        private readonly BlockingCollection<LoaderThreadArgs> _generationRequestQueue;
        private readonly List<Vector3Int> _toSaveList;
        private ulong _clientListVersion;

        private class LoaderThreadArgs
        {
            public Vector3Int coords;
        }

        private class WorldChunkState
        {
            public ulong Version { get; set; }
            public ulong StoredVersion { get; set; }
            public WorldChunk Chunk { get; set; }
        }

        public int LoaderThreadCount = 4;
        public int SaverThreadsCount = 1;

        public WorldMap(string name, WorldType type, int seed)
        {
            _worldName = name;
            _worldSeed = seed;
            _generator = new(seed, type);
            _worldState = new();
            _clientStates = new();
            _chunkDataPath = Path.Combine(Application.persistentDataPath, name);
            _runCancelTS = new();
            _loaderThreads = new();
            _saverThreads = new();
            _generationRequestQueue = new();
            _toSaveList = new();
            _clientListVersion = 0;

            if (Directory.Exists(_chunkDataPath))
            {
                Debug.Log($"Loading data from {_chunkDataPath}");
            }
            else
            {
                Debug.Log($"Create data in {_chunkDataPath}");
                Directory.CreateDirectory(_chunkDataPath);
            }
        }

        public Mesh GeneratePreviewMesh(int dist)
        {
            return _generator.GeneratePreviewMesh(dist);
        }

        private WorldChunkState GetWorldState(Vector3Int coords, bool requestIfNotPresent = false)
        {
            lock (_worldState)
            {
                if (_worldState.TryGetValue(coords, out WorldChunkState chunkState))
                    return chunkState;

                if (requestIfNotPresent)
                {
                    _generationRequestQueue.Add(new() { coords = coords });
                    _worldState.Add(coords, new()
                    {
                        Version = 0,
                        StoredVersion = 0,
                        Chunk = null,
                    });
                }

                return chunkState;
            }
        }

        public ulong GetWorldChunkStateVersion(Vector3Int coords)
        {
            WorldChunkState state = GetWorldState(coords, true);
            if (state == null)
                return 0;

            return state.Version;
        }

        private ClientWorldMapState GetClientState(ClientId id)
        {
            lock (_clientStates)
            {
                return _clientStates[id];
            }
        }

        public void AddClient(ClientId id)
        {
            // Debug.Log($"ServerMap: AddClient ${id}");

            lock (_clientStates)
            {
                bool added = _clientStates.TryAdd(id, new($"Player {id.Value}")
                {
                    PlayerPosition = Vector3.zero,
                    PlayerChunkCoords = Vector3Int.zero,
                });

                if (!added)
                    return;

                _clientListVersion++;
            }
        }

        public void PlayerMoved(ClientId id, Vector3 newPosition)
        {
            // Debug.Log($"ServerMap: PlayerMoved ${id} ${newCoords}");
            ClientWorldMapState state = GetClientState(id);

            Vector3Int newChunkCoords = WorldChunk.PositionToChunkCoords(newPosition);
            bool movedChunk = state.PlayerChunkCoords != newChunkCoords;

            state.PlayerPosition = newPosition;
            state.PlayerChunkCoords = newChunkCoords;

            if (movedChunk)
            {
                state.ResetChunkPriority(newChunkCoords);
            }
        }

        public void RemoveClient(ClientId id)
        {
            lock (_clientStates)
            {
                _clientStates.Remove(id);
                _clientListVersion--;
            }
        }

        /// <summary>
        /// Called by <c>ClientUpdaterThread</c>
        /// </summary>
        public WorldChunkUpdate GetNextChunkUpdate(ClientId id)
        {
            ClientWorldMapState state = GetClientState(id);

            Vector3Int? next = state.GetNextAndSetUpdated(GetWorldChunkStateVersion);
            if (!next.HasValue)
                return null;

            WorldChunkState worldState = GetWorldState(next.Value);
            if (worldState == null || worldState.Version == 0)
                return null;

            // TODO: Problem: chunk can change at any moment in time / async
            return new()
            {
                Coords = next.Value,
                Version = worldState.Version,
                Chunk = worldState.Chunk,
            };
        }

        public ServerStatusUpdate GetNextServerStatus(ClientId id)
        {
            ClientWorldMapState state = GetClientState(id);
            ServerStatusUpdate update = new();

            lock (_clientStates)
            {
                update.PlayerPositions = _clientStates.Values.Select(clientState => clientState.PlayerPosition).ToArray();

                if (state.SeenClientListVersion != _clientListVersion)
                {
                    update.PlayerList = new PlayerInfo[_clientStates.Count];
                    int i = 0;
                    foreach (var playerEntry in _clientStates)
                    {
                        update.PlayerList[i++] = new()
                        {
                            Name = playerEntry.Value.PlayerName,
                            Flags = (id == playerEntry.Key) ? PlayerFlags.IsYou : 0,
                        };
                    }

                    state.SeenClientListVersion = _clientListVersion;
                }
            }

            return update;
        }

        public void PlayerTakeKlotz(ClientId id, Vector3Int chunkCoords, Vector3Int innerChunkCoords)
        {
            // Debug.Log($"ServerMap: PlayerTakeKlotz ${id} ${chunkCoords} ${innerChunkCoords}");
            WorldChunkState worldState = GetWorldState(chunkCoords);
            if (worldState == null)
            {
                // this should not happen..
                return;
            }

            // TODO: Sure we want to do this without lock?!
            worldState.Chunk.RemoveKlotz(innerChunkCoords);
            worldState.Version++;

            lock (_toSaveList)
            {
                if (!_toSaveList.Contains(chunkCoords))
                    _toSaveList.Add(chunkCoords);
            }
        }

        public void StartThreads()
        {
            Debug.LogFormat("World management threads starting...");

            for (int i = 0; i < LoaderThreadCount; i++)
            {
                var thread = new Thread(LoaderThreadMain) { Name = $"LoaderThread{i}" };
                _loaderThreads.Add(thread);
                thread.Start();
            }

            for (int i = 0; i < SaverThreadsCount; i++)
            {
                var thread = new Thread(SaverThreadMain) { Name = $"SaverThread{i}" };
                _saverThreads.Add(thread);
                thread.Start();
            }

            Debug.LogFormat("World management threads started");
        }

        public void StopThreads()
        {
            Debug.LogFormat("World management threads stopping...");
            _runCancelTS.Cancel();

            foreach (var thread in _loaderThreads)
            {
                if (!thread.Join(TimeSpan.FromSeconds(1)))
                    thread.Abort();
            }

            foreach (var thread in _saverThreads)
            {
                if (!thread.Join(TimeSpan.FromSeconds(3)))
                    thread.Abort();
            }

            Debug.LogFormat("World management threads stopped");
        }

        private void LoaderThreadMain()
        {
            try
            {
                while (!_runCancelTS.Token.IsCancellationRequested)
                {
                    LoaderThreadArgs args = _generationRequestQueue.Take(_runCancelTS.Token);
                    Vector3Int coords = args.coords;
                    WorldChunk chunk = LoadWorldChunk(coords);

                    if (chunk == null)
                    {
                        chunk = _generator.GetChunk(coords);
                        SaveWorldChunk(chunk, coords);
                    }

                    lock (_worldState)
                    {
                        WorldChunkState state = _worldState[coords];
                        if (state.Version != 0)
                            throw new ArgumentException("World chunk was already created by someone else");

                        state.Version = 1;
                        state.StoredVersion = 1;
                        state.Chunk = chunk;
                    }
                }
            }
            catch (Exception ex)
            {
                if (_runCancelTS.Token.IsCancellationRequested || ex is ThreadAbortException)
                {
                    Debug.LogFormat($"LoaderThread stopped with exception ({ex.GetType().Name}).");
                }
                else
                {
                    Debug.LogException(ex);
                    Debug.LogFormat("LoaderThread stopped on exception (see above).");
                }
            }
        }

        private void SaverThreadMain()
        {
            try
            {
                while (!_runCancelTS.Token.IsCancellationRequested)
                {
                    Thread.Sleep(1000);

                    Vector3Int[] toSave;
                    lock (_toSaveList)
                    {
                        toSave = _toSaveList.ToArray();
                        _toSaveList.Clear();
                    }

                    if (toSave.Length == 0)
                        continue;

                    foreach (Vector3Int coords in toSave)
                    {
                        WorldChunk chunk;
                        ulong version;

                        lock (_worldState)
                        {
                            WorldChunkState state = _worldState[coords];
                            chunk = state.Chunk;
                            version = state.Version;
                        }

                        // TODO: Problem: chunk can change at any moment in time / async
                        SaveWorldChunk(chunk, coords);

                        lock (_worldState)
                        {
                            WorldChunkState state = _worldState[coords];
                            state.StoredVersion = version;
                        }
                    }

                    Debug.LogFormat($"Saved {toSave.Length} chunks");
                }
            }
            catch (Exception ex)
            {
                if (_runCancelTS.Token.IsCancellationRequested || ex is ThreadAbortException)
                {
                    Debug.LogFormat($"SaverThread stopped with exception ({ex.GetType().Name}).");
                }
                else
                {
                    Debug.LogException(ex);
                    Debug.LogFormat("SaverThread stopped on exception (see above).");
                }
            }
        }

        private WorldChunk LoadWorldChunk(Vector3Int coords)
        {
            string path = Path.Combine(_chunkDataPath, $"{coords.x},{coords.y},{coords.z}.chunk");
            if (!File.Exists(path))
                return null;

            try
            {
                byte[] data = File.ReadAllBytes(path);

                using MemoryStream memoryStream = new(data);
                using GZipStream gzipStream = new(memoryStream, CompressionMode.Decompress);
                using BinaryReader reader = new(gzipStream);

                return WorldChunk.Deserialize(reader);
            }
            catch
            {
                Debug.LogWarning($"Failed to load chunk {coords}.");
                return null;
            }
        }

        private void SaveWorldChunk(WorldChunk chunk, Vector3Int coords)
        {
            using MemoryStream memoryStream = new();
            using (GZipStream gzipStream = new(memoryStream, CompressionMode.Compress))
            using (BinaryWriter writer = new(gzipStream))
            {
                chunk.Serialize(writer);
            }

            byte[] data = memoryStream.ToArray();
            string path = Path.Combine(_chunkDataPath, $"{coords.x},{coords.y},{coords.z}.chunk");

            File.WriteAllBytes(path, data);
        }
    }
}
