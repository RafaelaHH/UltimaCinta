using UnityEngine;
using UltimaCinta.Data;
using UltimaCinta.Ghost;
namespace UltimaCinta.UI
{
    [DisallowMultipleComponent]
    public sealed class GameEndScreen : MonoBehaviour
    {
        [SerializeField] private NightClock nightClock;
        private string _message;
        private bool _visible;
        public void Configure(NightClock clock)
        {
            if (nightClock != null) nightClock.NightEnded -= OnNightEnded;
            nightClock = clock;
            if (isActiveAndEnabled && nightClock != null) nightClock.NightEnded += OnNightEnded;
        }
        private void OnEnable() { if (nightClock != null) nightClock.NightEnded += OnNightEnded; }
        private void OnDisable() { if (nightClock != null) nightClock.NightEnded -= OnNightEnded; }
        private void OnNightEnded(bool victory)
        {
            _visible = true;
            _message = victory ? "VICTORIA - EL FANTASMA FUE EXPULSADO" :
                AllPlayersDead() ? "DERROTA - TODO EL EQUIPO HA MUERTO" : "DERROTA - AMANECIO";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        private static bool AllPlayersDead()
        {
            var players = FindObjectsByType<SanityController>(FindObjectsSortMode.None);
            if (players.Length == 0) return false;
            foreach (var player in players) if (player != null && !player.IsDead) return false;
            return true;
        }
        private void OnGUI()
        {
            if (!_visible) return;
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 36, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 100f), _message, style);
        }
    }
}
