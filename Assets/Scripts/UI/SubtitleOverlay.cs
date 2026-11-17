using UltimaCinta.Data;
using UnityEngine;

namespace UltimaCinta.UI
{
    [DisallowMultipleComponent]
    public sealed class SubtitleOverlay : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        private static SubtitleOverlay _localPlayer;
        private string _message;
        private float _remaining;

        public static void Show(string message, float duration = 0f)
        {
            if (string.IsNullOrWhiteSpace(message) || !GameSettingsStore.SubtitlesEnabled || _localPlayer == null) return;
            _localPlayer._message = message;
            _localPlayer._remaining = duration > 0f ? duration :
                _localPlayer.gameConfig != null ? _localPlayer.gameConfig.Audio.SubtitleDurationSeconds : 2.5f;
        }

        public static void ShowAt(Vector3 sourcePosition, float audibleDistance, string message, float duration = 0f)
        {
            if (_localPlayer == null || Vector3.Distance(_localPlayer.transform.position, sourcePosition) > audibleDistance) return;
            Show(message, duration);
        }

        public void SetLocalRole(bool isLocalPlayer)
        {
            if (isLocalPlayer) _localPlayer = this;
            else if (_localPlayer == this) _localPlayer = null;
        }

        private void Update()
        {
            if (_remaining > 0f) _remaining = Mathf.Max(0f, _remaining - Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (_remaining <= 0f || !GameSettingsStore.SubtitlesEnabled) return;
            var width = Mathf.Min(Screen.width - 40f, 760f * GameMenuStyles.Scale);
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 98f * GameMenuStyles.Scale,
                width, 58f * GameMenuStyles.Scale);
            GUI.color = GamePalette.Panel;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = GamePalette.Lamp;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f * GameMenuStyles.Scale, rect.height), Texture2D.whiteTexture);
            GUI.color = GamePalette.Paper;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(17f * GameMenuStyles.Scale),
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            style.normal.textColor = GamePalette.Paper;
            GUI.Label(new Rect(rect.x + 18f, rect.y + 3f, rect.width - 36f, rect.height - 6f), _message, style);
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            if (_localPlayer == this) _localPlayer = null;
        }
    }
}
