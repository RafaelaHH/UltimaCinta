using UltimaCinta.Audio;
using UltimaCinta.Data;
using UltimaCinta.Interaction;
using UltimaCinta.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UltimaCinta.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        private PlayerInputReader _inputReader;
        private NetworkPlayerAvatar _networkAvatar;
        private AudioSource _uiAudio;
        private Behaviour[] _blockedControls;
        private bool[] _previousControlStates;
        private bool _isLocalPlayer;
        private bool _isOpen;
        private bool _showOptions;
        private float _previousTimeScale;
        private bool _previousListenerPause;

        private void Awake()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            _networkAvatar = GetComponent<NetworkPlayerAvatar>();
            _blockedControls = new Behaviour[]
            {
                _inputReader,
                GetComponent<FirstPersonMovementController>(),
                GetComponent<FirstPersonLookController>(),
                GetComponent<PlayerInteractionController>(),
                GetComponent<UltimaCinta.CameraSystem.FlashlightController>(),
                GetComponent<UltimaCinta.CameraSystem.CameraResourceController>()
            };
            _previousControlStates = new bool[_blockedControls.Length];
            _uiAudio = gameObject.AddComponent<AudioSource>();
            _uiAudio.playOnAwake = false;
            _uiAudio.spatialBlend = 0f;
            _uiAudio.ignoreListenerPause = true;
        }

        private void Start()
        {
            if (_networkAvatar == null || !_networkAvatar.IsNetworkStarted) ConfigureRole(true);
            else ConfigureRole(_networkAvatar.IsLocalPlayer);
            GameSettingsStore.Apply(gameConfig);
        }

        private void Update()
        {
            if (!_isLocalPlayer || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (_isOpen && _showOptions)
            {
                PlayClick();
                GameSettingsStore.Save();
                _showOptions = false;
            }
            else if (_isOpen) Close();
            else Open();
        }

        public void ConfigureRole(bool isLocalPlayer)
        {
            _isLocalPlayer = isLocalPlayer;
            if (!isLocalPlayer && _isOpen) Close();
        }

        private void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            _showOptions = false;
            _previousTimeScale = Time.timeScale;
            _previousListenerPause = AudioListener.pause;
            if (_networkAvatar == null || !_networkAvatar.IsNetworkStarted)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            for (var index = 0; index < _blockedControls.Length; index++)
            {
                var control = _blockedControls[index];
                if (control == null) continue;
                _previousControlStates[index] = control.enabled;
                control.enabled = false;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayClick();
        }

        private void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            GameSettingsStore.Save();
            if (_networkAvatar == null || !_networkAvatar.IsNetworkStarted)
            {
                Time.timeScale = _previousTimeScale;
                AudioListener.pause = _previousListenerPause;
            }
            for (var index = 0; index < _blockedControls.Length; index++)
                if (_blockedControls[index] != null) _blockedControls[index].enabled = _previousControlStates[index];
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            PlayClick();
        }

        private void OnGUI()
        {
            if (!_isOpen || !_isLocalPlayer) return;
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var scale = GameMenuStyles.Scale;
            var width = Mathf.Min(Screen.width - 36f, 640f * scale);
            var panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.18f, width, Screen.height * 0.64f);
            GameMenuStyles.DrawPanel(panel);
            GameMenuStyles.DrawSubtitle(new Rect(panel.x + 36f, panel.y + 24f, panel.width - 72f, 28f), "SESIÓN EN PAUSA");
            GameMenuStyles.DrawTitle(new Rect(panel.x + 36f, panel.y + 50f, panel.width - 72f, 64f), "NO APAGUES LA CÁMARA");
            GameMenuStyles.DrawRule(panel.x + 48f, panel.y + 126f, panel.width - 96f);
            var buttonWidth = Mathf.Min(panel.width - 120f, 380f * scale);
            var buttonHeight = 44f * scale;
            var x = panel.center.x - buttonWidth * 0.5f;

            if (_showOptions)
            {
                GameSettingsView.Draw(gameConfig, new Rect(panel.x + 54f, panel.y + 156f, panel.width - 108f, 184f * scale));
                if (GameMenuStyles.Button(new Rect(x, panel.yMax - 72f * scale, buttonWidth, buttonHeight), "VOLVER A PAUSA"))
                {
                    PlayClick();
                    GameSettingsStore.Save();
                    _showOptions = false;
                }
                return;
            }

            var y = panel.y + 158f * scale;
            if (GameMenuStyles.Button(new Rect(x, y, buttonWidth, buttonHeight), "CONTINUAR")) Close();
            y += buttonHeight + 11f * scale;
            if (GameMenuStyles.Button(new Rect(x, y, buttonWidth, buttonHeight), "OPCIONES"))
            {
                PlayClick();
                _showOptions = true;
            }
            y += buttonHeight + 11f * scale;
            if (GameMenuStyles.Button(new Rect(x, y, buttonWidth, buttonHeight), "VOLVER AL MENÚ"))
            {
                PlayClick();
                Close();
                var lobby = FindAnyObjectByType<NetworkLobbyController>();
                if (lobby != null) lobby.ReturnToTitle();
                else SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            }
            GameMenuStyles.DrawSubtitle(new Rect(panel.x + 30f, panel.yMax - 38f * scale, panel.width - 60f, 22f * scale), "ESC · CONTINUAR");
        }

        private void PlayClick()
        {
            if (_uiAudio != null) _uiAudio.PlayOneShot(AudioCueLibrary.UiClick(), gameConfig != null ? gameConfig.Audio.UiVolume : 0.5f);
        }

        private void OnDisable()
        {
            if (_isOpen) Close();
        }
    }
}
