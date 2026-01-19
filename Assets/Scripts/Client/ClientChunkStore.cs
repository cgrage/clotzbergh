using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Client
{
    public class ClientChunkStore
    {
        public Transform ParentObject { get; set; }
        public IClientSideOps AsyncTerrainOps { get; set; }
        public Material KlotzMat { get; set; }
        public PlayerSelection Selection { get; set; }

        private readonly Dictionary<ChunkCoords, ClientChunk> _dict = new();

        // _activeChunks is sorted by their priority
        private readonly List<ClientChunk> _activeChunks = new();

        public int ChunkCount { get => _dict.Count; }
        public int ActiveChunkCount { get => _activeChunks.Count; }

        /// <summary>
        /// This method is expected to be run on main thread.
        /// </summary>
        public void OnUpdate()
        {
            int reqCount = 0;

            foreach (var chunk in _activeChunks)
            {
                if (reqCount < 20 && chunk.UpdateMeshOrRequestMeshUpdateIfNeeded())
                    reqCount++;
            }
        }

        /// <summary>
        /// This method is expected to be run on main thread.
        /// </summary>
        public void OnViewerMoved(ChunkCoords newCoords)
        {
            int intLoadDist = (int)WorldDef.ChunkLoadDistance;

            int xStart = Math.Max(newCoords.X - intLoadDist, WorldDef.Limits.MinCoordsX);
            int xEnd = Math.Min(newCoords.X + intLoadDist, WorldDef.Limits.MaxCoordsX);
            int yStart = Math.Max(newCoords.Y - intLoadDist, WorldDef.Limits.MinCoordsY);
            int yEnd = Math.Min(newCoords.Y + intLoadDist, WorldDef.Limits.MaxCoordsY);
            int zStart = Math.Max(newCoords.Z - intLoadDist, WorldDef.Limits.MinCoordsZ);
            int zEnd = Math.Min(newCoords.Z + intLoadDist, WorldDef.Limits.MaxCoordsZ);
            // HashSet<ClientChunk> killList = new(_activeChunks);

            for (int z = zStart; z <= zEnd; z++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int x = xStart; x <= xEnd; x++)
                    {
                        ChunkCoords chunkCoords = new(x, y, z);
                        float dist = ChunkCoords.Distance(newCoords, chunkCoords);

                        if (dist <= WorldDef.ChunkLoadDistance)
                        {
                            var chunk = GetOrCreate(chunkCoords);
                            chunk.OnViewerMoved(dist);

                            if (chunk.IsActive)
                            {
                                if (!_activeChunks.Contains(chunk))
                                    _activeChunks.Add(chunk);
                            }
                            else
                            {
                                _activeChunks.Remove(chunk);
                            }

                            // killList.Remove(chunk);
                        }
                    }
                }
            }

            _activeChunks.Sort((a, b) => a.LoadPriority.CompareTo(b.LoadPriority));

            // foreach (var chunk in killList)
            // {
            //     _dict.Remove(chunk.Coords);
            //     _activeChunks.Remove(chunk);
            //
            //     chunk.CleanUp();
            // }
        }

        public void OnViewerAimChanged()
        {
            //
        }

        public void OnWorldChunkReceived(ChunkCoords coords, ulong version, WorldChunk chunk)
        {
            GetOrCreate(coords).OnWorldUpdate(version, chunk);
        }

        /// <summary>
        /// Tries to find the <c>ClientChunk</c> with the given coords. If it cannot be found the <c>ClientChunk</c> is 
        /// created.
        /// This method shall be the only method that creates <c>ClientChunk</c>s.
        /// This method is expected to be run on main thread.
        /// </summary>
        private ClientChunk GetOrCreate(ChunkCoords coords)
        {
            // try to find the existing
            if (_dict.TryGetValue(coords, out ClientChunk thisChunk))
                return thisChunk;

            // nothing found there. we need a new one.
            // Debug.Log($"Create new terrain chunk ${coords}");

            thisChunk = new ClientChunk(coords, ParentObject, AsyncTerrainOps, Selection, KlotzMat);
            _dict.Add(coords, thisChunk);

            // find the neighbors
            _dict.TryGetValue(new ChunkCoords(coords.X - 1, coords.Y, coords.Z), out ClientChunk neighborXM1);
            _dict.TryGetValue(new ChunkCoords(coords.X + 1, coords.Y, coords.Z), out ClientChunk neighborXP1);
            _dict.TryGetValue(new ChunkCoords(coords.X, coords.Y - 1, coords.Z), out ClientChunk neighborYM1);
            _dict.TryGetValue(new ChunkCoords(coords.X, coords.Y + 1, coords.Z), out ClientChunk neighborYP1);
            _dict.TryGetValue(new ChunkCoords(coords.X, coords.Y, coords.Z - 1), out ClientChunk neighborZM1);
            _dict.TryGetValue(new ChunkCoords(coords.X, coords.Y, coords.Z + 1), out ClientChunk neighborZP1);

            // and link the neighbors
            if (neighborXM1 != null) { thisChunk.NeighborXM1 = neighborXM1; neighborXM1.NeighborXP1 = thisChunk; }
            if (neighborXP1 != null) { thisChunk.NeighborXP1 = neighborXP1; neighborXP1.NeighborXM1 = thisChunk; }
            if (neighborYM1 != null) { thisChunk.NeighborYM1 = neighborYM1; neighborYM1.NeighborYP1 = thisChunk; }
            if (neighborYP1 != null) { thisChunk.NeighborYP1 = neighborYP1; neighborYP1.NeighborYM1 = thisChunk; }
            if (neighborZM1 != null) { thisChunk.NeighborZM1 = neighborZM1; neighborZM1.NeighborZP1 = thisChunk; }
            if (neighborZP1 != null) { thisChunk.NeighborZP1 = neighborZP1; neighborZP1.NeighborZM1 = thisChunk; }

            return thisChunk;
        }
    }
}
