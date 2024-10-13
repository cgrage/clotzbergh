using UnityEngine;

public class PlayerSelection : MonoBehaviour
{
    public TerrainClient terrain;

    private Vector3 _hitPosition = Vector3.zero;
    private Vector3Int _hitChunk = Vector3Int.zero;
    private GameObject _highlightBox = null;

    // Start is called before the first frame update
    void Start()
    {
        if (terrain == null)
        {
            Debug.Log($"PlayerSelection has no terrain. Deactivating.");
            return;
        }

        _highlightBox = CreateHighlightCube();
    }

    // Update is called once per frame
    void Update()
    {
        if (_highlightBox == null)
            return;

        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000))
        {
            _hitPosition = hit.point;
            _hitChunk = WorldChunk.PositionToChunkCoords(_hitPosition);

            _highlightBox.transform.position = WorldChunk.ChunkCoordsToPosition(_hitChunk);
            _highlightBox.transform.localScale = WorldDef.ChunkSize;
            _highlightBox.SetActive(true);
        }
        else
        {
            _hitPosition = Vector3.zero;
            _hitChunk = Vector3Int.zero;
            _highlightBox.SetActive(false);
        }
    }

    void OnGUI()
    {
        GUIStyle style = new() { fontSize = 16 };
        style.normal.textColor = Color.black;

        GUI.Label(new Rect(5, 150, 500, 150),
            $"Hit: {_hitPosition}\n" +
            $"Chunk: {_hitChunk}\n",
            style);
    }

    private static GameObject CreateHighlightCube()
    {
        GameObject box = new("Highlight Box");

        // Define the vertices of the cuboid
        Vector3[] vertices = {
            new (0, 0, 0), new (1, 0, 0), new (1, 1, 0), new (0, 1, 0),
            new (0, 0, 1), new (1, 0, 1), new (1, 1, 1), new (0, 1, 1)
        };

        // Define the edges of the cuboid
        int[,] edges = {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
        };

        // Create a LineRenderer for each edge
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            GameObject lineObj = new("Line");
            lineObj.transform.SetParent(box.transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default")); // Using a simple shader
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.positionCount = 2;
            lr.SetPosition(0, vertices[edges[i, 0]]);
            lr.SetPosition(1, vertices[edges[i, 1]]);
            lr.useWorldSpace = false; // Ensure local space rendering
        }

        return box;
    }
}
