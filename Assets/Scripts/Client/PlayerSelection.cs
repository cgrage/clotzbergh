using System;
using UnityEngine;

namespace Clotzbergh.Client
{
    public class PlayerSelection : MonoBehaviour
    {
        public enum SelectionModes
        {
            None,
            Klotz,
            HorizontalCircleSmall,
            HorizontalCircleMedium,
            HorizontalCircleLarge,
        }

        private SelectionModes _selectionMode = SelectionModes.None;
        private Vector3 _viewedPosition = Vector3.zero;
        private KlotzWorldData _viewedKlotz = null;
        private GameObject _highlightBox = null;
        private KlotzRegion _cutout = KlotzRegion.Empty;
        private long _changeCount = 0;

        private bool _actIsHolding;
        private float _actHoldTime;
        private const float RequiredHoldTime = 0.1f; // The duration required to trigger the action

        public Material material;

        public long ChangeCount { get => _changeCount; }
        public KlotzRegion Cutout { get => _cutout; }

        public Vector3 ViewedPosition { get => _viewedPosition; } // for debug UI
        public KlotzWorldData ViewedKlotz { get => _viewedKlotz; } // for debug UI

        public SelectionModes SelectionMode { get => _selectionMode; }

        private class PlayerView
        {
            /// <summary>
            /// The point in the world the player is looking at. 'Hit-point' with the world.
            /// </summary>
            public Vector3 viewedPosition;
            public ClientChunk viewedChunk;
            public KlotzWorldData viewedKlotz;
        }

        // Start is called before the first frame update
        void Start()
        {
            _highlightBox = CreateHighlightCube();
            _cutout = KlotzRegion.Empty;
        }

        // Update is called once per frame
        void Update()
        {
            if (_highlightBox == null)
                return;

            HandleModeChanges();

            var view = GetPlayerView();
            bool viewChanged = _viewedKlotz != view?.viewedKlotz;

            if (view != null)
            {
                _viewedKlotz = view.viewedKlotz;
                _viewedPosition = view.viewedPosition;
            }
            else
            {
                _viewedKlotz = null;
                _viewedPosition = _viewedPosition = Vector3.zero;
            }

            if (viewChanged)
            {
                UpdateSelection();
            }

            HandleMouseActions(view);
        }

        /// <summary>
        /// Is only called when the viewed Klotz changes. Updates the selection box to match the currently viewed Klotz.
        /// </summary>
        private void UpdateSelection()
        {
            if (_viewedKlotz != null)
            {
                SetSelectionBoxColor(_viewedKlotz.isFreeToTake ? Color.green : Color.red);
                _highlightBox.transform.position = _viewedKlotz.worldPosition;
                _highlightBox.transform.localScale = _viewedKlotz.worldSize;
                _highlightBox.transform.rotation = _viewedKlotz.worldRotation;
                _highlightBox.SetActive(true);
                _cutout = KlotzRegion.Cylindrical(_viewedKlotz.rootCoords, 5, 1);
            }
            else
            {
                _highlightBox.SetActive(false);
                _cutout = KlotzRegion.Empty;
            }

            _changeCount++;
        }

        private PlayerView GetPlayerView()
        {
            Vector3 screenCenter = new(Screen.width / 2, Screen.height / 2, 0);
            Ray ray = Camera.main.ScreenPointToRay(screenCenter);
            // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, 8))
                return null;

            if (hit.collider == null || hit.collider.gameObject == null)
                return null;

            if (!hit.collider.gameObject.TryGetComponent(out ClientChunk.OwnerRef ownerRef))
                return null;

            ClientChunk chunk = ownerRef.owner;
            if (chunk == null)
                return null;

            KlotzWorldData klotz = chunk.GetKlotzFromTriangleIndex(hit.triangleIndex);
            if (klotz == null)
                return null;

            return new PlayerView()
            {
                viewedPosition = hit.point,
                viewedChunk = chunk,
                viewedKlotz = klotz,
            };
        }

        private static SelectionModes NextSelectionMode(SelectionModes current, int direction)
        {
            var modes = (SelectionModes[])Enum.GetValues(typeof(SelectionModes));
            int newIndex = (Array.IndexOf(modes, current) + direction + modes.Length) % modes.Length;
            return modes[newIndex];
        }

        private void HandleModeChanges()
        {
            // IF mouse wheel is used, change selection mode
            if (Input.mouseScrollDelta.y != 0)
            {
                int direction = Input.mouseScrollDelta.y > 0 ? 1 : -1;
                _selectionMode = NextSelectionMode(_selectionMode, direction);
            }
        }

        private void HandleMouseActions(PlayerView selection)
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

        private void SetSelectionBoxColor(Color color)
        {
            LineRenderer lr = _highlightBox.GetComponent<LineRenderer>();
            lr.startColor = color;
            lr.endColor = color;
        }

        private GameObject CreateHighlightCube()
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
            lr.material = material;
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
}
