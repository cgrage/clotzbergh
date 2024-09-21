using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDist = 450;
    public Transform viewer;

    public static Vector2 viewerPos;
    int chunkSize;
    int chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
    }

    void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        foreach (var chunk in terrainChunksVisibleLastUpdate)
        {
            chunk.IsVisible = false;
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDict[viewedChunkCoord].IsVisible)
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDict[viewedChunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, this.transform));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        public TerrainChunk(Vector2 coord, int size, Transform parent)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            IsVisible = false;
        }

        public void UpdateTerrainChunk()
        {
            float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            bool isVisible = viewerDistFromNearestEdge <= maxViewDist;
            IsVisible = isVisible;
        }

        public bool IsVisible
        {
            get { return meshObject.activeSelf; }
            set { meshObject.SetActive(value); }
        }


    }
}
