using System;
using UnityEngine;

namespace Clotzbergh.Server.WorldGeneration
{
    public class WG03_WaveFunctionCollapseGenerator : VoxelChunkGenerator
    {
        private readonly KlotzTypeSet64[,,] _possibleTypesArray;

        public WG03_WaveFunctionCollapseGenerator()
        {
            _possibleTypesArray = new KlotzTypeSet64[WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
        }

        public KlotzTypeSet64 PossibleTypesAt(int x, int y, int z)
        {
            return _possibleTypesArray[x, y, z];
        }

        public KlotzTypeSet64 PossibleTypesAt(Vector3Int coords)
        {
            return _possibleTypesArray[coords.x, coords.y, coords.z];
        }

        public void RemovePossibleTypeAt(int x, int y, int z, KlotzType type)
        {
            _possibleTypesArray[x, y, z] = _possibleTypesArray[x, y, z].Remove(type);
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
                _possibleTypesArray[x, y, z] = WorldGenDefs.AirSet.Merge(WorldGenDefs.AllGroundTypesSet);
            }
            else if (generalType == GeneralVoxelType.Ground)
            {
                _possibleTypesArray[x, y, z] = WorldGenDefs.AllGroundTypesSet;
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

            foreach (KlotzType type in PossibleTypesAt(x, y, z))
            {
                KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
                if (!IsFreeToComplete(x, y, z, type, dir))
                {
                    RemovePossibleTypeAt(x, y, z, type);
                }
            }
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

            for (int z = aStart.z; z < aEnd.z; z++)
            {
                for (int y = aStart.y; y < aEnd.y; y++)
                {
                    for (int x = aStart.x; x < aEnd.x; x++)
                    {
                        if (IsCompletedAt(x, y, z))
                            continue;

                        Vector3Int pCoords = new(x, y, z);

                        foreach (KlotzType pType in PossibleTypesAt(x, y, z))
                        {
                            Vector3Int pSize = KlotzKB.KlotzSize(pType);
                            KlotzDirection pDir = KlotzDirection.ToPosX; // TODO: All other directions

                            if (DoIntersect(coords, size, dir, pCoords, pSize, pDir))
                            {
                                RemovePossibleTypeAt(x, y, z, pType);
                            }
                        }

                        if (PossibleTypesAt(x, y, z).ContainsOnly(WorldGenDefs.All1x1x1TypesSet))
                        {
                            Collapse(pCoords);
                        }
                    }
                }
            }
        }

        public static bool DoIntersect(Vector3Int posA, Vector3Int sizeA, KlotzDirection dirA, Vector3Int posB, Vector3Int sizeB, KlotzDirection dirB)
        {
            // Limitations
            if (dirA != KlotzDirection.ToPosX || dirB != KlotzDirection.ToPosX)
            {
                throw new NotImplementedException();
            }

            return
                posA.x < posB.x + sizeB.x && posA.x + sizeA.x > posB.x &&
                posA.y < posB.y + sizeB.y && posA.y + sizeA.y > posB.y &&
                posA.z < posB.z + sizeB.z && posA.z + sizeA.z > posB.z;
        }

        private void Collapse(Vector3Int rootCoords)
        {
            if (IsCompletedAt(rootCoords))
                throw new InvalidOperationException("Already collapsed (Collapse)");

            KlotzType? option1 = null;
            KlotzType? option2 = null;
            KlotzType type;

            foreach (var testType in WorldGenDefs.AllGroundTypesSortedByVolumeDesc)
            {
                if (PossibleTypesAt(rootCoords).Contains(testType))
                {
                    if (option1.HasValue) { option2 = testType; break; }
                    else { option1 = testType; }
                }
            }

            if (!option1.HasValue)
                throw new InvalidOperationException("No PossibleTypes found in Collapse");

            if (option2.HasValue)
            {
                // 50:50 chance of the last 2 items in list
                type = NextRandomCoinFlip() ? option1.Value : option2.Value;
            }
            else
            {
                type = option1.Value;
            }

            if (type == KlotzType.Air)
            {
                PlaceAir(rootCoords);
            }
            else
            {
                KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
                PlaceKlotz(rootCoords, type, dir);
            }
        }
    }
}
