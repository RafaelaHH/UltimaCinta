using UnityEngine;
using UnityEngine.InputSystem;
namespace UltimaCinta.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class PhaseOneResourceDebugKeys : MonoBehaviour
    {
        [SerializeField] private CameraResourceController resources;
        private void Awake() { if (resources == null) resources = GetComponent<CameraResourceController>(); }
        private void Update()
        {
            if (Keyboard.current == null || resources == null) return;
            if (Keyboard.current.f6Key.wasPressedThisFrame) resources.DebugSetBatteryNormalized(0.01f);
            if (Keyboard.current.f7Key.wasPressedThisFrame) resources.DebugSetTapeNormalized(0.01f);
            if (Keyboard.current.f8Key.wasPressedThisFrame) resources.DebugRestoreResources();
        }
    }
}
