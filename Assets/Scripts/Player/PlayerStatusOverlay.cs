using UnityEngine;
namespace UltimaCinta.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerStatusOverlay : MonoBehaviour
    {
        private static readonly Color StaminaBarColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private StaminaResource stamina;
        private void Awake() { if (stamina == null) stamina = GetComponent<StaminaResource>(); }
        private void OnGUI()
        {
            if (stamina == null) return;
            GUI.Label(new Rect(24f, Screen.height - 72f, 160f, 22f), "STAMINA");
            var barRect = new Rect(24f, Screen.height - 48f, 180f, 16f);
            GUI.Box(barRect, GUIContent.none);
            var originalColor = GUI.color;
            GUI.color = StaminaBarColor;
            GUI.Box(new Rect(barRect.x, barRect.y, barRect.width * stamina.NormalizedValue, barRect.height), GUIContent.none);
            GUI.color = originalColor;
        }
    }
}
