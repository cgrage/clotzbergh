using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public abstract class WorldGenerator
{
    protected Random Random { get; private set; }

    protected HeightMap HeightMap { get; private set; }

    public abstract WorldChunk GetChunk(Vector3Int chunkCoords);

    public static WorldGenerator Default { get; private set; } = new MicroBlockWorldGenerator();
    //public static WorldGenerator Default { get; private set; } = new WaveFunctionCollapseGenerator();

    protected WorldGenerator()
    {
        Random = new(0);
        HeightMap = new();
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
}

public class MicroBlockWorldGenerator : WorldGenerator
{
    public override WorldChunk GetChunk(Vector3Int chunkCoords)
    {
        var chunk = WorldChunk.CreateEmpty();

        for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
        {
            for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
            {
                int x = chunkCoords.x * WorldDef.ChunkSubDivsX + ix;
                int z = chunkCoords.z * WorldDef.ChunkSubDivsZ + iz;
                int groundStart = Mathf.RoundToInt(HeightMap.At(x, z) / WorldDef.SubKlotzSize.y);

                for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                {
                    int y = chunkCoords.y * WorldDef.ChunkSubDivsY + iy;
                    if (y > groundStart)
                    {
                        chunk.Set(ix, iy, iz, new SubKlotz(KlotzType.Air, KlotzColor.Green, KlotzDirection.ToPosX, 0, 0, 0));
                    }
                    else
                    {
                        chunk.Set(ix, iy, iz, new SubKlotz(KlotzType.Plate1x1, KlotzColor.Green, KlotzDirection.ToPosX, 0, 0, 0));
                    }
                }
            }
        }

        chunk.PlaceKlotz(KlotzType.Brick2x4, KlotzColor.Green, new Vector3Int(16, 39, 16), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick2x4, KlotzColor.Green, new Vector3Int(16, 39, 18), KlotzDirection.ToPosX);
        chunk.PlaceKlotz(KlotzType.Brick2x4, KlotzColor.Green, new Vector3Int(15, 39, 16), KlotzDirection.ToPosZ);
        chunk.PlaceKlotz(KlotzType.Brick2x4, KlotzColor.Green, new Vector3Int(13, 39, 16), KlotzDirection.ToPosZ);
        chunk.PlaceKlotz(KlotzType.Brick2x4, KlotzColor.Green, new Vector3Int(15, 39, 15), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick2x4, KlotzColor.Green, new Vector3Int(15, 39, 13), KlotzDirection.ToNegX);
        chunk.PlaceKlotz(KlotzType.Brick2x4, KlotzColor.Green, new Vector3Int(16, 39, 15), KlotzDirection.ToNegZ);
        chunk.PlaceKlotz(KlotzType.Brick2x4, KlotzColor.Green, new Vector3Int(18, 39, 15), KlotzDirection.ToNegZ);

        return chunk;
    }
}

public class WaveFunctionCollapseGenerator : WorldGenerator
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
                CollapsedType = new SubKlotz(KlotzType.Air, 0, 0, 0, 0, 0);
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

    public WaveFunctionCollapseGenerator()
    {
        _positions = new SubKlotzVoxelSuperPosition[
            WorldDef.ChunkSubDivsX, WorldDef.ChunkSubDivsY, WorldDef.ChunkSubDivsZ];
        _nonCollapsed = new();
    }

    public override WorldChunk GetChunk(Vector3Int chunkCoords)
    {
        Initialize(chunkCoords);
        RecalculateSuperpositions(Vector3Int.zero, WorldDef.ChunkSubDivs);

        while (_nonCollapsed.Count > 0)
        {
            Vector3Int coords = _nonCollapsed[Random.Next(0, _nonCollapsed.Count)];
            Collapse(coords);
            RecalculateSuperpositions(coords - new Vector3Int(-3, -3, -3), new(7, 7, 7));
        }

        WorldChunk chunk = WorldChunk.CreateEmpty();
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

    public void Initialize(Vector3Int chunkCoords)
    {
        for (int iz = 0; iz < WorldDef.ChunkSubDivsZ; iz++)
        {
            for (int ix = 0; ix < WorldDef.ChunkSubDivsX; ix++)
            {
                int x = chunkCoords.x * WorldDef.ChunkSubDivsX + ix;
                int z = chunkCoords.z * WorldDef.ChunkSubDivsZ + iz;
                int groundStart = Mathf.RoundToInt(HeightMap.At(x, z) / WorldDef.SubKlotzSize.y);

                for (int iy = 0; iy < WorldDef.ChunkSubDivsY; iy++)
                {
                    int y = chunkCoords.y * WorldDef.ChunkSubDivsY + iy;
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
            rootVoxel.CollapsedType = new SubKlotz(type, 0, 0, 0, 0, 0);
            _nonCollapsed.Remove(rootCoords);
        }
        else
        {
            KlotzDirection dir = KlotzDirection.ToPosX; // TODO: All other directions
            Vector3Int size = KlotzKB.KlotzSize(type);

            for (int subZ = 0; subZ < size.z; subZ++)
            {
                for (int subX = 0; subX < size.x; subX++)
                {
                    for (int subY = 0; subY < size.y; subY++)
                    {
                        Vector3Int coords = SubKlotz.TranslateSubIndexToCoords(
                            rootCoords, new(subX, subY, subZ), dir);

                        SubKlotzVoxelSuperPosition voxel = _positions[coords.x, coords.y, coords.z];
                        voxel.CollapsedType = new SubKlotz(type, KlotzColor.Blue, dir, subX, subY, subZ);

                        _nonCollapsed.Remove(coords);
                    }
                }
            }
        }
    }
}
