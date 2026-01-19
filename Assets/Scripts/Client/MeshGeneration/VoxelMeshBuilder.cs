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

        public VoxelMeshBuilder(Vector3 size, KlotzSize subDivs)
        {
            _segmentSize = new(size.x / subDivs.X, size.y / subDivs.Y, size.z / subDivs.Z);
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
        public void AddLeftFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x1, _y1, _z2), new(_x1, _y2, _z2), new(_x1, _y2, _z1), new(_x1, _y1, _z1),
                KlotzSide.Left, flags);
        }

        /// <summary>
        /// A.K.A. the right face
        /// </summary>
        public void AddRightFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x2, _y1, _z1), new(_x2, _y2, _z1), new(_x2, _y2, _z2), new(_x2, _y1, _z2),
                KlotzSide.Right, flags);
        }

        /// <summary>
        /// A.K.A. the bottom face
        /// </summary>
        public void AddBottomFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x2, _y1, _z1), new(_x2, _y1, _z2), new(_x1, _y1, _z2), new(_x1, _y1, _z1),
                KlotzSide.Bottom, flags);
        }

        /// <summary>
        /// A.K.A. the top face
        /// </summary>
        public void AddTopFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x1, _y2, _z1), new(_x1, _y2, _z2), new(_x2, _y2, _z2), new(_x2, _y2, _z1),
                KlotzSide.Top, flags);
        }

        /// <summary>
        /// A.K.A. the back face
        /// </summary>
        public void AddBackFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x1, _y2, _z1), new(_x2, _y2, _z1), new(_x2, _y1, _z1), new(_x1, _y1, _z1),
                KlotzSide.Back, flags);
        }

        /// <summary>
        /// A.K.A. the front face
        /// </summary>
        public void AddFrontFace(KlotzSideFlags flags = 0)
        {
            AddFace(
                new(_x1, _y1, _z2), new(_x2, _y1, _z2), new(_x2, _y2, _z2), new(_x1, _y2, _z2),
                KlotzSide.Front, flags);
        }

        /// <summary>
        /// Adds a face to the current mesh (-builder)
        /// </summary>
        private void AddFace(Vector3 corner1, Vector3 corner2, Vector3 corner3, Vector3 corner4, KlotzSide side, KlotzSideFlags sideFlags)
        {
            int v0 = Vertices.Count;

            Vertices.Add(corner1);
            Vertices.Add(corner2);
            Vertices.Add(corner3);
            Vertices.Add(corner4);

            KlotzVertexFlags flags = 0;
            if (sideFlags.HasFlag(KlotzSideFlags.HasStuds)) flags |= KlotzVertexFlags.SideHasStuds;
            if (sideFlags.HasFlag(KlotzSideFlags.HasHoles)) flags |= KlotzVertexFlags.SideHasHoles;

            Vector2 vertexData = BuildVertexUvData(side, flags, _color, _variant);
            UvData.Add(vertexData); UvData.Add(vertexData); UvData.Add(vertexData); UvData.Add(vertexData);

            Triangles.Add(v0 + 0); Triangles.Add(v0 + 1); Triangles.Add(v0 + 2);
            Triangles.Add(v0 + 0); Triangles.Add(v0 + 2); Triangles.Add(v0 + 3);

            VoxelCoords.Add(_currentCoords); VoxelCoords.Add(_currentCoords);
        }
    }
}
