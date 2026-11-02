using UltimaCinta.Player;
using UnityEngine;
namespace UltimaCinta.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraInput : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CameraResourceController resources;
        private void Awake() { if (input == null) input = GetComponent<PlayerInputReader>(); if (resources == null) resources = GetComponent<CameraResourceController>(); }
        private void Update() { if (input == null || resources == null) return; if (input.NightVisionPressed) resources.ToggleNightVision(); resources.SetZoom(input.ZoomDelta); resources.Tick(Time.deltaTime); }
    }
}
