using UltimaCinta.Interaction;
using UltimaCinta.Player;
using UnityEngine;

namespace UltimaCinta.UI
{
    [DisallowMultipleComponent]
    public sealed class VanTutorialGuide : MonoBehaviour
    {
        private const string TutorialSeenKey = "ultima-cinta.tutorial.van-seen";
        private NetworkPlayerAvatar _networkAvatar;
        private VanSupplyInteractable _van;
        private bool _isLocalPlayer;
        private bool _visible;

        private void Awake()
        {
            _networkAvatar = GetComponent<NetworkPlayerAvatar>();
        }

        private void Start()
        {
            if (_networkAvatar == null || !_networkAvatar.IsNetworkStarted) ConfigureRole(true);
            else ConfigureRole(_networkAvatar.IsLocalPlayer);
            if (_van == null) _van = FindAnyObjectByType<VanSupplyInteractable>();
        }

        private void Update()
        {
            if (!_isLocalPlayer || PlayerPrefs.GetInt(TutorialSeenKey, 0) != 0)
            {
                _visible = false;
                return;
            }
            if (_van == null) _van = FindAnyObjectByType<VanSupplyInteractable>();
            _visible = _van != null && _van.IsOccupant(gameObject);
        }

        public void ConfigureRole(bool isLocalPlayer) => _isLocalPlayer = isLocalPlayer;

        private void OnGUI()
        {
            if (!_visible || !_isLocalPlayer) return;
            var scale = GameMenuStyles.Scale;
            var width = Mathf.Min(Screen.width - 32f, 760f * scale);
            var height = 458f * scale;
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GameMenuStyles.DrawPanel(panel);
            GameMenuStyles.DrawSubtitle(new Rect(panel.x + 32f, panel.y + 22f, panel.width - 64f, 28f), "GUÍA DE CAMPO · CAMIONETA");
            GameMenuStyles.DrawTitle(new Rect(panel.x + 30f, panel.y + 50f, panel.width - 60f, 58f), "ANTES DE ENTRAR");
            GameMenuStyles.DrawRule(panel.x + 38f, panel.y + 116f, panel.width - 76f);

            var text = "WASD: moverte   ·   RATÓN: mirar   ·   SHIFT: correr   ·   C: agacharte\n" +
                       "E: interactuar   ·   J: diario   ·   G: soltar objeto\n" +
                       "1/2/3: elegir herramienta   ·   Q: usarla\n" +
                       "F: linterna   ·   N: visión nocturna   ·   RUEDA: zoom\n\n" +
                       "El fantasma solo aparece a través del lente.\n" +
                       "Revisa el diario antes del ritual. Ve a la puerta trasera y pulsa E para salir.";
            GameMenuStyles.DrawBody(new Rect(panel.x + 50f, panel.y + 136f, panel.width - 100f, 226f * scale), text);
            var buttonWidth = Mathf.Min(panel.width - 100f, 300f * scale);
            if (GameMenuStyles.Button(new Rect(panel.center.x - buttonWidth * 0.5f, panel.yMax - 72f * scale,
                    buttonWidth, 44f * scale), "ENTENDIDO"))
            {
                PlayerPrefs.SetInt(TutorialSeenKey, 1);
                PlayerPrefs.Save();
                _visible = false;
            }
        }
    }
}
