using UnityEngine;

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class MansionPowerController : MonoBehaviour
    {
        private bool isPowered;

        public bool IsPowered => isPowered;

        private void Awake()
        {
            isPowered = false;
            ApplyPowerState();
        }

        public void RegisterSwitch(SwitchInteractable roomSwitch)
        {
            if (roomSwitch == null) return;
            roomSwitch.SetPowerAvailable(isPowered);
        }

        public void Activate()
        {
            if (isPowered)
            {
                Debug.Log($"Generador {name}: ya estaba activado.", this);
                return;
            }

            isPowered = true;
            Debug.Log($"Generador {name}: activado; alimentando luces de habitaciones según sus interruptores.", this);
            ApplyPowerState();
        }

        private void ApplyPowerState()
        {
            foreach (var roomSwitch in FindObjectsByType<SwitchInteractable>(FindObjectsInactive.Include))
                if (roomSwitch.gameObject.scene == gameObject.scene)
                    roomSwitch.SetPowerAvailable(isPowered);
        }
    }
}
