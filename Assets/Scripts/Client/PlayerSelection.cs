using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

namespace Clotzbergh.Client
{
    public class PlayerSelection : MonoBehaviour
    {
        private SelectionTool _selectionTool = SelectionTool.None;
        private Vector3 _viewedPosition = Vector3.zero;
        private ClientChunk _viewedChunk = null;
        private KlotzWorldData _viewedKlotz = null;
        private GameObject _highlightBox = null;
        private Mesh _highlightMesh = null;
        private Material _highlightMaterial = null;
        private Vector3 _highlightMeshSize = Vector3.zero;
        private const float HighlightLineWidth = 0.03f;
        private KlotzRegion _cutout = KlotzRegion.Empty;
        private long _selectionChangeCount = 0;
        private long _cutoutChangeCount = 0;
        private float _timeSinceLastAct;
        private const float ActRepeatInterval = 0.1f; // How fast the action repeats while held down

        public long SelectionChangeCount { get => _selectionChangeCount; }
        public long CutoutChangeCount { get => _cutoutChangeCount; }
        public KlotzRegion Cutout { get => _cutout; }

        public Vector3 ViewedPosition { get => _viewedPosition; } // for debug UI
        public ClientChunk ViewedChunk { get => _viewedChunk; } // for debug UI
        public KlotzWorldData ViewedKlotz { get => _viewedKlotz; } // for debug UI

        public SelectionTool CurrentTool { get => _selectionTool; }

        /// <summary>
        /// Set by GameClient, since applying a tool can reach across several chunks.
        /// </summary>
        public ClientChunkStore ChunkStore { get; set; }

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

            bool toolChanged = HandleToolChanges();

            var view = GetPlayerView();
            bool viewChanged;

            if (view != null)
            {
                viewChanged = !view.viewedKlotz.Equals(_viewedKlotz);
                _viewedChunk = view.viewedChunk;
                _viewedKlotz = view.viewedKlotz;
                _viewedPosition = view.viewedPosition;
            }
            else
            {
                viewChanged = _viewedKlotz != null;
                _viewedChunk = null;
                _viewedKlotz = null;
                _viewedPosition = Vector3.zero;
            }

            if (viewChanged || toolChanged)
            {
                _selectionChangeCount++;
                UpdateSelection();
            }

            HandleMouseActions(view);
        }

        /// <summary>
        /// Is only called when the viewed Klotz changes. Updates the selection box to match the currently viewed Klotz.
        /// </summary>
        private void UpdateSelection()
        {
            bool cutoutWasEmpty = _cutout.IsEmpty;

            if (_viewedKlotz == null || _selectionTool == SelectionTool.None)
            {
                _highlightBox.SetActive(false);
                _cutout = KlotzRegion.Empty;
            }
            else
            {
                SetSelectionBoxColor(_viewedKlotz.IsFreeToTake ? Color.green : Color.red);
                UpdateHighlightMesh(_viewedKlotz.WorldSize);
                _highlightBox.transform.position = _viewedKlotz.WorldPosition;
                _highlightBox.transform.rotation = _viewedKlotz.WorldRotation;
                _highlightBox.SetActive(true);
                (RelKlotzCoords relMin, RelKlotzCoords relMax) = _viewedKlotz.OccupiedRange;
                AbsKlotzCoords klotzMin = relMin.ToAbs(_viewedChunk.Coords);
                AbsKlotzCoords klotzMax = relMax.ToAbs(_viewedChunk.Coords);

                // SingleKlotz gets no cutout: the highlight box already marks the one klotz, and
                // cutting it away would leave the player aiming at a hole.
                _cutout = _selectionTool == SelectionTool.SingleKlotz
                    ? KlotzRegion.Empty
                    : SelectionTools.RegionFor(_selectionTool, klotzMin, klotzMax);
            }

            if (!cutoutWasEmpty || !_cutout.IsEmpty)
            {
                _cutoutChangeCount++;
            }
        }

