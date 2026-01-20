using UnityEngine;

namespace Clotzbergh.Client
{
    public class OnScreenDisplayBehavior : MonoBehaviour
    {
        public PlayerSelection PlayerSelection;

        void OnGUI()
        {
            if (PlayerSelection == null)
                return;

            if (!TryGetComponent<RectTransform>(out var rectTransform))
                return;

            GUIStyle style = new() { fontSize = 12 };
            style.normal.textColor = new Color(1.0f, 0.8f, 0.8f, 1f);

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            Vector2 guiTopLeft = new(topLeft.x, Screen.height - topLeft.y);  // Y is inverted in GUI

            string selectionMode = PlayerSelection.SelectionMode.ToString();

            Rect pos = new(guiTopLeft + new Vector2(10, 10), new Vector2(270, 200));

            // TODO: Use text mesh pro here too
            GUI.Label(pos, selectionMode, style);
        }
    }
}
