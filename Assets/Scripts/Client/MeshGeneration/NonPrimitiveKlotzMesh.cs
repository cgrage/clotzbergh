using System;
using UnityEngine;

namespace Clotzbergh.Client.MeshGeneration
{
    /// <summary>
    /// Raw vertex/normal/triangle data for a non-primitive <c>KlotzType</c> (e.g. DoorFrame1x4),
    /// extracted once from an imported <c>Mesh</c> asset on the main thread so that mesh
    /// generation on background threads never touches Unity's <c>Mesh</c> API directly.
    /// Vertex positions are expected in the "ToPosX" orientation, relative to the min corner
    /// of the klotz's bounding box (i.e. spanning from (0,0,0) to its world-space size).
    /// </summary>
    public readonly struct NonPrimitiveKlotzMesh
    {
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly int[] Triangles;
        public readonly KlotzSurfaceFeature[] VertexFeatures;

        /// <summary>
        /// <paramref name="materials"/> must be the source renderer's <c>sharedMaterials</c>, in
        /// submesh order: each submesh's <see cref="KlotzSurfaceFeature"/> is resolved by parsing
        /// materials[i].name (e.g. "HasStuds"), since a <c>Mesh</c> alone carries no material
        /// reference and Unity's FBX import doesn't preserve submesh order reliably enough to
        /// infer the type from the index alone.
        /// </summary>
        public NonPrimitiveKlotzMesh(Mesh mesh, Material[] materials)
        {
            Vertices = mesh.vertices;
            Normals = mesh.normals;
            Triangles = mesh.triangles;

            VertexFeatures = new KlotzSurfaceFeature[Vertices.Length];
            for (int subMesh = 0; subMesh < mesh.subMeshCount && subMesh < materials.Length; subMesh++)
            {
                string materialName = materials[subMesh] != null ? materials[subMesh].name : null;
                if (!Enum.TryParse(materialName, ignoreCase: true, out KlotzSurfaceFeature feature))
                    feature = KlotzSurfaceFeature.Default;

                if (feature == KlotzSurfaceFeature.Default)
                    continue;

                foreach (int vertexIndex in mesh.GetTriangles(subMesh))
                {
                    VertexFeatures[vertexIndex] = feature;
                }
            }
        }
    }
}
