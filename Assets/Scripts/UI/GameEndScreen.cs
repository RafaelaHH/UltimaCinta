using UnityEngine;
using UltimaCinta.Data;
using UltimaCinta.Ghost;
using UnityEngine.SceneManagement;
namespace UltimaCinta.UI
{
    [DisallowMultipleComponent]
    public sealed class GameEndScreen : MonoBehaviour
    {
        [SerializeField] private NightClock nightClock;
        private string _message;
        private bool _visible;
        public bool IsVisible => _visible;
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
                (nightClock != null ? nightClock.IsEveryoneDead : AllPlayersDead())
                    ? "DERROTA - TODO EL EQUIPO HA MUERTO" : "DERROTA - AMANECIO";
            if (victory) UltimaCinta.Audio.PlayerAudioController.LocalInstance?.PlayMilestoneCue();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        private static bool AllPlayersDead()
        {
            var players = FindObjectsByType<SanityController>();
            if (players.Length == 0) return false;
            foreach (var player in players) if (player != null && !player.IsDead) return false;
            return true;
        }
        private void OnGUI()
        {
            if (!_visible) return;
            GUI.color = new Color(GamePalette.Background.r, GamePalette.Background.g, GamePalette.Background.b, 0.94f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var width = Mathf.Min(Screen.width - 36f, 760f * GameMenuStyles.Scale);
            var panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.28f, width, Screen.height * 0.44f);
            GameMenuStyles.DrawPanel(panel);
            GameMenuStyles.DrawSubtitle(new Rect(panel.x + 28f, panel.y + 36f, panel.width - 56f, 34f), "REGISTRO DE INVESTIGACIÓN · CINTA CERRADA");
            GameMenuStyles.DrawTitle(new Rect(panel.x + 34f, panel.y + 84f, panel.width - 68f, 112f), _message);
            GameMenuStyles.DrawBody(new Rect(panel.x + 52f, panel.y + 202f, panel.width - 104f, 48f),
                "La grabación termina aquí. El archivo se conserva para el siguiente equipo.");
            var buttonWidth = Mathf.Min(panel.width - 100f, 360f * GameMenuStyles.Scale);
            if (GameMenuStyles.Button(new Rect(panel.center.x - buttonWidth * 0.5f, panel.yMax - 74f * GameMenuStyles.Scale,
                    buttonWidth, 46f * GameMenuStyles.Scale), "VOLVER AL MENÚ PRINCIPAL"))
            {
                var lobby = FindAnyObjectByType<NetworkLobbyController>();
                if (lobby != null) lobby.ReturnToTitle();
                else SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            }
        }
    }
}
