using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class WorldGenerator
{
    protected IHeightMap HeightMap { get; } = new DefaultHeightMap();

    public WorldChunk GetChunk(Vector3Int chunkCoords)
    {
        ChunkGenerator gen = new MicroBlockWorldGenerator(chunkCoords, HeightMap);
        // ChunkGenerator gen = new WaveFunctionCollapseGenerator(chunkCoords, HeightMap);

        return gen.Generate();
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

    protected Random Random { get; private set; }

    protected Vector3Int ChunkCoords { get; private set; }

    protected IHeightMap HeightMap { get; private set; }

    public ChunkGenerator(Vector3Int chunkCoords, IHeightMap heightMap)
    {
        lock (RandomCreationLock) { Random = new(chunkCoords.x + chunkCoords.y * 1000 + chunkCoords.z * 1000000); }
        ChunkCoords = chunkCoords;
        HeightMap = heightMap;
    }

    public abstract WorldChunk Generate();

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
        return (KlotzColor)Random.Next(0, (int)KlotzColor.NextFree);
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
        return (KlotzVariant)(uint)Random.Next(0, KlotzVariant.MaxValue + 1);
    }

}

public class MicroBlockWorldGenerator : ChunkGenerator
{
    public MicroBlockWorldGenerator(Vector3Int chunkCoords, IHeightMap heightMap)
        : base(chunkCoords, heightMap) { }

