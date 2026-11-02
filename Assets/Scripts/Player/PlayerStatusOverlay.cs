using UnityEngine;
namespace UltimaCinta.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerStatusOverlay : MonoBehaviour
    {
        [SerializeField] private StaminaResource stamina;
        private void Awake() { if (stamina == null) stamina = GetComponent<StaminaResource>(); }
        private void OnGUI()
        {
            if (stamina == null) return;
            GUI.Label(new Rect(24f, Screen.height - 72f, 160f, 22f), "STAMINA");
            GUI.Box(new Rect(24f, Screen.height - 48f, 180f * stamina.NormalizedValue, 16f), GUIContent.none);
            GUI.Box(new Rect(24f, Screen.height - 48f, 180f, 16f), GUIContent.none);
        }
    }
}
