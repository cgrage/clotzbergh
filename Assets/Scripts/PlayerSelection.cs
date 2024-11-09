using UnityEngine;

public class PlayerSelection : MonoBehaviour
{
    private Vector3 _viewedPosition = Vector3.zero;
    private KlotzWorldData _viewedKlotz = null;
    private GameObject _highlightBox = null;

    private bool _actIsHolding;
    private float _actHoldTime;
    private const float RequiredHoldTime = 0.1f; // The duration required to trigger the action

    private class Selection
    {
        public Vector3 viewedPosition;
        public ClientChunk viewedChunk;
        public KlotzWorldData viewedKlotz;
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

            SetSelectionBoxColor(_viewedKlotz.isFreeToTake ? Color.green : Color.red);
            _highlightBox.transform.position = _viewedKlotz.worldPosition;
            _highlightBox.transform.localScale = _viewedKlotz.worldSize;
            _highlightBox.transform.rotation = _viewedKlotz.worldRotation;
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

    private void SetSelectionBoxColor(Color color)
    {
        LineRenderer lr = _highlightBox.GetComponent<LineRenderer>();
        lr.startColor = color;
        lr.endColor = color;
    }

    private static GameObject CreateHighlightCube()
    {
        GameObject box = new("Highlight Box");

        // Define the vertices of the cuboid
        Vector3[] vertices = {
            new (0, 0, 0), new (1, 0, 0), new (1, 0, 1), new (0, 0, 1), // Bottom vertices
            new (0, 1, 0), new (1, 1, 0), new (1, 1, 1), new (0, 1, 1), // Top vertices
        };

        // Define the edges of the cuboid
        int[] positions = {
            0, 1, 2, 3, 0, // Bottom
            4, 5, 6, 7, 4, // Top
            5, 1, 2, 6, 7, 3 // Sticky
        };

        // Create a single LineRenderer for all edges
        LineRenderer lr = box.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default")); // Using a simple shader
        lr.startColor = Color.black;
        lr.endColor = Color.black;
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        lr.positionCount = positions.Length;
        lr.useWorldSpace = false; // Ensure local space rendering
        lr.numCapVertices = 2; // Add anti-aliasing to the lines

        // Set positions for all edges
        for (int i = 0; i < positions.Length; i++)
        {
            lr.SetPosition(i, vertices[positions[i]]);
        }

        return box;
    }
}
