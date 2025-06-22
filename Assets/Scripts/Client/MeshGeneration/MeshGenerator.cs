using System;
using System.Collections.Generic;
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
        public static bool DoStudsAndHoles { get; set; } = true;

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

            int lodSkip = 1 << lod; // 1, 2, 4, 8, or 16
            WorldReader reader = new(chunk, lodSkip, cutout);
            VoxelMeshBuilder builder = new(WorldDef.ChunkSize, WorldDef.ChunkSubDivs / lodSkip);

            for (int z = 0, zi = 0; z < WorldDef.ChunkSubDivsZ; z += lodSkip, zi++)
            {
                for (int y = 0, yi = 0; y < WorldDef.ChunkSubDivsY; y += lodSkip, yi++)
                {
                    for (int x = 0, xi = 0; x < WorldDef.ChunkSubDivsX; x += lodSkip, xi++)
                    {
                        reader.MoveTo(x, y, z);
                        if (!reader.IsExposed)
                            continue;

                        SubKlotz? kRoot = reader.RootSubKlotz;
                        if (!kRoot.HasValue)
                            continue; // can't access the root sub-klotz

                        KlotzType type = kRoot.Value.Type;
                        builder.MoveTo(xi, yi, zi);
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

            return builder;
        }
    }
}
