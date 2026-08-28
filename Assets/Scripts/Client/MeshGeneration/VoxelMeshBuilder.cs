using System.Collections.Generic;
using UnityEngine;

namespace Clotzbergh.Client.MeshGeneration
{
    public class VoxelMeshBuilder : MeshBuilder
    {
        private readonly Vector3 _segmentSize;

        private KlotzColor _color;

        private KlotzVariant _variant;

        private float _x1, _x2, _y1, _y2, _z1, _z2;

        private Vector3Int _currentCoords;

        /// <summary>
        /// Can be used to look-up the voxel coords once you know the triangle index.
        /// </summary>
        public List<Vector3Int> VoxelCoords { get; private set; }

        public VoxelMeshBuilder(Vector3 size, Vector3Int subDivs)
        {
            _segmentSize = new(size.x / subDivs.x, size.y / subDivs.y, size.z / subDivs.z);
            _color = KlotzColor.White;
            _variant = KlotzVariant.Zero;

            VoxelCoords = new();
        }

        public void MoveTo(int x, int y, int z)
        {
            _currentCoords = new(x, y, z);
            _x1 = x * _segmentSize.x;
            _x2 = _x1 + _segmentSize.x;
            _y1 = y * _segmentSize.y;
            _y2 = _y1 + _segmentSize.y;
            _z1 = z * _segmentSize.z;
            _z2 = _z1 + _segmentSize.z;
        }

        public void SetColor(KlotzColor color)
        {
            _color = color;
        }

        public void SetVariant(KlotzVariant variant)
        {
            _variant = variant;
        }

        /// <summary>
        /// A.K.A. the left face
        /// </summary>
        public void AddLeftFace(KlotzSurfaceFeature surface = KlotzSurfaceFeature.Default)
        {
            AddFace(
                new(_x1, _y1, _z2), new(_x1, _y2, _z2), new(_x1, _y2, _z1), new(_x1, _y1, _z1),
                KlotzSide.Left, surface);
        }

        /// <summary>
        /// A.K.A. the right face
        /// </summary>
        public void AddRightFace(KlotzSurfaceFeature surface = KlotzSurfaceFeature.Default)
        {
            AddFace(
                new(_x2, _y1, _z1), new(_x2, _y2, _z1), new(_x2, _y2, _z2), new(_x2, _y1, _z2),
                KlotzSide.Right, surface);
        }

        /// <summary>
        /// A.K.A. the bottom face
        /// </summary>
        public void AddBottomFace(KlotzSurfaceFeature surface = KlotzSurfaceFeature.Default)
        {
            AddFace(
                new(_x2, _y1, _z1), new(_x2, _y1, _z2), new(_x1, _y1, _z2), new(_x1, _y1, _z1),
                KlotzSide.Bottom, surface);
        }

        /// <summary>
        /// A.K.A. the top face
        /// </summary>
        public void AddTopFace(KlotzSurfaceFeature surface = KlotzSurfaceFeature.Default)
        {
            AddFace(
                new(_x1, _y2, _z1), new(_x1, _y2, _z2), new(_x2, _y2, _z2), new(_x2, _y2, _z1),
                KlotzSide.Top, surface);
        }

        /// <summary>
        /// A.K.A. the back face
        /// </summary>
        public void AddBackFace(KlotzSurfaceFeature surface = KlotzSurfaceFeature.Default)
        {
            AddFace(
                new(_x1, _y2, _z1), new(_x2, _y2, _z1), new(_x2, _y1, _z1), new(_x1, _y1, _z1),
                KlotzSide.Back, surface);
        }

        /// <summary>
        /// A.K.A. the front face
        /// </summary>
        public void AddFrontFace(KlotzSurfaceFeature surface = KlotzSurfaceFeature.Default)
        {
            AddFace(
                new(_x1, _y1, _z2), new(_x2, _y1, _z2), new(_x2, _y2, _z2), new(_x1, _y2, _z2),
                KlotzSide.Front, surface);
        }