        private PlayerView GetPlayerView()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.value);

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

        private static SelectionTool NextSelectionTool(SelectionTool current, int direction)
        {
            var tools = (SelectionTool[])Enum.GetValues(typeof(SelectionTool));
            int newIndex = (Array.IndexOf(tools, current) + direction + tools.Length) % tools.Length;
            return tools[newIndex];
        }

        /// <summary>
        /// Handles changes to the selection tool based on user input.
        /// </summary>
        /// <returns>true if the selection tool was changed, false otherwise</returns>
        private bool HandleToolChanges()
        {
            // IF mouse wheel is used, change selection tool
            if (Mouse.current.scroll.value.y != 0)
            {
                int direction = Mouse.current.scroll.value.y > 0 ? -1 : 1;
                _selectionTool = NextSelectionTool(_selectionTool, direction);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Takes the viewed klotz on button press, then repeats while the button stays held.
        /// </summary>
        private void HandleMouseActions(PlayerView selection)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _timeSinceLastAct = 0f;
                ApplyCurrentTool(selection);
            }
            else if (Mouse.current.leftButton.isPressed)
            {
                _timeSinceLastAct += Time.deltaTime;
                if (_timeSinceLastAct >= ActRepeatInterval)
                {
                    _timeSinceLastAct = 0f;
                    ApplyCurrentTool(selection);
                }
            }
        }

        /// <summary>
        /// The region is worked out here rather than reused from the cutout, which is only the
        /// preview - it stays empty for SingleKlotz, where the highlight box already shows what
        /// is about to go.
        /// </summary>
        private void ApplyCurrentTool(PlayerView selection)
        {
            if (ChunkStore == null || selection?.viewedChunk == null || selection.viewedKlotz == null)
                return;

            ChunkCoords chunkCoords = selection.viewedChunk.Coords;
            (RelKlotzCoords relMin, RelKlotzCoords relMax) = selection.viewedKlotz.OccupiedRange;

            KlotzRegion region = SelectionTools.RegionFor(
                _selectionTool, relMin.ToAbs(chunkCoords), relMax.ToAbs(chunkCoords));

            ChunkStore.ApplyTool(chunkCoords, selection.viewedKlotz.RootCoords, _selectionTool, region);
        }

        private void SetSelectionBoxColor(Color color)
        {
            _highlightMaterial.color = color;
        }

        /// <summary>
        /// The wireframe around the klotz being aimed at. Real geometry rather than a
        /// LineRenderer, whose camera-facing ribbon twists where the edges meet at the corners.
        /// </summary>
        private GameObject CreateHighlightCube()
        {
            GameObject box = new("Highlight Box");
            box.SetActive(false);

            _highlightMesh = new Mesh { name = "Highlight Wireframe" };
            _highlightMaterial = new Material(Shader.Find("Unlit/Color"));

            box.AddComponent<MeshFilter>().sharedMesh = _highlightMesh;

            MeshRenderer meshRenderer = box.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _highlightMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            return box;
        }

        /// <summary>
        /// Rebuilds the wireframe as twelve bars spanning a box of the given size. Built to size
        /// instead of scaling the transform, which would make the bars thicker along whichever
        /// axis the klotz is longer.
        /// </summary>
        private void UpdateHighlightMesh(Vector3 size)
        {
            if (_highlightMeshSize == size)
                return;

            _highlightMeshSize = size;
            float w = HighlightLineWidth / 2;

            List<Vector3> vertices = new();
            List<int> triangles = new();

            // Bars run past the corners by half a width, so the corners come out solid.
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    float x = i * size.x, y = i * size.y;
                    float z = j * size.z;

                    AddBox(vertices, triangles,
                        new(-w, y - w, z - w), new(size.x + w, y + w, z + w));
                    AddBox(vertices, triangles,
                        new(x - w, -w, z - w), new(x + w, size.y + w, z + w));
                    AddBox(vertices, triangles,
                        new(x - w, j * size.y - w, -w), new(x + w, j * size.y + w, size.z + w));
                }
            }

            _highlightMesh.Clear();
            _highlightMesh.SetVertices(vertices);
            _highlightMesh.SetTriangles(triangles, 0);
            _highlightMesh.RecalculateBounds();
        }

        /// <summary>
        /// Faces wound the same way as <see cref="MeshGeneration.VoxelMeshBuilder"/> does it, so
        /// that (b-a) x (c-a) points out of the box and none of them get culled away.
        /// </summary>
        private static void AddBox(List<Vector3> vertices, List<int> triangles, Vector3 min, Vector3 max)
        {
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int v0 = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                triangles.Add(v0); triangles.Add(v0 + 1); triangles.Add(v0 + 2);
                triangles.Add(v0); triangles.Add(v0 + 2); triangles.Add(v0 + 3);
            }

            Face(new(min.x, max.y, min.z), new(min.x, max.y, max.z), new(max.x, max.y, max.z), new(max.x, max.y, min.z));
            Face(new(max.x, min.y, min.z), new(max.x, min.y, max.z), new(min.x, min.y, max.z), new(min.x, min.y, min.z));
            Face(new(min.x, min.y, max.z), new(min.x, max.y, max.z), new(min.x, max.y, min.z), new(min.x, min.y, min.z));
            Face(new(max.x, min.y, min.z), new(max.x, max.y, min.z), new(max.x, max.y, max.z), new(max.x, min.y, max.z));
            Face(new(min.x, max.y, min.z), new(max.x, max.y, min.z), new(max.x, min.y, min.z), new(min.x, min.y, min.z));
            Face(new(min.x, min.y, max.z), new(max.x, min.y, max.z), new(max.x, max.y, max.z), new(min.x, max.y, max.z));
        }
    }
}
