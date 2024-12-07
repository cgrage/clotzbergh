using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public class WG04_WaveFunctionCollapseGeneratorV2 : VoxelChunkGenerator
    {
        private static readonly KlotzDirection[] SupportedDirs = { KlotzDirection.ToPosX, /*KlotzDirection.ToPosZ*/ };

        private readonly KlotzTypeSet64[,,,] _possibleTypes;
        private readonly HitCube8x3x8[,,] _hitCubes;

        public WG04_WaveFunctionCollapseGeneratorV2()
        {
            _possibleTypes = new KlotzTypeSet64[
                WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ,
                SupportedDirs.Length];
            _hitCubes = new HitCube8x3x8[
                WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
        }

        public KlotzTypeSet64 PossibleTypesAt(Vector3Int coords, KlotzDirection dir)
        {
            if (dir == SupportedDirs[0])
                return _possibleTypes[coords.x, coords.y, coords.z, 0];
            else return _possibleTypes[coords.x, coords.y, coords.z, 1];
        }

        public void RemovePossibleTypeAt(Vector3Int coords, KlotzDirection dir, KlotzType type)
        {
            if (dir == SupportedDirs[0])
                _possibleTypes[coords.x, coords.y, coords.z, 0] = _possibleTypes[coords.x, coords.y, coords.z, 0].Remove(type);
            else _possibleTypes[coords.x, coords.y, coords.z, 1] = _possibleTypes[coords.x, coords.y, coords.z, 1].Remove(type);
        }

        public HitCube8x3x8 GetHitCube(Vector3Int coords)
        {
            return _hitCubes[coords.x, coords.y, coords.z];
        }

        public void SetHitCube(Vector3Int coords, HitCube8x3x8 hitCube)
        {
            _hitCubes[coords.x, coords.y, coords.z] = hitCube;
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
                    _possibleTypes[x, y, z, i] = WorldGenDefs.AirSet.Merge(WorldGenDefs.AllGroundTypesSet);
                }
            }
            else if (generalType == GeneralVoxelType.Ground)
            {
                for (int i = 0; i < SupportedDirs.Length; i++)
                {
                    _possibleTypes[x, y, z, i] = WorldGenDefs.AllGroundTypesSet;
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
                        if (IsCompletedAt(x, y, z))
                            continue;

                        Vector3Int coords = new(x, y, z);
                        foreach (var dir in SupportedDirs)
                        {
                            RecalculateSuperpositionsOfPos(coords, dir);
                        }
                    }
                }
            }
        }

        private void RecalculateSuperpositionsOfPos(Vector3Int coords, KlotzDirection dir)
        {
            foreach (KlotzType type in PossibleTypesAt(coords, dir))
            {
                if (!IsFreeToComplete(coords, type, dir))
                {
                    RemovePossibleTypeAt(coords, dir, type);
                }
            }

            SetHitCube(coords, HitCube8x3x8.FromSet(PossibleTypesAt(coords, dir)));
        }

        private void RecalculateSuperpositionsAffectedBy(Vector3Int coords)
        {
            SubKlotz subKlotz = SubKlotzAt(coords);
            KlotzType type = subKlotz.Type;
            Vector3Int size = KlotzKB.KlotzSize(type);
            KlotzDirection dir = subKlotz.Direction;

            Vector3Int worstCaseSize = new(KlotzKB.MaxKlotzSizeXZ - 1, KlotzKB.MaxKlotzSizeY - 1, KlotzKB.MaxKlotzSizeXZ - 1);
            Vector3Int aStart = coords - worstCaseSize;
            Vector3Int aEnd = coords + size;

            aStart.Clamp(Vector3Int.zero, WorldDef.ChunkSubDivs);
            aEnd.Clamp(Vector3Int.zero, WorldDef.ChunkSubDivs);

            for (int x = aStart.x; x < aEnd.x; x++)
            {
                for (int z = aStart.z; z < aEnd.z; z++)
                {
                    for (int y = aStart.y; y < aEnd.y; y++)
                    {
                        if (IsCompletedAt(x, y, z))
                            continue;

                        Vector3Int pCoords = new(x, y, z);
                        Vector3Int relPos = pCoords - coords;
                        HitCube8x3x8 relHitMask = HitCube8x3x8.Draw(type, relPos);

                        bool only1x1x1 = true;

                        foreach (var pDir in SupportedDirs)
                        {
                            if (GetHitCube(pCoords).Hits(relHitMask))
                            {
                                // TODO: Other dirs
                                RecalculateSuperpositionsOfPos(pCoords, KlotzDirection.ToPosX);
                            }

                            if (!PossibleTypesAt(pCoords, pDir).ContainsOnly(WorldGenDefs.All1x1x1TypesSet))
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

        private void Collapse(Vector3Int rootCoords)
        {
            if (IsCompletedAt(rootCoords))
                throw new InvalidOperationException("Already collapsed (Collapse)");

            const int maxOptions = 2;

            List<Tuple<KlotzType, KlotzDirection>> options = new();
            KlotzDirection[] dirsToCheck = NextRandomCoinFlip() ?
                new[] { KlotzDirection.ToPosX/*, KlotzDirection.ToPosZ*/ } :
                new[] { /*KlotzDirection.ToPosZ,*/ KlotzDirection.ToPosX };

            foreach (var testType in WorldGenDefs.AllGroundTypesSortedByVolumeDesc)
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
