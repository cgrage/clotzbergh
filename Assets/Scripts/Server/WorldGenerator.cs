using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class WorldGenerator
{
    protected IHeightMap HeightMap { get; } = new DefaultHeightMap();

    public WorldChunk GetChunk(Vector3Int chunkCoords)
    {
        ChunkGenerator gen = new MicroBlockWorldGenerator();
        // ChunkGenerator gen = new WaveFunctionCollapseGenerator();

        return gen.Generate(chunkCoords, HeightMap);
    }

    public Mesh GeneratePreviewMesh(int dist)
    {
        int size = 2 * dist;

        Vector3[] vertices = new Vector3[size * size];
        int[] triangles = new int[(size - 1) * (size - 1) * 6];

        int vIndex = 0;
        for (int y = -dist; y < dist; y++)
        {
            for (int x = -dist; x < dist; x++)
            {
                vertices[vIndex++] = new Vector3(
                    x * WorldDef.SubKlotzSize.x,
                    HeightMap.At(x, y),
                    y * WorldDef.SubKlotzSize.z);
            }
        }

        int triIndex = 0;
        for (int iy = 0; iy < size - 1; iy++)
        {
            for (int ix = 0; ix < size - 1; ix++)
            {
                int current = ix + iy * size;

                triangles[triIndex++] = current;
                triangles[triIndex++] = current + size;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = current + size;
                triangles[triIndex++] = current + size + 1;
            }
        }

        Mesh mesh = new()
        {
            vertices = vertices,
            triangles = triangles
        };

        mesh.RecalculateNormals();
        return mesh;
    }
}

public abstract class ChunkGenerator
{
    private static readonly object RandomCreationLock = new();

    protected Random _random;

    protected Vector3Int _chunkCoords;

    protected IHeightMap _heightMap;

    public virtual WorldChunk Generate(Vector3Int chunkCoords, IHeightMap heightMap)
    {
        lock (RandomCreationLock) { _random = new(chunkCoords.x + chunkCoords.y * 1000 + chunkCoords.z * 1000000); }
        _chunkCoords = chunkCoords;
        _heightMap = heightMap;

        return InnerGenerate();
    }

    public abstract WorldChunk InnerGenerate();

    protected bool IsOutOfBounds(Vector3Int coords)
    {
        return IsOutOfBounds(coords.x, coords.y, coords.z);
    }

    protected bool IsOutOfBounds(int x, int y, int z)
    {
        return
            x < 0 || y < 0 || z < 0 ||
            x >= WorldDef.ChunkSubDivsX ||
            y >= WorldDef.ChunkSubDivsY ||
            z >= WorldDef.ChunkSubDivsZ;
    }

    /// <summary>
    /// Little helper
    /// </summary>
    protected KlotzColor NextRandColor()
    {
        return (KlotzColor)_random.Next(0, (int)KlotzColor.NextFree);
    }

    protected KlotzColor ColorFromHeight(int y)
    {
        if (y < -85) return KlotzColor.DarkBlue;
        if (y < -80) return KlotzColor.Azure;
        if (y < -70) return KlotzColor.Yellow;
        if (y < -20) return KlotzColor.DarkGreen;
        if (y < 30) return KlotzColor.DarkBrown;
        if (y < 70) return KlotzColor.Gray;
        return KlotzColor.White;
    }

    /// <summary>
    /// Little helper
    /// </summary>
    protected KlotzVariant NextRandVariant()
    {
        return (KlotzVariant)(uint)_random.Next(0, KlotzVariant.MaxValue + 1);
    }

}

