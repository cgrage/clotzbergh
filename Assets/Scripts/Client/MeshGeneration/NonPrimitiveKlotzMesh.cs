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

        public NonPrimitiveKlotzMesh(Mesh mesh)
        {
            Vertices = mesh.vertices;
            Normals = mesh.normals;
            Triangles = mesh.triangles;
        }
    }
}
