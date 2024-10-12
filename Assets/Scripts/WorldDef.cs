using System.Linq;
using UnityEngine;

/// <summary>
/// The current chunk size is 32x80x32
/// 
/// Factorization:
/// fact 32: 1, 2, 4, -, 8, --, 16, --, 32, --, --
/// fact 80: 1, 2, 4, 5, 8, 10, 16, 20, --, 40, 80
/// common:  1, 2, 4, -, 8, --, 16, --, --, --, --
/// 
/// 32*32*80 = 81.920 voxels per chunk
/// </summary>
public static class WorldDef
{
    /// <summary>
    /// Word limits. Numbers are inclusive.
    /// </summary>
    public static class Limits
    {
        public static int MinCoordsX = -10;
        public static int MaxCoordsX = 10;
        public static int MinCoordsY = -2;
        public static int MaxCoordsY = 2;
        public static int MinCoordsZ = -10;
        public static int MaxCoordsZ = 10;
    }

    /// <summary>
    /// Where all the sizing is based on
    /// </summary>
    private static class Fundamentals
    {
        public const float P = 0.008f;
        public const float h = 0.0032f;
        public const float ScaleInv = 45;
    }

    /// <summary>
    /// This is calculated from constants.
    /// 
    /// X/Z: P * ScaleInv = 0.0080 * 45 = 0.360
    /// Y:   h * ScaleInv = 0.0032 * 45 = 0.144
    /// 
    /// So the result is { 0.36, 0.144, 0.36 }
    /// </summary>
    public static readonly Vector3 SubKlotzSize = new(
        Fundamentals.P * Fundamentals.ScaleInv,
        Fundamentals.h * Fundamentals.ScaleInv,
        Fundamentals.P * Fundamentals.ScaleInv);

    /// <summary>
    /// 
    /// </summary>
    public const int ChunkSubDivsX = 32;

    /// <summary>
    /// 
    /// </summary>
    public const int ChunkSubDivsY = 80;

    /// <summary>
    /// 
    /// </summary>
    public const int ChunkSubDivsZ = 32;

    /// <summary>
    /// 
    /// </summary>
    public static readonly Vector3Int ChunkSubDivs = new(ChunkSubDivsX, ChunkSubDivsY, ChunkSubDivsZ);

    /// <summary>
    /// 32 * 0.36 = 80 * 0.144 = 11,52
    /// </summary>
    public static readonly Vector3 ChunkSize = new(SubKlotzSize.x * ChunkSubDivsX, SubKlotzSize.y * ChunkSubDivsY, SubKlotzSize.z * ChunkSubDivsZ);

    /// <summary>
    /// Lod (level of detail) is a number from 0..4
    /// Lod 0 ->  1 sub-klotz packing
    /// Lod 1 ->  2 sub-klotz packing
    /// Lod 2 ->  4 sub-klotz packing
    /// Lod 3 ->  8 sub-klotz packing
    /// Lod 4 -> 16 sub-klotz packing
    /// </summary>
    public const int MaxLodValue = 4;

    /// <summary>
    /// Helper class
    /// </summary>
    public struct LevelOfDetailSetting
    {
        public int LevelOfDetail;
        public int MaxThreshold;
    }

    /// <summary>
    /// Numbers are inclusive.
    /// </summary>
    public static readonly LevelOfDetailSetting[] DetailLevels =
    {
         new() { LevelOfDetail = 0, MaxThreshold = 2, }, // 4
         new() { LevelOfDetail = 1, MaxThreshold = 4, }, // 8
         new() { LevelOfDetail = 2, MaxThreshold = 8, }, // 12
         new() { LevelOfDetail = 2, MaxThreshold = 10, }, // 16
         new() { LevelOfDetail = -1, MaxThreshold = 11, }, // world load distance
    };

    /// <summary>
    /// Max threshold from <c>DetailLevels</c>
    /// </summary>
    public static int ChunkLoadDistance { get { return DetailLevels.Last().MaxThreshold; } }
}
