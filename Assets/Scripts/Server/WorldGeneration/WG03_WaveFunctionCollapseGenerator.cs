using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public class WG03_WaveFunctionCollapseGenerator : VoxelChunkGenerator
    {
        private static readonly KlotzDirection[] SupportedDirs = { KlotzDirection.ToPosX, /*KlotzDirection.ToPosZ*/ };

        private readonly KlotzTypeSet64[,,,] _possibleTypes;

        public WG03_WaveFunctionCollapseGenerator()
        {
            _possibleTypes = new KlotzTypeSet64[
                WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ,
                SupportedDirs.Length];
        }

        public KlotzTypeSet64 PossibleTypesAt(int x, int y, int z, KlotzDirection dir)
        {
            if (dir == SupportedDirs[0])
                return _possibleTypes[x, y, z, 0];
            else return _possibleTypes[x, y, z, 1];
        }

        public KlotzTypeSet64 PossibleTypesAt(Vector3Int coords, KlotzDirection dir)
        {
            if (dir == SupportedDirs[0])
                return _possibleTypes[coords.x, coords.y, coords.z, 0];
            else return _possibleTypes[coords.x, coords.y, coords.z, 1];
        }

        public void RemovePossibleTypeAt(int x, int y, int z, KlotzDirection dir, KlotzType type)
        {
            if (dir == SupportedDirs[0])
                _possibleTypes[x, y, z, 0] = _possibleTypes[x, y, z, 0].Remove(type);
            else _possibleTypes[x, y, z, 1] = _possibleTypes[x, y, z, 1].Remove(type);
        }

        protected override WorldChunk InnerGenerate()
        {
            PlaceGround();
            RecalculateSuperpositionsInRange(Vector3Int.zero, WorldDef.ChunkSubDivs);

            while (NonCompleted.Count > 0)
            {
                Vector3Int coords = NextRandomElement(NonCompleted);
                Collapse(coords);

                RecalculateSuperpositionsAffectedBy(coords);
            }

            return ToWorldChunk();
        }

        protected override void OnGeneralVoxelTypeDecided(int x, int y, int z, GeneralVoxelType generalType)
        {
            if (generalType == GeneralVoxelType.AirOrGround)
            {
                for (int i = 0; i < SupportedDirs.Length; i++)
                {
                    _possibleTypes[x, y, z, i] = KlotzTypeSet64.Air.Merge(GroundDefinitions.NiceGroundTypesSet);
                }
            }
            else if (generalType == GeneralVoxelType.Ground)
            {
                for (int i = 0; i < SupportedDirs.Length; i++)
                {
                    _possibleTypes[x, y, z, i] = GroundDefinitions.NiceGroundTypesSet;
                }
            }
        }

        private void RecalculateSuperpositionsInRange(Vector3Int from, Vector3Int to)
        {
            for (int z = from.z; z < to.z; z++)
            {
                for (int y = from.y; y < to.y; y++)
                {
                    for (int x = from.x; x < to.x; x++)
                    {
                        RecalculateSuperpositionsOfPos(x, y, z);
                    }
                }
            }
        }

        private void RecalculateSuperpositionsOfPos(int x, int y, int z)
        {
            if (IsOutOfBounds(x, y, z))
                return;

            if (IsCompletedAt(x, y, z))
                return;

            foreach (var dir in SupportedDirs)
            {
                foreach (KlotzType type in PossibleTypesAt(x, y, z, dir))
                {
                    if (!IsFreeToComplete(x, y, z, type, dir))
                    {
                        RemovePossibleTypeAt(x, y, z, dir, type);
                    }
                }
            }
        }

        private void RecalculateSuperpositionsAffectedBy(Vector3Int coords)
        {
            SubKlotz subKlotz = SubKlotzAt(coords);
            KlotzType type = subKlotz.Type;
            KlotzSize size = KlotzKB.Size(type);
            KlotzDirection dir = subKlotz.Direction;

            KlotzSize worstCaseSize = new(KlotzKB.MaxKlotzSizeXZ - 1, KlotzKB.MaxKlotzSizeY - 1, KlotzKB.MaxKlotzSizeXZ - 1);
            Vector3Int aStart = coords - worstCaseSize.ToVector();
            Vector3Int aEnd = coords + size.ToVector();

            aStart.Clamp(Vector3Int.zero, WorldDef.ChunkSubDivs);
            aEnd.Clamp(Vector3Int.zero, WorldDef.ChunkSubDivs);

            for (int z = aStart.z; z < aEnd.z; z++)
            {
                for (int y = aStart.y; y < aEnd.y; y++)
                {
                    for (int x = aStart.x; x < aEnd.x; x++)
                    {
                        if (IsCompletedAt(x, y, z))
                            continue;

                        Vector3Int pCoords = new(x, y, z);
                        bool only1x1x1 = true;

                        foreach (var pDir in SupportedDirs)
                        {
                            foreach (KlotzType pType in PossibleTypesAt(x, y, z, pDir))
                            {
                                if (DoIntersect(coords, type, dir, pCoords, pType, pDir))
                                {
                                    RemovePossibleTypeAt(x, y, z, pDir, pType);
                                }
                            }

                            if (!PossibleTypesAt(x, y, z, pDir).ContainsOnly(KlotzTypeSet64.All1x1x1Types))
                            {
                                only1x1x1 = false;
                            }
                        }

                        if (only1x1x1)
                        {
                            Collapse(pCoords);
                        }
                    }
                }
            }
        }

        public static bool DoIntersect(Vector3Int posA, KlotzType typeA, KlotzDirection dirA, Vector3Int posB, KlotzType typeB, KlotzDirection dirB)
        {
            // Limitations
            if (dirA != KlotzDirection.ToPosX || dirB != KlotzDirection.ToPosX)
            {
                throw new NotImplementedException();
            }

            KlotzSize sizeA = KlotzKB.Size(typeA);
            KlotzSize sizeB = KlotzKB.Size(typeB);

            return
                posA.x < posB.x + sizeB.X && posA.x + sizeA.X > posB.x &&
                posA.y < posB.y + sizeB.Y && posA.y + sizeA.Y > posB.y &&
                posA.z < posB.z + sizeB.Z && posA.z + sizeA.Z > posB.z;
        }

        private void Collapse(Vector3Int rootCoords)
        {
            if (IsCompletedAt(rootCoords))
                throw new InvalidOperationException("Already collapsed (Collapse)");

            const int maxOptions = 2;

            List<Tuple<KlotzType, KlotzDirection>> options = new();
            KlotzDirection[] dirsToCheck = NextRandomCoinFlip() ?
                new[] { KlotzDirection.ToPosX/*, KlotzDirection.ToPosZ*/ } :
                new[] { /*KlotzDirection.ToPosZ,*/ KlotzDirection.ToPosX };

            foreach (var testType in GroundDefinitions.NiceGroundTypesSortedByVolumeDesc)
            {
                foreach (var dir in dirsToCheck)
                {
                    if (PossibleTypesAt(rootCoords, dir).Contains(testType))
                    {
                        options.Add(new(testType, dir));
                        if (options.Count >= maxOptions)
                            break;
                    }
                }

                if (options.Count >= maxOptions)
                    break;
            }

            if (options.Count == 0)
                throw new InvalidOperationException("No PossibleTypes found in Collapse");

            Tuple<KlotzType, KlotzDirection> option = NextRandomElement(options);

            if (option.Item1 == KlotzType.Air)
            {
                PlaceAir(rootCoords);
            }
            else
            {
                PlaceKlotz(rootCoords, option.Item1, option.Item2);
            }
        }
    }
}
