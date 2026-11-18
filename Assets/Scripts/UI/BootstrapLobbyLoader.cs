using UltimaCinta.Audio;
using UltimaCinta.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaCinta.UI
{
    [DisallowMultipleComponent]
    public sealed class BootstrapLobbyLoader : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private string lobbySceneName = "Lobby";
        private bool _showOptions;
        private AudioSource _uiAudio;

        private void Awake()
        {
            if (gameConfig == null) gameConfig = Resources.Load<GameConfig>("GameConfig");
            GameSettingsStore.Apply(gameConfig);
            _uiAudio = gameObject.AddComponent<AudioSource>();
            _uiAudio.playOnAwake = false;
            _uiAudio.spatialBlend = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Start()
        {
            var arguments = System.Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                var create = string.Equals(arguments[index], "-ucCreateRoom", System.StringComparison.OrdinalIgnoreCase);
                var join = string.Equals(arguments[index], "-ucJoinRoom", System.StringComparison.OrdinalIgnoreCase);
                if ((!create && !join) || string.IsNullOrWhiteSpace(arguments[index + 1])) continue;
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
                return;
            }
        }

        private void OnGUI()
        {
            GameMenuStyles.DrawBackdrop();
            var scale = GameMenuStyles.Scale;
            var width = Mathf.Min(Screen.width - 36f, 610f * scale);
            var panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.19f, width, Screen.height * 0.62f);
            GameMenuStyles.DrawPanel(panel);

            GameMenuStyles.DrawSubtitle(new Rect(panel.x + 32f, panel.y + 26f, panel.width - 64f, 30f), "ARCHIVO 01 · MANSIÓN SAN TELMO");
            GameMenuStyles.DrawTitle(new Rect(panel.x + 30f, panel.y + 58f, panel.width - 60f, 70f), "ÚLTIMA CINTA");
            GameMenuStyles.DrawBody(new Rect(panel.x + 46f, panel.y + 128f, panel.width - 92f, 46f),
                "La cinta sigue grabando después de que el equipo deja de responder.");
            GameMenuStyles.DrawRule(panel.x + 56f, panel.y + 184f, panel.width - 112f);

            var buttonWidth = Mathf.Min(panel.width - 120f, 360f * scale);
            var buttonHeight = 48f * scale;
            var x = panel.center.x - buttonWidth * 0.5f;
            var y = panel.y + 214f * scale;
            if (_showOptions)
            {
                GameSettingsView.Draw(gameConfig, new Rect(panel.x + 54f, y, panel.width - 108f, 184f * scale));
                if (GameMenuStyles.Button(new Rect(x, panel.yMax - 72f * scale, buttonWidth, buttonHeight), "VOLVER"))
                {
                    PlayClick();
                    GameSettingsStore.Save();
                    _showOptions = false;
                }
                return;
            }

            if (GameMenuStyles.Button(new Rect(x, y, buttonWidth, buttonHeight), "JUGAR · MODO ONLINE"))
            {
                PlayClick();
                GameSettingsStore.Save();
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
            }
            y += buttonHeight + 12f * scale;
            if (GameMenuStyles.Button(new Rect(x, y, buttonWidth, buttonHeight), "OPCIONES"))
            {
                PlayClick();
                _showOptions = true;
            }
            y += buttonHeight + 12f * scale;
            if (GameMenuStyles.Button(new Rect(x, y, buttonWidth, buttonHeight), "SALIR"))
            {
                PlayClick();
                GameSettingsStore.Save();
                Application.Quit();
            }
            GameMenuStyles.DrawSubtitle(new Rect(panel.x + 36f, panel.yMax - 38f * scale, panel.width - 72f, 24f * scale),
                "SHARED MODE · 2–4 INVESTIGADORES");
        }

        private void PlayClick()
        {
            if (_uiAudio != null) _uiAudio.PlayOneShot(AudioCueLibrary.UiClick(), gameConfig != null ? gameConfig.Audio.UiVolume : 0.5f);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) GameSettingsStore.Save();
        }

        private void OnApplicationQuit() => GameSettingsStore.Save();
    }
}
