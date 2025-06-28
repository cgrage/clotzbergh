using System.Collections;
using System.Collections.Generic;
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

            GUIStyle style = new() { fontSize = 12 };
            style.normal.textColor = new Color(1.0f, 0.8f, 0.8f, 1f);

            string selectionMode = "MODE";

            Rect pos = new (10, 10, 270, 200);
            GUI.Label(pos, selectionMode, style);
        }
    }
}
