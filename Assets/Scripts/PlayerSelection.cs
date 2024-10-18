using UnityEngine;

public class PlayerSelection : MonoBehaviour
{
    private Vector3 _viewedPosition = Vector3.zero;
    private Klotz _viewedKlotz = null;
    private GameObject _highlightBox = null;

    private bool _actIsHolding;
    private float _actHoldTime;
    private const float RequiredHoldTime = 0.1f; // The duration required to trigger the action

    private class Selection
    {
        public Vector3 viewedPosition;
        public ClientChunk viewedChunk;
        public Klotz viewedKlotz;
    }

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

        var selection = GetSelection();
        if (selection != null)
        {
            _viewedKlotz = selection.viewedKlotz;
            _viewedPosition = selection.viewedPosition;

            _highlightBox.transform.position = _viewedKlotz.worldPosition;
            _highlightBox.transform.localScale = _viewedKlotz.worldSize;
            _highlightBox.SetActive(true);
        }
        else
        {
            _viewedKlotz = null;
            _viewedPosition = _viewedPosition = Vector3.zero;

            _highlightBox.SetActive(false);
        }

        HandleMouseActions(selection);
    }

    private Selection GetSelection()
    {
        Vector3 screenCenter = new(Screen.width / 2, Screen.height / 2, 0);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 8))
            return null;

        if (!hit.collider.gameObject.TryGetComponent<ClientChunk.OwnerRef>(out ClientChunk.OwnerRef ownerRef))
            return null;

        return new Selection()
        {
            viewedPosition = hit.point,
            viewedChunk = ownerRef.owner,
            viewedKlotz = ownerRef.owner.GetKlotzFromTriangleIndex(hit.triangleIndex),
        };
    }

    private void HandleMouseActions(Selection selection)
    {
        if (Input.GetMouseButtonDown(0)) // 0 is the left mouse button
        {
            _actIsHolding = true;
            _actHoldTime = 0f;
        }

        if (Input.GetMouseButtonUp(0)) // Release the mouse button
        {
            _actIsHolding = false;
            _actHoldTime = 0f;
        }

        if (_actIsHolding)
        {
            _actHoldTime += Time.deltaTime;
            if (_actHoldTime >= RequiredHoldTime)
            {
                selection?.viewedChunk.TakeKlotz(selection.viewedKlotz.rootCoords);
                _actHoldTime = 0f;
            }
        }
    }

    void OnGUI()
    {
        GUIStyle style = new() { fontSize = 16 };
        style.normal.textColor = Color.black;

        GUI.Label(new Rect(5, 200, 500, 200),
            $"Hit: {_viewedPosition}\n" +
            $"Type: {_viewedKlotz?.type}\n",
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
            lr.startColor = Color.black;
            lr.endColor = Color.black;
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
