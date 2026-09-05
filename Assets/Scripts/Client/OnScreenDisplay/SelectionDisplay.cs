using TMPro;
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

            if (!TryGetComponent<TextMeshProUGUI>(out var textMeshPro))
                return;

            string selectionTool = PlayerSelection.CurrentTool.ToString();
            textMeshPro.text = $"Selection Tool: {selectionTool}";
        }
    }
}
