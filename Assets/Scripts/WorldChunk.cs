using System;
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

        public WorldChunk()
        {
            _klotzCount = 0;
            _klotzData = new SubKlotz[
                WorldDef.ChunkSubDivsX,
                WorldDef.ChunkSubDivsY,
                WorldDef.ChunkSubDivsZ];
        }

        public void FloodFill(int toHeight = WorldDef.ChunkSubDivsY)
        {
            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < toHeight; y++)
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

        public SubKlotz Get(Vector3Int coords) { return Get(coords.x, coords.y, coords.z); }

        public void Set(int x, int y, int z, SubKlotz t)
        {
            bool wasRoot = _klotzData[x, y, z].IsRootAndNotAir;
            _klotzData[x, y, z] = t;
            bool isRoot = t.IsRootAndNotAir;

            if (isRoot != wasRoot)
            {
                _klotzCount += wasRoot ? -1 : 1;
            }
        }

        public void Set(Vector3Int coords, SubKlotz t) { Set(coords.x, coords.y, coords.z, t); }

        protected void SetUncounted(int x, int y, int z, SubKlotz t) { _klotzData[x, y, z] = t; }

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
                throw new Exception("Internal data counting error");

            return result;
        }

        private const int UseListIfFillLevelInPercent = 50; // 40,960

        public void Serialize(BinaryWriter w)
        {
            int fillLevel = (_klotzCount * 100) / WorldDef.SubKlotzPerChunkCount;
            bool asList = fillLevel < UseListIfFillLevelInPercent;

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

            uint bits = r.ReadUInt32();
            bool isList = (bits & (1 << 31)) != 0;
            int klotzCount = (int)(bits & ~(1 << 31));

            if (isList)
            {
                for (int i = 0; i < klotzCount; i++)
                {
                    chunk.PlaceKlotz(Klotz.Deserialize(r));
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
                            chunk.SetUncounted(x, y, z, SubKlotz.Deserialize(r));
                        }
                    }
                }
            }

            return chunk;
        }

        public void PlaceKlotz(Klotz klotz)
        {
            PlaceKlotz(klotz.Type, klotz.Color, klotz.Variant, klotz.Coords, klotz.Direction);
        }

        public void PlaceKlotz(KlotzType type, KlotzColor color, KlotzVariant variant, Vector3Int rootCoords, KlotzDirection dir)
        {
            Vector3Int size = KlotzKB.KlotzSize(type);

            for (int subZ = 0; subZ < size.z; subZ++)
            {
                for (int subX = 0; subX < size.x; subX++)
                {
                    for (int subY = 0; subY < size.y; subY++)
                    {
                        Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
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

        public void RemoveKlotz(Vector3Int klotzCoords)
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

            Vector3Int size = KlotzKB.KlotzSize(k.Type);

            for (int subZ = 0; subZ < size.z; subZ++)
            {
                for (int subX = 0; subX < size.x; subX++)
                {
                    for (int subY = 0; subY < size.y; subY++)
                    {
                        Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
                            klotzCoords, new Vector3Int(subX, subY, subZ), k.Direction);

                        Set(coords, SubKlotz.Air);
                    }
                }
            }
        }

        public static Vector3Int PositionToChunkCoords(Vector3 position)
        {
            return new(
                Mathf.FloorToInt(position.x / WorldDef.ChunkSize.x),
                Mathf.FloorToInt(position.y / WorldDef.ChunkSize.y),
                Mathf.FloorToInt(position.z / WorldDef.ChunkSize.z));
        }

        public static Vector3 ChunkCoordsToPosition(Vector3Int coords)
        {
            return Vector3.Scale(coords, WorldDef.ChunkSize);
        }

        public static float DistanceToChunkCenter(Vector3 position, Vector3Int chunkCoords)
        {
            Vector3 chunkPosition = ChunkCoordsToPosition(chunkCoords);
            Vector3 chunkCenter = chunkPosition + WorldDef.ChunkSize / 2;
            return Vector3.Distance(position, chunkCenter);
        }

        public static int ChunkDistance(Vector3Int a, Vector3Int b)
        {
            return (int)Vector3Int.Distance(a, b);
        }
    }
}
