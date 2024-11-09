using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class WorldGenerator
{
    protected HeightMap HeightMap { get; } = new();

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

    protected HeightMap HeightMap { get; private set; }

    public ChunkGenerator(Vector3Int chunkCoords, HeightMap heightMap)
    {
        lock (RandomCreationLock) { Random = new(); }
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
    public MicroBlockWorldGenerator(Vector3Int chunkCoords, HeightMap heightMap)
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
                        chunk.Set(ix, iy, iz, new SubKlotz(KlotzType.Air, 0, KlotzVariant.Zero, 0));
                    }
                    else
                    {
                        chunk.Set(ix, iy, iz, new SubKlotz(KlotzType.Plate1x1, KlotzColor.White, NextRandVariant(), KlotzDirection.ToPosX));
                    }
                }
            }
        }

        chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 16), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 18), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 16), KlotzDirection.ToPosZ);
        chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(13, 39, 16), KlotzDirection.ToPosZ);
        chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 15), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(15, 39, 13), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(16, 39, 15), KlotzDirection.ToNegZ);
        chunk.PlaceKlotz(KlotzType.Brick2x4, NextRandColor(), NextRandVariant(), new Vector3Int(18, 39, 15), KlotzDirection.ToNegZ);

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
        public List<KlotzType> PossibleTypes { get; private set; } = new();
        public SubKlotz? CollapsedType = null;

        public SubKlotzVoxelSuperPosition(GeneralVoxelType generalType)
        {
            GeneralType = generalType;

            if (generalType == GeneralVoxelType.Air)
            {
                CollapsedType = new SubKlotz(KlotzType.Air, 0, KlotzVariant.Zero, 0);
            }
            else
            {
                if (generalType == GeneralVoxelType.AirOrGround)
                    PossibleTypes.Add(KlotzType.Air);

                PossibleTypes.AddRange(KlotzKB.AllGroundTypes);
            }
        }

        public bool IsAir { get { return CollapsedType?.Type == KlotzType.Air; } }

        public bool IsFreeGround { get { return !CollapsedType.HasValue && GeneralType != GeneralVoxelType.Air; } }

        public bool IsCollapsed { get { return CollapsedType.HasValue; } }
    }

    public WaveFunctionCollapseGenerator(Vector3Int chunkCoords, HeightMap heightMap)
        : base(chunkCoords, heightMap)
    {
        _positions = new SubKlotzVoxelSuperPosition[
            WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
        _nonCollapsed = new();
    }

    public override WorldChunk Generate()
    {
        Initialize();
        RecalculateSuperpositions(Vector3Int.zero, WorldDef.ChunkSubDivs);

        while (_nonCollapsed.Count > 0)
        {
            Vector3Int coords = _nonCollapsed[Random.Next(0, _nonCollapsed.Count)];
            Collapse(coords);
            RecalculateSuperpositions(coords - new Vector3Int(-3, -3, -3), new(7, 7, 7));
        }

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

    public void Initialize()
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

    public void RecalculateSuperpositions(Vector3Int from, Vector3Int size)
    {
        for (int iz = from.z; iz < from.z + size.z; iz++)
        {
            for (int iy = from.y; iy < from.y + size.y; iy++)
            {
                for (int ix = from.x; ix < from.x + size.x; ix++)
                {
                    RecalculateSuperpositions(ix, iy, iz);
                }
            }
        }
    }

    public void RecalculateSuperpositions(int x, int y, int z)
    {
        if (IsOutOfBounds(x, y, z))
            return;

        SubKlotzVoxelSuperPosition voxel = _positions[x, y, z];
        if (voxel.IsCollapsed)
            return;

        foreach (var type in voxel.PossibleTypes.ToArray())
        {
            KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
            if (!IsPossible(x, y, z, type, dir))
            {
                voxel.PossibleTypes.Remove(type);
            }
        }

        if (voxel.PossibleTypes.Count <= 2)
        {
            Collapse(new(x, y, z));
        }
    }

    public bool IsPossible(int x, int y, int z, KlotzType type, KlotzDirection dir)
    {
        Vector3Int size = KlotzKB.KlotzSize(type);

        for (int subZ = 0; subZ < size.z; subZ++)
        {
            for (int subX = 0; subX < size.x; subX++)
            {
                for (int subY = 0; subY < size.y; subY++)
                {
                    Vector3Int root = new(x, y, z);
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
            return;

        KlotzType type;

        if (rootVoxel.PossibleTypes.Count == 1)
        {
            type = rootVoxel.PossibleTypes[0];
        }
        else
        {
            int index = Random.Next(rootVoxel.PossibleTypes.Count - 2, rootVoxel.PossibleTypes.Count); // 50:50 chance
            type = rootVoxel.PossibleTypes[index];
        }

        if (type == KlotzType.Air)
        {
            rootVoxel.CollapsedType = new SubKlotz(type, 0, KlotzVariant.Zero, 0);
            _nonCollapsed.Remove(rootCoords);
        }
        else
        {
            KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
            Vector3Int size = KlotzKB.KlotzSize(type);
            KlotzColor color = NextRandColor();
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

                        if (subX == 0 && subY == 0 && subZ == 0)
                        {
                            voxel.CollapsedType = new SubKlotz(type, color, variant, dir);
                        }
                        else
                        {
                            voxel.CollapsedType = new SubKlotz(type, dir, subX, subY, subZ);
                        }

                        _nonCollapsed.Remove(coords);
                    }
                }
            }
        }
    }
}