public class MicroBlockWorldGenerator : ChunkGenerator
{
    public override WorldChunk InnerGenerate()
    {
        WorldChunk chunk = new();

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
                        chunk.Set(ix, iy, iz, SubKlotz.Air);
                    }
                    else
                    {
                        chunk.Set(ix, iy, iz, SubKlotz.Root(
                            KlotzType.Plate1x1,
                            ColorFromHeight(y),
                            NextRandVariant(),
                            KlotzDirection.ToPosX));
                    }
                }
            }
        }

        // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 16), KlotzDirection.ToPosX);
        // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 18), KlotzDirection.ToPosX);
        // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 16), KlotzDirection.ToPosZ);
        // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(13, 39, 16), KlotzDirection.ToPosZ);
        // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 15), KlotzDirection.ToNegX);
        // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 13), KlotzDirection.ToNegX);
        // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 15), KlotzDirection.ToNegZ);
        // chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(18, 39, 15), KlotzDirection.ToNegZ);

        return chunk;
    }
}

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

    private static readonly KlotzType[] AllGroundTypesSortedByVolume = SortByVolume(AllGroundTypes);
    private static readonly KlotzType[] All1x1x1Types = { KlotzType.Air, KlotzType.Plate1x1 };

    private static readonly KlotzTypeSet AirSet = new(new KlotzType[] { KlotzType.Air });
    private static readonly KlotzTypeSet AllGroundTypesSet = new(AllGroundTypes);
    private static readonly KlotzTypeSet All1x1x1TypesSet = new(All1x1x1Types);

    private static KlotzType[] SortByVolume(IEnumerable<KlotzType> types)
    {
        List<KlotzType> list = new(types);
        list.Sort((a, b) =>
        {
            Vector3Int sa = KlotzKB.KlotzSize(a);
            Vector3Int sb = KlotzKB.KlotzSize(b);
            return (sa.x * sa.y * sa.z).CompareTo(sb.x * sb.y * sb.z);
        });
        return list.ToArray();
    }

    public readonly struct KlotzTypeSet : IEnumerable<KlotzType>
    {
        private readonly ulong _value;

        public static readonly KlotzTypeSet Empty = new();

        public KlotzTypeSet(ulong value) { _value = value; }

        public KlotzTypeSet(IEnumerable<KlotzType> types)
        {
            _value = 0;
            foreach (var type in types)
            {
                _value |= 1UL << (int)type;
            }
        }

        public KlotzTypeSet Merge(KlotzTypeSet other)
        {
            return new KlotzTypeSet(_value | other._value);
        }

        public bool Contains(KlotzType type)
        {
            return (_value & 1UL << (int)type) != 0;
        }

        public KlotzTypeSet Remove(KlotzType type)
        {
            return new(_value & ~(1UL << (int)type));
        }

        public bool ContainsOnly(KlotzTypeSet other)
        {
            return (_value & ~other._value) == 0;
        }

        public int Count
        {
            get { return CountSetBits(_value); }
        }

        public readonly void GetHighest(out KlotzType highest, out KlotzType? secondHighest)
        {
            int highestBit = GetHighestSetBitPosition(_value);
            if (highestBit == -1)
                throw new InvalidOperationException("No bits set in PossibleTypes (Collapse)");

            highest = (KlotzType)highestBit;

            // Unset the highest set bit
            ulong remainingBits = _value & ~(1UL << highestBit);

            // Find the new highest set bit, which is the second highest in the original bit-field
            int secondHighestBit = GetHighestSetBitPosition(remainingBits);
            if (secondHighestBit == -1)
            {
                secondHighest = null;
            }
            else
            {
                secondHighest = (KlotzType)secondHighestBit;
            }
        }

        private static int CountSetBits(ulong bitField)
        {
            int count = 0;
            while (bitField != 0)
            {
                bitField &= (bitField - 1); // Clear the least significant bit set
                count++;
            }
            return count;
        }

        private static IEnumerable<int> GetSetBitPositions(ulong bitField)
        {
            int position = 0;
            while (bitField != 0)
            {
                if ((bitField & 1) != 0)
                {
                    yield return position;
                }
                bitField >>= 1;
                position++;
            }
        }

        private static int GetHighestSetBitPosition(ulong bitField)
        {
            for (int i = 63; i >= 0; i--)
            {
                if ((bitField & (1UL << i)) != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        // Explicit cast to int
        public static explicit operator ulong(KlotzTypeSet variant) { return variant._value; }

        // Explicit cast from uint
        public static explicit operator KlotzTypeSet(ulong value) { return new KlotzTypeSet(value); }

        IEnumerator<KlotzType> IEnumerable<KlotzType>.GetEnumerator()
        {
            foreach (int bitPosition in GetSetBitPositions(_value))
            {
                yield return (KlotzType)bitPosition;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KlotzType>)this).GetEnumerator();
        }
    }

    public struct SubKlotzVoxelSuperPosition
    {
        private readonly GeneralVoxelType _generalType;
        private KlotzTypeSet _possibleTypes;
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

        public readonly KlotzTypeSet PossibleTypes => _possibleTypes;

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

        rootVoxel.PossibleTypes.GetHighest(out KlotzType highest, out KlotzType? secondHighest);
        KlotzType type;

        if (secondHighest.HasValue)
        {
            // 50:50 chance of the last 2 items in list
            type = _random.Next() % 2 == 0 ? highest : secondHighest.Value;
        }
        else
        {
            type = highest;
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
