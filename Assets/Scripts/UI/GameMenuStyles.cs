using UltimaCinta.Data;
using UnityEngine;

namespace UltimaCinta.UI
{
    internal static class GameMenuStyles
    {
        private static GUIStyle _title;
        private static GUIStyle _subtitle;
        private static GUIStyle _body;
        private static GUIStyle _button;
        private static GUIStyle _smallButton;
        private static Texture2D _panelTexture;
        private static Texture2D _buttonTexture;
        private static Texture2D _buttonHoverTexture;
        private static Texture2D _buttonActiveTexture;

        public static float Scale => Mathf.Clamp(Mathf.Min(Screen.width / 1600f, Screen.height / 900f), 0.9f, 1.2f);

        public static void DrawBackdrop()
        {
            EnsureStyles();
            GUI.color = GamePalette.Background;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(GamePalette.Steel.r, GamePalette.Steel.g, GamePalette.Steel.b, 0.16f);
            GUI.DrawTexture(new Rect(Screen.width * 0.68f, 0f, Screen.width * 0.32f, Screen.height), Texture2D.whiteTexture);
            GUI.color = GamePalette.Signal;
            GUI.DrawTexture(new Rect(Screen.width * 0.08f, Screen.height * 0.18f, 3f * Scale, Screen.height * 0.64f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void DrawPanel(Rect rect)
        {
            EnsureStyles();
            GUI.color = GamePalette.Shadow;
            GUI.DrawTexture(new Rect(rect.x + 7f * Scale, rect.y + 9f * Scale, rect.width, rect.height), _panelTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(rect, _panelTexture);
            GUI.color = GamePalette.Lamp;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f * Scale), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void DrawTitle(Rect rect, string text)
        {
            EnsureStyles();
            _title.fontSize = Mathf.RoundToInt(46f * Scale);
            GUI.Label(rect, text, _title);
        }

        public static void DrawSubtitle(Rect rect, string text)
        {
            EnsureStyles();
            _subtitle.fontSize = Mathf.RoundToInt(15f * Scale);
            GUI.Label(rect, text, _subtitle);
        }

        public static void DrawBody(Rect rect, string text)
        {
            EnsureStyles();
            _body.fontSize = Mathf.RoundToInt(16f * Scale);
            GUI.Label(rect, text, _body);
        }

        public static bool Button(Rect rect, string text, bool compact = false)
        {
            EnsureStyles();
            var style = compact ? _smallButton : _button;
            style.fontSize = Mathf.RoundToInt((compact ? 14f : 18f) * Scale);
            return GUI.Button(rect, text, style);
        }

        public static void DrawRule(float x, float y, float width)
        {
            GUI.color = new Color(GamePalette.Lamp.r, GamePalette.Lamp.g, GamePalette.Lamp.b, 0.55f);
            GUI.DrawTexture(new Rect(x, y, width, Mathf.Max(1f, Scale)), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void DrawLabel(Rect rect, string text)
        {
            EnsureStyles();
            _body.fontSize = Mathf.RoundToInt(14f * Scale);
            _body.alignment = TextAnchor.MiddleLeft;
            GUI.Label(rect, text, _body);
        }

        private static void EnsureStyles()
        {
            if (_title != null) return;
            _panelTexture = CreateTexture(GamePalette.Panel);
            _buttonTexture = CreateTexture(GamePalette.Steel);
            _buttonHoverTexture = CreateTexture(new Color32(60, 66, 66, 255));
            _buttonActiveTexture = CreateTexture(GamePalette.Wood);
            _title = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = false
            };
            _title.normal.textColor = GamePalette.Paper;
            _subtitle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            _subtitle.normal.textColor = GamePalette.Lamp;
            _body = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _body.normal.textColor = GamePalette.Text;
            _button = CreateButtonStyle(16f);
            _smallButton = CreateButtonStyle(13f);
        }

        private static GUIStyle CreateButtonStyle(float padding)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset((int)padding, (int)padding, 10, 10),
                border = new RectOffset(2, 2, 2, 2)
            };
            style.normal.background = _buttonTexture;
            style.hover.background = _buttonHoverTexture;
            style.active.background = _buttonActiveTexture;
            style.focused.background = _buttonHoverTexture;
            style.normal.textColor = GamePalette.Paper;
            style.hover.textColor = Color.white;
            style.active.textColor = GamePalette.Lamp;
            style.focused.textColor = Color.white;
            return style;
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