    public override WorldChunk Generate()
    {
        WorldChunk chunk = new();

        for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
        {
            for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
            {
                int x = ChunkCoords.x * WorldDef.ChunkSubDivsX + ix;
                int z = ChunkCoords.z * WorldDef.ChunkSubDivsZ + iz;
                int groundStart = Mathf.RoundToInt(HeightMap.At(x, z) / WorldDef.SubKlotzSize.y);

                for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                {
                    int y = ChunkCoords.y * WorldDef.ChunkSubDivsY + iy;
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
    private readonly SubKlotzVoxelSuperPosition[,,] _positions;
    private readonly List<Vector3Int> _nonCollapsed;

    public enum GeneralVoxelType
    {
        Air, Ground, AirOrGround
    }

    public class SubKlotzVoxelSuperPosition
    {
        public GeneralVoxelType GeneralType { get; private set; }
        public ulong PossibleTypes { get; set; }
        public SubKlotz? CollapsedType = null;

        public static ulong AllGroundTypes =
            1UL << (int)KlotzType.Plate1x1 |
            1UL << (int)KlotzType.Plate1x2 |
            1UL << (int)KlotzType.Plate1x3 |
            1UL << (int)KlotzType.Plate1x4 |
            1UL << (int)KlotzType.Plate1x6 |
            1UL << (int)KlotzType.Plate1x8 |
            1UL << (int)KlotzType.Plate2x2 |
            1UL << (int)KlotzType.Plate2x3 |
            1UL << (int)KlotzType.Plate2x4 |
            1UL << (int)KlotzType.Plate2x6 |
            1UL << (int)KlotzType.Plate2x8 |
            1UL << (int)KlotzType.Plate4x4 |
            1UL << (int)KlotzType.Plate4x6 |
            1UL << (int)KlotzType.Plate4x8 |
            1UL << (int)KlotzType.Plate6x6 |
            1UL << (int)KlotzType.Plate6x8 |
            1UL << (int)KlotzType.Plate8x8 |
            1UL << (int)KlotzType.Brick1x1 |
            1UL << (int)KlotzType.Brick1x2 |
            1UL << (int)KlotzType.Brick1x3 |
            1UL << (int)KlotzType.Brick1x4 |
            1UL << (int)KlotzType.Brick1x6 |
            1UL << (int)KlotzType.Brick1x8 |
            1UL << (int)KlotzType.Brick2x2 |
            1UL << (int)KlotzType.Brick2x3 |
            1UL << (int)KlotzType.Brick2x4 |
            1UL << (int)KlotzType.Brick2x6 |
            1UL << (int)KlotzType.Brick2x8 |
            1UL << (int)KlotzType.Brick4x6;

        public SubKlotzVoxelSuperPosition(GeneralVoxelType generalType)
        {
            GeneralType = generalType;

            if (generalType == GeneralVoxelType.Air)
            {
                CollapsedType = SubKlotz.Air;
            }
            else
            {
                if (generalType == GeneralVoxelType.AirOrGround)
                    PossibleTypes |= 1 << ((int)KlotzType.Air);

                PossibleTypes |= AllGroundTypes;
            }
        }

        public bool IsAir { get { return CollapsedType?.Type == KlotzType.Air; } }

        public bool IsFreeGround { get { return !CollapsedType.HasValue && GeneralType != GeneralVoxelType.Air; } }

        public bool IsCollapsed { get { return CollapsedType.HasValue; } }

        public override string ToString()
        {
            if (CollapsedType.HasValue) { return $"Collapsed to: {CollapsedType.Value}"; }
            else { return $"SuperPos of {CountSetBits(PossibleTypes)}"; }
        }
    }

    public static int CountSetBits(ulong bitField)
    {
        int count = 0;
        while (bitField != 0)
        {
            bitField &= (bitField - 1); // Clear the least significant bit set
            count++;
        }
        return count;
    }

    static IEnumerable<int> GetSetBitPositions(ulong bitField)
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

    public static int GetHighestSetBitPosition(ulong bitField)
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

    public WaveFunctionCollapseGenerator(Vector3Int chunkCoords, IHeightMap heightMap)
        : base(chunkCoords, heightMap)
    {
        _positions = new SubKlotzVoxelSuperPosition[
            WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
        _nonCollapsed = new();
    }

    public override WorldChunk Generate()
    {
        Initialize();
        RecalculateSuperpositionsInRange(Vector3Int.zero, WorldDef.ChunkSubDivs);

        while (_nonCollapsed.Count > 0)
        {
            Vector3Int coords = _nonCollapsed[Random.Next(0, _nonCollapsed.Count)];
            SubKlotz newRoot = Collapse(coords);

            RecalculateSuperpositionsAffectedBy(coords, newRoot);
        }

        return ToWorldChunk();
    }

    private void Initialize()
    {
        for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
        {
            for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
            {
                int x = ChunkCoords.x * WorldDef.ChunkSubDivsX + ix;
                int z = ChunkCoords.z * WorldDef.ChunkSubDivsZ + iz;
                int groundStart = Mathf.RoundToInt(HeightMap.At(x, z) / WorldDef.SubKlotzSize.y);

                for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                {
                    int y = ChunkCoords.y * WorldDef.ChunkSubDivsY + iy;
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

    private void RecalculateSuperpositionsInRange(Vector3Int from, Vector3Int size)
    {
        for (int iz = from.z; iz < from.z + size.z; iz++)
        {
            for (int iy = from.y; iy < from.y + size.y; iy++)
            {
                for (int ix = from.x; ix < from.x + size.x; ix++)
                {
                    RecalculateSuperpositionsOfPos(ix, iy, iz);
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

        foreach (int bitPosition in GetSetBitPositions(voxel.PossibleTypes))
        {
            KlotzType type = (KlotzType)bitPosition;
            KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
            if (!IsPossible(x, y, z, type, dir))
            {
                voxel.PossibleTypes &= ~(1UL << bitPosition);
            }
        }
    }

    private void RecalculateSuperpositionsAffectedBy(Vector3Int coords, SubKlotz subKlotz)
    {
        // TODO: Improve here
        // Vector3Int size = KlotzKB.KlotzSize(subKlotz.Type);
        // int minXR8 = coords.x - 7;

        const int dMinXZ = KlotzKB.MaxKlotzSizeXZ - 1;
        const int dMinY = KlotzKB.MaxKlotzSizeY - 1;
        const int dRangeXZ = KlotzKB.MaxKlotzSizeXZ * 2 - 1;
        const int dRangeY = KlotzKB.MaxKlotzSizeY * 2 - 1;
        RecalculateSuperpositionsInRange(coords - new Vector3Int(dMinXZ, dMinY, dMinXZ),
            new(dRangeXZ, dRangeY, dRangeXZ));
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

    private SubKlotz Collapse(Vector3Int rootCoords)
    {
        SubKlotzVoxelSuperPosition rootVoxel = _positions[rootCoords.x, rootCoords.y, rootCoords.z];
        if (rootVoxel.IsCollapsed)
            throw new InvalidOperationException("Already collapsed (Collapse)");

        int highestBit = GetHighestSetBitPosition(rootVoxel.PossibleTypes);
        if (highestBit == -1)
            throw new InvalidOperationException("No bits set in PossibleTypes (Collapse)");

        // Unset the highest set bit
        rootVoxel.PossibleTypes &= ~(1UL << highestBit);

        // Find the new highest set bit, which is the second highest in the original bit-field
        int secondHighestBit = GetHighestSetBitPosition(rootVoxel.PossibleTypes);

        KlotzType type;

        if (secondHighestBit == -1)
        {
            type = (KlotzType)highestBit;
        }
        else
        {
            // 50:50 chance of the last 2 items in list
            int bitPos = Random.Next() % 2 == 0 ? highestBit : secondHighestBit;
            type = (KlotzType)bitPos;
        }

        if (type == KlotzType.Air)
        {
            rootVoxel.CollapsedType = SubKlotz.Air;
            _nonCollapsed.Remove(rootCoords);
        }
        else
        {
            PlaceKlotzInRandDirection(rootCoords, type);
        }

        return rootVoxel.CollapsedType.Value;
    }

    void PlaceKlotzInRandDirection(Vector3Int rootCoords, KlotzType type)
    {
        KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
        Vector3Int size = KlotzKB.KlotzSize(type);
        KlotzColor color = ColorFromHeight(ChunkCoords.y * WorldDef.ChunkSubDivsY + rootCoords.y);
        KlotzVariant variant = NextRandVariant();

        for (int subZ = 0; subZ < size.z; subZ++)
        {
            for (int subX = 0; subX < size.x; subX++)
            {
                for (int subY = 0; subY < size.y; subY++)
                {
                    Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
                        rootCoords, new(subX, subY, subZ), dir);

                    SubKlotzVoxelSuperPosition voxel = _positions[coords.x, coords.y, coords.z];

                    // safety-check
                    // if (voxel.IsCollapsed)
                    // {
                    //     bool possible = IsPossible(rootCoords.x, rootCoords.y, rootCoords.z, type, dir);
                    //     throw new InvalidOperationException(
                    //         $"Cannot place {type} at {rootCoords} because {coords} is already used by {voxel}. IsPossible returned {possible}");
                    // }

                    if (subX == 0 && subY == 0 && subZ == 0)
                    {
                        voxel.CollapsedType = SubKlotz.Root(type, color, variant, dir);
                    }
                    else
                    {
                        voxel.CollapsedType = SubKlotz.NonRoot(type, dir, subX, subY, subZ);
                    }

                    _nonCollapsed.Remove(coords);
                }
            }
        }
    }

    private WorldChunk ToWorldChunk()
    {
        WorldChunk chunk = new();

        for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
        {
            for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
            {
                for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
                {
                    chunk.Set(ix, iy, iz, _positions[ix, iy, iz].CollapsedType.Value);
                }
            }
        }

        return chunk;
    }
}
