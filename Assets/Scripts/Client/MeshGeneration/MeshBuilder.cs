using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Client.MeshGeneration
{
    public class MeshBuilder
    {
        public List<Vector3> Vertices { get; private set; }
        public List<int> Triangles { get; private set; }
        public List<Vector2> UvData { get; private set; }

        public MeshBuilder(int estimatedVertexCount = 0, int estimatedTriangleCount = 0)
        {
            Vertices = new(estimatedVertexCount);
            Triangles = new List<int>(capacity: estimatedTriangleCount * 3);
            UvData = new List<Vector2>(estimatedVertexCount);
        }

        public MeshBuilder(Vector3[] vertices, int[] triangles, Vector2[] uvData)
        {
            Vertices = new(vertices);
            Triangles = new(triangles);
            UvData = new(uvData);
        }

        public static Vector2 BuildVertexUvData(KlotzSide side, KlotzVertexFlags flags, KlotzColor color, KlotzVariant variant)
        {
            float x = (((uint)color) << 3) | ((uint)side);
            float y = (((uint)flags) << 7) | (uint)variant;
            return new Vector2(x, y);
        }

        public void AddTriangle(int a, int b, int c)
        {
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new()
            {
                vertices = Vertices.ToArray(),
                triangles = Triangles.ToArray(),
                uv = UvData.ToArray(),
            };

            return mesh;
        }
    }
}
