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

            string selectionMode = PlayerSelection.SelectionMode.ToString();
            textMeshPro.text = $"Selection Mode: {selectionMode}";
        }
    }
}
