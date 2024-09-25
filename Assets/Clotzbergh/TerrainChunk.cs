using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    private readonly GameObject _gameObject;
    private readonly Vector3 _position;
    private readonly Bounds _bounds;

    private readonly MeshRenderer _meshRenderer;
    private readonly MeshFilter _meshFilter;

    //LODInfo[] detailLevels;
    //LODMesh[] lodMeshes;

    //MapData mapData;
    //bool mapDataReceived;

    //int previousLODIndex = -1;

    public TerrainChunk(Vector3Int coords, /*int size, LODInfo[] detailLevels,*/ Transform parent /*, Material material*/)
    {
        // this.detailLevels = detailLevels;

        _position.x = coords.x * WorldChunk.Size.x;
        _position.y = coords.y * WorldChunk.Size.y;
        _position.z = coords.z * WorldChunk.Size.z;

        _bounds = new Bounds(_position, WorldChunk.Size);

        _gameObject = new GameObject("Terrain Chunk");
        _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
        _meshFilter = _gameObject.AddComponent<MeshFilter>();
        // _meshRenderer.material = material;

        _gameObject.transform.position = _position;
        _gameObject.transform.parent = parent;
        // IsVisible = false;

        // lodMeshes = new LODMesh[detailLevels.Length];
        //for (int i = 0; i < lodMeshes.Length; i++)
        //{
        //    lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
        //}

        //mapGenerator.RequestMapData(position, OnMapDataReceived);
    }

    /*
        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            // print("Map data received");

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                // print(string.Format("UpdateTerrainChunk {0}", this.position));

                float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
                bool isVisible = viewerDistFromNearestEdge <= maxViewDist;

                if (isVisible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                            // print(string.Format("SetMesh {0}", this.position));
                        }
                        else if (!lodMesh.hasReqMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this);
                }

                IsVisible = isVisible;
            }
        }

        public bool IsVisible
        {
            get { return meshObject.activeSelf; }
            set { meshObject.SetActive(value); }
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasReqMesh;
        public bool hasMesh;
        int lod;
        Action updateCallback;

        public LODMesh(int lod, Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            // print("Mesh data received");
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasReqMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
            */
}