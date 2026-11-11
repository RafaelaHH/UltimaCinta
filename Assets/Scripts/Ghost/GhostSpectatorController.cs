using UnityEngine;
using UltimaCinta.Data;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostSpectatorController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private GameConfig gameConfig;
        private void Awake() { if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(); }
        public void SetGameConfig(GameConfig config) => gameConfig = config;
        private void OnGUI()
        {
            GUI.Box(new Rect(Screen.width * 0.5f - 100f, 20f, 200f, 34f), "ESPECTADOR");
            if (GUI.Button(new Rect(Screen.width * 0.5f - 110f, 62f, 220f, 30f), "Parpadear luz [clic]")) BlinkLookedAtLight();
        }
        private void Update()
        {
            if (UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame == true) BlinkLookedAtLight();
        }
        private void BlinkLookedAtLight()
        {
            if (playerCamera == null || gameConfig == null || !Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hit, gameConfig.SpectatorLightRange)) return;
            var light = hit.collider.GetComponentInParent<Light>();
            if (light != null) light.enabled = !light.enabled;
        }
    }
}
