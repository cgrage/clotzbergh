using UnityEngine;

public class PlayerSelection : MonoBehaviour
{
    private Vector3 _hitPosition = Vector3.zero;
    private Klotz _hitKlotz = null;
    private GameObject _highlightBox = null;

    // Start is called before the first frame update
    void Start()
    {
        _highlightBox = CreateHighlightCube();
    }

    // Update is called once per frame
    void Update()
    {
        if (_highlightBox == null)
            return;

        // Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        // Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        _hitPosition = Vector3.zero;
        _hitKlotz = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 8))
        {
            _hitPosition = hit.point;
            if (hit.collider.gameObject.TryGetComponent<TerrainChunk.OwnerRef>(out TerrainChunk.OwnerRef ownerRef))
            {
                _hitKlotz = ownerRef.owner.GetKlotzFromTriangleIndex(hit.triangleIndex);
            }
        }

        if (_hitKlotz != null)
        {
            _highlightBox.transform.position = _hitKlotz.position;
            _highlightBox.transform.localScale = _hitKlotz.size;
            _highlightBox.SetActive(true);
        }
        else
        {
            _highlightBox.SetActive(false);
        }
    }

    void OnGUI()
    {
        GUIStyle style = new() { fontSize = 16 };
        style.normal.textColor = Color.black;

        GUI.Label(new Rect(5, 150, 500, 150),
            $"Hit: {_hitPosition}\n" +
            $"Klotz: {_hitKlotz?.type}\n",
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
            lr.startWidth = 0.03f;
            lr.endWidth = 0.03f;
            lr.positionCount = 2;
            lr.SetPosition(0, vertices[edges[i, 0]]);
            lr.SetPosition(1, vertices[edges[i, 1]]);
            lr.useWorldSpace = false; // Ensure local space rendering
            lr.numCapVertices = 2; // Add anti-aliasing to the lines
        }

        return box;
    }
}
