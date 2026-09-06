using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Clotzbergh
{
    /// <summary>
    /// 
    /// </summary>
    public class WorldChunk
    {
        private readonly SubKlotz[,,] _klotzData;

        private int _klotzCount;
        private ulong _checksum;

        /// <summary>
        /// Bump for changes to the serialized layout that <see cref="ComputeFormatId"/> cannot
        /// see by itself, such as SubKlotz's bit packing or the order fields are written in.
        /// </summary>
        private const uint LayoutVersion = 1;

        /// <summary>
        /// Identifies what a serialized chunk's bytes mean. Derived from the klotz tables and
        /// the chunk size rather than maintained by hand, so renumbering a klotz type or
        /// resizing a chunk changes it on its own - those are exactly the changes that would
        /// otherwise read back as valid data with a different meaning.
        /// </summary>
        public static readonly uint FormatId = ComputeFormatId();

        private static uint ComputeFormatId()
        {
            uint hash = 2166136261; // FNV-1a

            void Feed(string text)
            {
                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
            }

            Feed($"v{LayoutVersion};");
            Feed($"{WorldDef.ChunkSubDivsX}x{WorldDef.ChunkSubDivsY}x{WorldDef.ChunkSubDivsZ};");

            foreach (KlotzType type in (KlotzType[])Enum.GetValues(typeof(KlotzType)))
                Feed($"{(int)type}:{type};");

            foreach (KlotzColor color in (KlotzColor[])Enum.GetValues(typeof(KlotzColor)))
                Feed($"{(int)color}:{color};");

            return hash;
        }

        public WorldChunk()
        {
            _klotzCount = 0;
            _checksum = 0;
            _klotzData = new SubKlotz[
                WorldDef.ChunkSubDivsX,
                WorldDef.ChunkSubDivsY,
                WorldDef.ChunkSubDivsZ];
        }

        /// <summary>
        /// A checksum of the whole chunk's contents. Set maintains it incrementally (XOR out the
        /// old cell's contribution, XOR in the new one) rather than recomputing it from scratch -
        /// so patching a single klotz stays O(klotz size), not O(chunk volume). Deserialize gets
        /// it straight off the wire instead of rebuilding it from the cells it just read - see
        /// SetUnchecked. Two chunks with this checksum equal very likely (not guaranteed) hold
        /// identical data.
        /// </summary>
        public ulong Checksum => _checksum;

        /// <summary>
        /// A full copy, safe to mutate independently of the original. A WorldChunk that has been
        /// handed to another thread (e.g. a mesh-generation worker) must not be mutated - clone it
        /// and swap the reference instead.
        /// </summary>
        public WorldChunk Clone()
        {
            WorldChunk copy = new();
            Array.Copy(_klotzData, copy._klotzData, _klotzData.Length);
            copy._klotzCount = _klotzCount;
            copy._checksum = _checksum;
            return copy;
        }

        /// <summary>
        /// Fills layers of the chunk with klotzes from fromHeight to toHeight (y-axis).
        /// </summary>
        public void LayerFill(int fromHeight = 0, int toHeight = WorldDef.ChunkSubDivsY)
        {
            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = fromHeight; y < toHeight; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        Set(x, y, z, SubKlotz.Root(
                            KlotzType.Plate1x1,
                            KlotzColor.White,
                            KlotzVariant.Zero,
                            KlotzDirection.ToPosX));
                    }
                }
            }
        }

        /// <summary>
        /// Fills the core of the chunk with klotzes in all three dimensions, leaving an empty border around it.
        /// </summary>
        public void CoreFill(int startPercent = 25, int endPercent = 75)
        {
            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        bool inCore =
                             x >= startPercent * WorldDef.ChunkSubDivsX / 100 && x < endPercent * WorldDef.ChunkSubDivsX / 100 &&
                             y >= startPercent * WorldDef.ChunkSubDivsY / 100 && y < endPercent * WorldDef.ChunkSubDivsY / 100 &&
                             z >= startPercent * WorldDef.ChunkSubDivsZ / 100 && z < endPercent * WorldDef.ChunkSubDivsZ / 100;

                        if (inCore)
                        {
                            Set(x, y, z, SubKlotz.Root(
                                KlotzType.Plate1x1,
                                KlotzColor.White,
                                KlotzVariant.Zero,
                                KlotzDirection.ToPosX));
                        }
                        else
                        {
                            Set(x, y, z, SubKlotz.Air);
                        }
                    }
                }
            }
        }

        public SubKlotz Get(int x, int y, int z) { return _klotzData[x, y, z]; }

        public SubKlotz Get(RelKlotzCoords coords) { return Get(coords.X, coords.Y, coords.Z); }

        public void Set(int x, int y, int z, SubKlotz t)
        {
            SubKlotz old = _klotzData[x, y, z];
            bool wasRoot = old.IsRootAndNotAir;
            _klotzData[x, y, z] = t;
            bool isRoot = t.IsRootAndNotAir;

            if (isRoot != wasRoot)
            {
                _klotzCount += wasRoot ? -1 : 1;
            }

            _checksum ^= HashCell(x, y, z, old.RawBits) ^ HashCell(x, y, z, t.RawBits);
        }

        public void Set(RelKlotzCoords coords, SubKlotz t) { Set(coords.X, coords.Y, coords.Z, t); }

        /// <summary>
        /// Sets a cell without maintaining _klotzCount or _checksum - for bulk-loading paths
        /// (deserialize) that know both up front and set them directly instead.
        /// </summary>
        protected void SetUnchecked(int x, int y, int z, SubKlotz t) { _klotzData[x, y, z] = t; }

        /// <summary>
        /// A position+value hash for one cell's checksum contribution. Air (rawBits 0) always
        /// hashes to 0 regardless of position, so a freshly-constructed (all-air) chunk's
        /// checksum is correctly 0 without having to touch all ChunkSubDivsX*Y*Z cells, and the
        /// running checksum only ever reflects non-air cells - mirroring _klotzCount.
        /// </summary>
        private static ulong HashCell(int x, int y, int z, uint rawBits)
        {
            if (rawBits == 0)
                return 0;

            ulong h = (uint)x;
            h = h * 2654435761u ^ (uint)y;
            h = h * 2654435761u ^ (uint)z;
            h = h * 2654435761u ^ rawBits;

            // Final avalanche mix (splitmix64's finalizer) so nearby cells/values don't produce
            // similar-looking hashes.
            h ^= h >> 33;
            h *= 0xff51afd7ed558ccdUL;
            h ^= h >> 33;
            h *= 0xc4ceb9fe1a85ec53UL;
            h ^= h >> 33;
            return h;
        }

        public Klotz[] ToKlotzArray()
        {
            Klotz[] result = new Klotz[_klotzCount];
            int i = 0;

            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        SubKlotz k = Get(x, y, z);

                        if (k.IsRootAndNotAir)
                        {
                            result[i++] = k.ToKlotz(x, y, z);
                        }
                    }
                }
            }

            if (i != _klotzCount)
            {
                throw new Exception(
                    $"Failed to convert to Klotz array (miscount, expected {_klotzCount}, got {i})");
            }

            return result;
        }

        private const int UseListIfFillLevelInPercent = 50; // 40,960

        public void Serialize(BinaryWriter w)
        {
            int fillLevel = (_klotzCount * 100) / WorldDef.SubKlotzPerChunkCount;
            bool asList = fillLevel < UseListIfFillLevelInPercent;

            w.Write(FormatId);
            w.Write(_checksum);

            if (asList)
            {
                w.Write((uint)1 << 31 | (uint)_klotzCount);

                foreach (Klotz klotz in ToKlotzArray())
                {
                    klotz.Serialize(w);
                }
            }
            else
            {
                w.Write((uint)0 << 31 | (uint)_klotzCount);

                for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
                {
                    for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                    {
                        for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                        {
                            Get(x, y, z).Serialize(w);
                        }
                    }
                }
            }
        }

        public static WorldChunk Deserialize(BinaryReader r)
        {
            WorldChunk chunk = new();

            uint formatId = r.ReadUInt32();
            if (formatId != FormatId)
            {
                throw new InvalidDataException(
                    $"Chunk format id {formatId:x8} does not match this build's {FormatId:x8}. " +
                    "The data was written with a different klotz table or chunk size.");
            }

            ulong checksum = r.ReadUInt64();

            uint bits = r.ReadUInt32();
            bool isList = (bits & (1 << 31)) != 0;
            int klotzCount = (int)(bits & ~(1 << 31));

            if (isList)
            {
                for (int i = 0; i < klotzCount; i++)
                {
                    chunk.PlaceKlotz(Klotz.Deserialize(r));
                }

                if (chunk._klotzCount != klotzCount)
                {
                    throw new Exception(
                        $"Klotz count mismatch detected during deserialization! Expected {klotzCount}, got {chunk._klotzCount}");
                }
            }
            else
            {
                for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
                {
                    for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                    {
                        for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                        {
                            chunk.SetUnchecked(x, y, z, SubKlotz.Deserialize(r));
                        }
                    }
                }

                chunk._klotzCount = klotzCount;
            }

            chunk._checksum = checksum;
            return chunk;
        }

        public void PlaceKlotz(Klotz klotz)
        {
            PlaceKlotz(klotz.Type, klotz.Color, klotz.Variant, klotz.Coords, klotz.Direction);
        }

        public void PlaceKlotz(KlotzType type, KlotzColor color, KlotzVariant variant, RelKlotzCoords rootCoords, KlotzDirection dir)
        {
            KlotzSize size = KlotzKB.Size(type);

            for (int subZ = 0; subZ < size.Z; subZ++)
            {
                for (int subX = 0; subX < size.X; subX++)
                {
                    for (int subY = 0; subY < size.Y; subY++)
                    {
                        RelKlotzCoords coords = SubKlotz.TranslateSubIndexToCoords(
                            rootCoords, new(subX, subY, subZ), dir);

                        if (subX == 0 && subY == 0 && subZ == 0)
                        {
                            Set(coords, SubKlotz.Root(type, color, variant, dir));
                        }
                        else
                        {
                            Set(coords, SubKlotz.NonRoot(type, dir, subX, subY, subZ));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes every klotz of this chunk that the region touches, whole - a klotz reaching
        /// into the region goes entirely, matching how the selection preview cuts it away. Only
        /// this chunk is affected, which covers a region spanning several because no klotz ever
        /// straddles a chunk border. Returns the roots removed, for the client to replay onto
        /// chunk data arriving from the server.
        /// </summary>
        public List<RelKlotzCoords> RemoveKlotzesIn(KlotzRegion region, ChunkCoords myCoords)
        {
            List<RelKlotzCoords> removed = new();
            BoundsInt bounds = region.RoughBounds;

            int originX = myCoords.X * WorldDef.ChunkSubDivsX;
            int originY = myCoords.Y * WorldDef.ChunkSubDivsY;
            int originZ = myCoords.Z * WorldDef.ChunkSubDivsZ;

            int xFrom = Math.Max(0, bounds.xMin - originX);
            int xTo = Math.Min(WorldDef.ChunkSubDivsX - 1, bounds.xMax - originX);
            int yFrom = Math.Max(0, bounds.yMin - originY);
            int yTo = Math.Min(WorldDef.ChunkSubDivsY - 1, bounds.yMax - originY);
            int zFrom = Math.Max(0, bounds.zMin - originZ);
            int zTo = Math.Min(WorldDef.ChunkSubDivsZ - 1, bounds.zMax - originZ);

            HashSet<RelKlotzCoords> seen = new();

            for (int z = zFrom; z <= zTo; z++)
            {
                for (int y = yFrom; y <= yTo; y++)
                {
                    for (int x = xFrom; x <= xTo; x++)
                    {
                        SubKlotz cell = Get(x, y, z);
                        if (cell.IsRoot && cell.IsAir)
                            continue;

                        RelKlotzCoords root = cell.RootPos(new(x, y, z));
                        if (!seen.Add(root))
                            continue;

                        SubKlotz rootCell = Get(root);
                        (RelKlotzCoords min, RelKlotzCoords max) = SubKlotz.TranslateToOccupiedRange(
                            root, rootCell.Type, rootCell.Direction);

                        if (!region.IntersectsAbs(min.ToAbs(myCoords), max.ToAbs(myCoords)))
                            continue;

                        RemoveKlotz(root);
                        removed.Add(root);
                    }
                }
            }

            return removed;
        }

        public void RemoveKlotz(RelKlotzCoords klotzCoords)
        {
            SubKlotz k = Get(klotzCoords);

            if (!k.IsRoot)
            {
                Debug.LogError($"Cannot RemoveKlotz at {klotzCoords} (not a root).");
                return;
            }

            if (k.IsAir)
            {
                Debug.LogError($"Cannot RemoveKlotz at {klotzCoords} (air).");
                return;
            }

            KlotzSize size = KlotzKB.Size(k.Type);

            for (int subZ = 0; subZ < size.Z; subZ++)
            {
                for (int subX = 0; subX < size.X; subX++)
                {
                    for (int subY = 0; subY < size.Y; subY++)
                    {
                        RelKlotzCoords coords = SubKlotz.TranslateSubIndexToCoords(
                            klotzCoords, new KlotzIndex(subX, subY, subZ), k.Direction);

                        Set(coords, SubKlotz.Air);
                    }
                }
            }
        }

        public static ChunkCoords PositionToChunkCoords(Vector3 position)
        {
            return new(
                Mathf.FloorToInt(position.x / WorldDef.ChunkSize.x),
                Mathf.FloorToInt(position.y / WorldDef.ChunkSize.y),
                Mathf.FloorToInt(position.z / WorldDef.ChunkSize.z));
        }

        public static Vector3 ChunkCoordsToPosition(ChunkCoords coords)
        {
            return Vector3.Scale(coords.ToVector(), WorldDef.ChunkSize);
        }

        public static float DistanceToChunkCenter(Vector3 position, ChunkCoords chunkCoords)
        {
            Vector3 chunkPosition = ChunkCoordsToPosition(chunkCoords);
            Vector3 chunkCenter = chunkPosition + WorldDef.ChunkSize / 2;
            return Vector3.Distance(position, chunkCenter);
        }

        /// <summary>
        /// Recounts the number of klotzes in the chunk and returns it.
        /// This function should only be used for debugging purposes.
        /// </summary>
        protected int RecountKlotzes()
        {
            int count = 0;

            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        if (Get(x, y, z).IsRootAndNotAir)
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Recomputes the checksum from scratch (O(chunk volume) - only for debugging, Set's
        /// incremental version is what's used normally).
        /// </summary>
        protected ulong RecomputeChecksum()
        {
            ulong checksum = 0;

            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        checksum ^= HashCell(x, y, z, Get(x, y, z).RawBits);
                    }
                }
            }

            return checksum;
        }

        /// <summary>
        /// Debug function to check the integrity of the chunk's klotz count and checksum.
        /// Throws an exception if a mismatch is detected.
        /// </summary>
        public void DebugCheckIntegrity()
        {
            int recount = RecountKlotzes();
            if (recount != _klotzCount)
            {
                throw new Exception(
                    $"Klotz count mismatch detected! Expected {_klotzCount}, recounted {recount}");
            }

            ulong recomputedChecksum = RecomputeChecksum();
            if (recomputedChecksum != _checksum)
            {
                throw new Exception(
                    $"Checksum mismatch detected! Expected {_checksum:x}, recomputed {recomputedChecksum:x}");
            }
        }
    }
}