        /// <summary>
        /// Adds a face to the current mesh (-builder)
        /// </summary>
        private void AddFace(Vector3 corner1, Vector3 corner2, Vector3 corner3, Vector3 corner4, KlotzSide side, KlotzSurfaceFeature surface)
        {
            int v0 = Vertices.Count;

            Vertices.Add(corner1);
            Vertices.Add(corner2);
            Vertices.Add(corner3);
            Vertices.Add(corner4);

            Vector3 normal = NormalForSide(side);
            Normals.Add(normal); Normals.Add(normal); Normals.Add(normal); Normals.Add(normal);

            Vector2 vertexData = BuildVertexUvData(surface, _color, _variant);
            UvData.Add(vertexData); UvData.Add(vertexData); UvData.Add(vertexData); UvData.Add(vertexData);

            Triangles.Add(v0 + 0); Triangles.Add(v0 + 1); Triangles.Add(v0 + 2);
            Triangles.Add(v0 + 0); Triangles.Add(v0 + 2); Triangles.Add(v0 + 3);

            VoxelCoords.Add(_currentCoords); VoxelCoords.Add(_currentCoords);
        }

        private static Vector3 NormalForSide(KlotzSide side)
        {
            return side switch
            {
                KlotzSide.Left => new(-1, 0, 0),
                KlotzSide.Right => new(1, 0, 0),
                KlotzSide.Bottom => new(0, -1, 0),
                KlotzSide.Top => new(0, 1, 0),
                KlotzSide.Back => new(0, 0, -1),
                KlotzSide.Front => new(0, 0, 1),
                _ => Vector3.zero
            };
        }

        public void AddNonPrimitiveKlotz(NonPrimitiveKlotzMesh template, KlotzDirection dir)
        {
            int v0 = Vertices.Count;
            Vector3 origin = new(_x1, _y1, _z1);

            for (int i = 0; i < template.Vertices.Length; i++)
            {
                Vertices.Add(origin + RotatePositionForDirection(template.Vertices[i], dir));
                Normals.Add(RotateDirectionForDirection(template.Normals[i], dir));
                UvData.Add(BuildVertexUvData(template.VertexFeatures[i], _color, _variant));
            }

            for (int i = 0; i < template.Triangles.Length; i++)
            {
                Triangles.Add(v0 + template.Triangles[i]);
            }

            int triangleCount = template.Triangles.Length / 3;
            for (int i = 0; i < triangleCount; i++)
            {
                VoxelCoords.Add(_currentCoords);
            }
        }

        /// <summary>
        /// Rotates a local mesh-space position for the given direction.
        /// Unlike <see cref="RotateDirectionForDirection"/>, this also corrects for the fact
        /// that a grid cell index denotes its min corner and extends forward: a purely mirrored
        /// axis (-p) would be off by exactly one cell size, since <see cref="SubKlotz.TranslateSubIndexToCoords"/>
        /// walks backwards one whole cell at a time from the root rather than reflecting the
        /// continuous span around it. Using (segmentSize - p) for a reversed axis instead of -p
        /// corrects for that.
        /// </summary>
        private Vector3 RotatePositionForDirection(Vector3 p, KlotzDirection dir)
        {
            return dir switch
            {
                KlotzDirection.ToPosX => new(p.x, p.y, p.z),
                KlotzDirection.ToNegX => new(_segmentSize.x - p.x, p.y, _segmentSize.z - p.z),
                KlotzDirection.ToPosZ => new(_segmentSize.z - p.z, p.y, p.x),
                KlotzDirection.ToNegZ => new(p.z, p.y, _segmentSize.x - p.x),
                _ => p
            };
        }

        /// <summary>
        /// Rotates a local direction vector (e.g. a normal) for the given direction. Since this
        /// is a direction and not a position, no cell-size correction is needed here (see
        /// <see cref="RotatePositionForDirection"/>) - it's a pure rotation.
        /// </summary>
        private static Vector3 RotateDirectionForDirection(Vector3 p, KlotzDirection dir)
        {
            return dir switch
            {
                KlotzDirection.ToPosX => new(p.x, p.y, p.z),
                KlotzDirection.ToNegX => new(-p.x, p.y, -p.z),
                KlotzDirection.ToPosZ => new(-p.z, p.y, p.x),
                KlotzDirection.ToNegZ => new(p.z, p.y, -p.x),
                _ => p
            };
        }
    }
}
