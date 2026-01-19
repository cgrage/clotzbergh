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

        public KlotzTypeSet64 PossibleTypesAt(RelKlotzCoords coords, KlotzDirection dir)
        {
            if (dir == SupportedDirs[0])
                return _possibleTypes[coords.X, coords.Y, coords.Z, 0];
            else return _possibleTypes[coords.X, coords.Y, coords.Z, 1];
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
            RecalculateSuperpositionsInRange(WorldDef.ChunkSubDivs);

            while (NonCompleted.Count > 0)
            {
                RelKlotzCoords coords = NextRandomElement(NonCompleted);
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

        private void RecalculateSuperpositionsInRange(KlotzSize size)
        {
            for (int z = 0; z < size.Z; z++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    for (int x = 0; x < size.X; x++)
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

        private void RecalculateSuperpositionsAffectedBy(RelKlotzCoords coords)
        {
            SubKlotz subKlotz = SubKlotzAt(coords);
            KlotzType type = subKlotz.Type;
            KlotzSize size = KlotzKB.Size(type);
            KlotzDirection dir = subKlotz.Direction;

            KlotzSize worstCaseSize = new(KlotzKB.MaxKlotzSizeXZ - 1, KlotzKB.MaxKlotzSizeY - 1, KlotzKB.MaxKlotzSizeXZ - 1);
            KlotzSize aStart = new(coords.ToVector() - worstCaseSize.ToVector());
            KlotzSize aEnd = new(coords.ToVector() + size.ToVector());

            aStart.Clamp(KlotzSize.Zero, WorldDef.ChunkSubDivs);
            aEnd.Clamp(KlotzSize.Zero, WorldDef.ChunkSubDivs);

            for (int z = aStart.Z; z < aEnd.Z; z++)
            {
                for (int y = aStart.Y; y < aEnd.Y; y++)
                {
                    for (int x = aStart.X; x < aEnd.X; x++)
                    {
                        if (IsCompletedAt(x, y, z))
                            continue;

                        RelKlotzCoords pCoords = new(x, y, z);
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

        public static bool DoIntersect(RelKlotzCoords posA, KlotzType typeA, KlotzDirection dirA, RelKlotzCoords posB, KlotzType typeB, KlotzDirection dirB)
        {
            // Limitations
            if (dirA != KlotzDirection.ToPosX || dirB != KlotzDirection.ToPosX)
            {
                throw new NotImplementedException();
            }

            KlotzSize sizeA = KlotzKB.Size(typeA);
            KlotzSize sizeB = KlotzKB.Size(typeB);

            return
                posA.X < posB.X + sizeB.X && posA.X + sizeA.X > posB.X &&
                posA.Y < posB.Y + sizeB.Y && posA.Y + sizeA.Y > posB.Y &&
                posA.Z < posB.Z + sizeB.Z && posA.Z + sizeA.Z > posB.Z;
        }

        private void Collapse(RelKlotzCoords rootCoords)
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
