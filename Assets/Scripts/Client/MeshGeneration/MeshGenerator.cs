using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Clotzbergh.Client.MeshGeneration
{
    /// <summary>
    /// Generates the meshes. When called on <c>GenerateTerrainMesh</c> it generates
    /// the mesh for a <c>ClientChunk</c> and its inner <c>WorldChunk</c>.
    /// Uses the neighbors of the <c>ClientChunk</c> to find adjacent world
    /// information to draw the mesh correctly.
    /// For overlapping Klotzes the general rule is that the chunk with the root
    /// <c>SubKlotz</c> owns the Klotz (that is the <c>SubKlotz</c> with the sub-
    /// coords {0,0,0}).
    /// </summary>
    public static class MeshGenerator
    {
        private static long _meshGenerationCount = 0;

        private static readonly Dictionary<KlotzType, NonPrimitiveKlotzMesh> _nonPrimitiveMeshes = new();

        public static bool DoStudsAndHoles { get; set; } = true;

        public static long MeshGenerationCount { get => Interlocked.Read(ref _meshGenerationCount); }

        /// <summary>
        /// Registers the mesh to use for a non-primitive <c>KlotzType</c> (e.g. Door1x4).
        /// Must be called from the main thread, before any mesh generation for that type is
        /// requested, since it reads the Unity <c>Mesh</c> API which is not thread-safe.
        /// </summary>
        public static void RegisterNonPrimitiveMesh(KlotzType type, Mesh mesh)
        {
            _nonPrimitiveMeshes[type] = new NonPrimitiveKlotzMesh(mesh);
        }

        /// <summary>
        /// 
        /// </summary>
        public static VoxelMeshBuilder GenerateTerrainMesh(ClientChunk chunk, int lod, KlotzRegion cutout = null)
        {
            if (lod < 0 || lod > 4)
                throw new ArgumentOutOfRangeException("lod", "lod must be 0 to 4");

            WorldChunk worldChunk = chunk.World;
            if (worldChunk == null)
                return null;

            // int lodSkip = 1 << lod; // 1, 2, 4, 8, or 16
            WorldReader reader = new(chunk, cutout);
            VoxelMeshBuilder builder = new(WorldDef.ChunkSize, WorldDef.ChunkSubDivs);

            for (int z = 0; z < WorldDef.ChunkSubDivsZ; z++)
            {
                for (int y = 0; y < WorldDef.ChunkSubDivsY; y++)
                {
                    for (int x = 0; x < WorldDef.ChunkSubDivsX; x++)
                    {
                        reader.MoveTo(x, y, z);

                        bool isRoot = reader.IsRoot;
                        bool exposed = reader.IsExposed;

                        // A non-root cell that isn't exposed can never contribute anything (this
                        // also covers all non-root cells of a non-primitive klotz, since those
                        // are never opaque/exposed) - skip it without resolving its root, which
                        // is the expensive part of RootSubKlotz.
                        if (!isRoot && !exposed)
                            continue;

                        SubKlotz? kRoot = reader.RootSubKlotz; // free when isRoot, only resolves here when exposed
                        if (!kRoot.HasValue)
                            continue; // can't access the root sub-klotz

                        KlotzType type = kRoot.Value.Type;
                        bool isPrimitive = KlotzKB.IsPrimitive(type);

                        if (!isPrimitive)
                        {
                            // isRoot is guaranteed true here: non-root cells of non-primitive
                            // klotzes are never exposed, so they were already filtered above.
                            if (_nonPrimitiveMeshes.TryGetValue(type, out NonPrimitiveKlotzMesh template))
                            {
                                builder.MoveTo(x, y, z);
                                builder.SetColor(kRoot.Value.Color);
                                builder.SetVariant(kRoot.Value.Variant);
                                builder.AddNonPrimitiveKlotz(template, kRoot.Value.Direction);
                            }
                            continue;
                        }

                        if (!exposed) // root cell that wasn't pre-filtered above, but isn't exposed either
                            continue;

                        builder.MoveTo(x, y, z);
                        builder.SetColor(kRoot.Value.Color);
                        builder.SetVariant(kRoot.Value.Variant);

                        KlotzSideFlags topFlags = 0;
                        KlotzSideFlags bottomFlags = 0;

                        if (lod == 0 && DoStudsAndHoles)
                        {
                            if (KlotzKB.TypeHasTopStuds(type))
                                topFlags |= KlotzSideFlags.HasStuds;
                            if (KlotzKB.TypeHasBottomHoles(type))
                                bottomFlags |= KlotzSideFlags.HasHoles;
                        }

                        if (reader.IsExposedXM1) builder.AddLeftFace();
                        if (reader.IsExposedXP1) builder.AddRightFace();
                        if (reader.IsExposedYM1) builder.AddBottomFace(bottomFlags);
                        if (reader.IsExposedYP1) builder.AddTopFace(topFlags);
                        if (reader.IsExposedZM1) builder.AddBackFace();
                        if (reader.IsExposedZP1) builder.AddFrontFace();
                    }
                }
            }

            Interlocked.Increment(ref _meshGenerationCount);
            return builder;
        }
    }
}
