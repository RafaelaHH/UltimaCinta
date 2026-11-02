using UnityEngine;
namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        [SerializeField] private Transform hinge;
        private bool _open;
        private Quaternion _closedRotation;
        public string Prompt => _open ? "E — close door" : "E — open door";
        private void Awake() { if (hinge == null) hinge = transform; _closedRotation = hinge.localRotation; }
        private void Update()
        {
            if (gameConfig == null) return;
            var target = _open ? _closedRotation * Quaternion.Euler(0, gameConfig.DoorOpenAngle, 0) : _closedRotation;
            hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, target, gameConfig.DoorOpenSpeed * Time.deltaTime);
        }
        public bool Interact(GameObject interactor) { _open = !_open; return true; }
    }
}
