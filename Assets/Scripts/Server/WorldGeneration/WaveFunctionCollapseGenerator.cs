using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapseGenerator : ChunkGenerator
{
    private SubKlotzVoxelSuperPosition[,,] _positions;
    private List<Vector3Int> _nonCollapsed;

    public enum GeneralVoxelType
    {
        Air, Ground, AirOrGround
    }

    private static readonly KlotzType[] AllGroundTypes = {
        KlotzType.Plate1x1, KlotzType.Plate1x2, KlotzType.Plate1x3, KlotzType.Plate1x4,
        KlotzType.Plate1x6, KlotzType.Plate1x8, KlotzType.Plate2x2, KlotzType.Plate2x3,
        KlotzType.Plate2x4, KlotzType.Plate2x6, KlotzType.Plate2x8, KlotzType.Plate4x4,
        KlotzType.Plate4x6, KlotzType.Plate4x8, KlotzType.Plate6x6, KlotzType.Plate6x8,
        KlotzType.Plate8x8,
        KlotzType.Brick1x1, KlotzType.Brick1x2, KlotzType.Brick1x3, KlotzType.Brick1x4,
        KlotzType.Brick1x6, KlotzType.Brick1x8, KlotzType.Brick2x2, KlotzType.Brick2x3,
        KlotzType.Brick2x4, KlotzType.Brick2x6, KlotzType.Brick2x8, KlotzType.Brick4x6 };

    private static readonly KlotzType[] AllGroundTypesSortedByVolumeDesc = SortByVolumeDesc(AllGroundTypes);
    private static readonly KlotzType[] All1x1x1Types = { KlotzType.Air, KlotzType.Plate1x1 };

    private static readonly KlotzTypeSet64 AirSet = new(new KlotzType[] { KlotzType.Air });
    private static readonly KlotzTypeSet64 AllGroundTypesSet = new(AllGroundTypes);
    private static readonly KlotzTypeSet64 All1x1x1TypesSet = new(All1x1x1Types);

    private static KlotzType[] SortByVolumeDesc(IEnumerable<KlotzType> types)
    {
        List<KlotzType> list = new(types);
        list.Sort((a, b) =>
        {
            Vector3Int sa = KlotzKB.KlotzSize(a);
            Vector3Int sb = KlotzKB.KlotzSize(b);
            return (sb.x * sb.y * sb.z).CompareTo(sa.x * sa.y * sa.z);
        });
        return list.ToArray();
    }

    public struct SubKlotzVoxelSuperPosition
    {
        private readonly GeneralVoxelType _generalType;
        private KlotzTypeSet64 _possibleTypes;
        private SubKlotz? _collapsedType;

        public SubKlotzVoxelSuperPosition(GeneralVoxelType generalType)
        {
            _generalType = generalType;
            _possibleTypes = new();
            _collapsedType = null;

            if (generalType == GeneralVoxelType.Air)
            {
                _collapsedType = SubKlotz.Air;
            }
            else
            {
                if (generalType == GeneralVoxelType.AirOrGround)
                    _possibleTypes = _possibleTypes.Merge(AirSet);

                _possibleTypes = _possibleTypes.Merge(AllGroundTypesSet);
            }
        }

        public readonly GeneralVoxelType GeneralType => _generalType;

        public readonly KlotzTypeSet64 PossibleTypes => _possibleTypes;

        public readonly bool IsAir => _collapsedType?.Type == KlotzType.Air;

        public readonly bool IsFreeGround => !_collapsedType.HasValue && GeneralType != GeneralVoxelType.Air;

        public readonly bool IsCollapsed => _collapsedType.HasValue;

        public readonly SubKlotz CollapsedType => _collapsedType.Value;

        public void RemovePossibleType(KlotzType type)
        {
            _possibleTypes = _possibleTypes.Remove(type);
        }

        public void SetCollapsedType(SubKlotz value)
        {
            _collapsedType = value;
        }

        public override string ToString()
        {
            if (_collapsedType.HasValue) { return $"Collapsed to: {_collapsedType.Value}"; }
            else { return $"SuperPos of {_possibleTypes.Count}"; }
        }
    }

    public override WorldChunk InnerGenerate()
    {
        _nonCollapsed = new();
        _positions = new SubKlotzVoxelSuperPosition[
            WorldDef.ChunkSubDivsX,
            WorldDef.ChunkSubDivsY,
            WorldDef.ChunkSubDivsZ];

        PlaceGround();
        RecalculateSuperpositionsInRange(Vector3Int.zero, WorldDef.ChunkSubDivs);

        while (_nonCollapsed.Count > 0)
        {
            Vector3Int coords = _nonCollapsed[_random.Next(0, _nonCollapsed.Count)];
            Collapse(coords);

            RecalculateSuperpositionsAffectedBy(coords);
        }

        return ToWorldChunk();
    }

    private void PlaceGround()
    {
        for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
        {
            for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
            {
                int x = _chunkCoords.x * WorldDef.ChunkSubDivsX + ix;
                int z = _chunkCoords.z * WorldDef.ChunkSubDivsZ + iz;
                int groundStart = Mathf.RoundToInt(_heightMap.At(x, z) / WorldDef.SubKlotzSize.y);

                for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                {
                    int y = _chunkCoords.y * WorldDef.ChunkSubDivsY + iy;
                    if (y > groundStart)
                    {
                        _positions[ix, iy, iz] = new SubKlotzVoxelSuperPosition(GeneralVoxelType.Air);
                    }
                    else if (y == groundStart)
                    {
                        _positions[ix, iy, iz] = new SubKlotzVoxelSuperPosition(GeneralVoxelType.AirOrGround);
                        _nonCollapsed.Add(new(ix, iy, iz));
                    }
                    else
                    {
                        _positions[ix, iy, iz] = new SubKlotzVoxelSuperPosition(GeneralVoxelType.Ground);
                        _nonCollapsed.Add(new(ix, iy, iz));
                    }
                }
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

        SubKlotzVoxelSuperPosition voxel = _positions[x, y, z];
        if (voxel.IsCollapsed)
            return;

        foreach (KlotzType type in voxel.PossibleTypes)
        {
            KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
            if (!IsPossible(x, y, z, type, dir))
            {
                _positions[x, y, z].RemovePossibleType(type);
            }
        }
    }

    private void RecalculateSuperpositionsAffectedBy(Vector3Int coords)
    {
        SubKlotz subKlotz = _positions[coords.x, coords.y, coords.z].CollapsedType;
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
                    SubKlotzVoxelSuperPosition voxel = _positions[x, y, z];
                    if (voxel.IsCollapsed)
                        continue;

                    Vector3Int pCoords = new(x, y, z);

                    foreach (KlotzType pType in voxel.PossibleTypes)
                    {
                        Vector3Int pSize = KlotzKB.KlotzSize(pType);
                        KlotzDirection pDir = KlotzDirection.ToPosX; // TODO: All other directions

                        if (DoIntersect(coords, size, dir, pCoords, pSize, pDir))
                        {
                            _positions[x, y, z].RemovePossibleType(pType);
                        }
                    }

                    if (voxel.PossibleTypes.ContainsOnly(All1x1x1TypesSet))
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

    private bool IsPossible(int x, int y, int z, KlotzType type, KlotzDirection dir)
    {
        Vector3Int root = new(x, y, z);
        Vector3Int size = KlotzKB.KlotzSize(type);

        for (int subZ = 0; subZ < size.z; subZ++)
        {
            for (int subX = 0; subX < size.x; subX++)
            {
                for (int subY = 0; subY < size.y; subY++)
                {
                    Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
                        root, new(subX, subY, subZ), dir);

                    if (IsOutOfBounds(coords))
                        return false;

                    if (!_positions[coords.x, coords.y, coords.z].IsFreeGround)
                        return false;
                }
            }
        }

        return true;
    }

    private void Collapse(Vector3Int rootCoords)
    {
        SubKlotzVoxelSuperPosition rootVoxel = _positions[rootCoords.x, rootCoords.y, rootCoords.z];
        if (rootVoxel.IsCollapsed)
            throw new InvalidOperationException("Already collapsed (Collapse)");

        KlotzType? option1 = null;
        KlotzType? option2 = null;
        KlotzType type;

        foreach (var testType in AllGroundTypesSortedByVolumeDesc)
        {
            if (rootVoxel.PossibleTypes.Contains(testType))
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
            type = _random.Next() % 2 == 0 ? option1.Value : option2.Value;
        }
        else
        {
            type = option1.Value;
        }

        if (type == KlotzType.Air)
        {
            _positions[rootCoords.x, rootCoords.y, rootCoords.z].SetCollapsedType(SubKlotz.Air);
            _nonCollapsed.Remove(rootCoords);
        }
        else
        {
            PlaceKlotzInRandDirection(rootCoords, type);
        }
    }

    void PlaceKlotzInRandDirection(Vector3Int rootCoords, KlotzType type)
    {
        KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
        Vector3Int size = KlotzKB.KlotzSize(type);
        KlotzColor color = ColorFromHeight(_chunkCoords.y * WorldDef.ChunkSubDivsY + rootCoords.y);
        KlotzVariant variant = NextRandVariant();

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
                        _positions[coords.x, coords.y, coords.z].SetCollapsedType(SubKlotz.Root(type, color, variant, dir));
                    }
                    else
                    {
                        _positions[coords.x, coords.y, coords.z].SetCollapsedType(SubKlotz.NonRoot(type, dir, subX, subY, subZ));
                    }

                    _nonCollapsed.Remove(coords);
                }
            }
        }
    }

    private WorldChunk ToWorldChunk()
    {
        WorldChunk chunk = new();

        for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
        {
            for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
            {
                for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                {
                    chunk.Set(x, y, z, _positions[x, y, z].CollapsedType);
                }
            }
        }

        return chunk;
    }
}
