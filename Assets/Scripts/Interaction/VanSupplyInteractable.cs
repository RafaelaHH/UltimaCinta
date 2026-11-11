using System.Collections.Generic;
using UltimaCinta.Player;
using UnityEngine;

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class VanSupplyInteractable : MonoBehaviour, IInteractable
    {
        private readonly List<PickupInteractable> _supplies = new List<PickupInteractable>();
        private readonly List<Renderer> _vehicleRenderers = new List<Renderer>();
        private readonly List<bool> _vehicleRendererStates = new List<bool>();
        private readonly List<Collider> _vehicleColliders = new List<Collider>();
        private readonly List<bool> _vehicleColliderStates = new List<bool>();
        private readonly List<PlayerReturnState> _occupants = new List<PlayerReturnState>(3);
        private BoxCollider _vanTrigger;
        private bool _vanTriggerWasEnabled;
        private GameObject _interiorRoot;

        public string Prompt => _occupants.Count == 0 ? "E — abrir acceso de la camioneta" : "E — entrar / salir de la camioneta";

        private void Awake()
        {
            _vanTrigger = GetComponent<BoxCollider>();
            if (_vanTrigger == null)
            {
                _vanTrigger = gameObject.AddComponent<BoxCollider>();
                _vanTrigger.isTrigger = true;
            }
            _vanTriggerWasEnabled = _vanTrigger.enabled;
            CacheSupplies();
            CacheVehicleParts();
            SetSuppliesVisible(false);
            SetVehicleVisible(true);
        }

        public bool Interact(GameObject interactor)
        {
            if (interactor == null) return false;
            var playerTransform = interactor.transform;
            for (var index = 0; index < _occupants.Count; index++)
            {
                if (_occupants[index].Player == playerTransform) return Leave(interactor);
            }
            if (_occupants.Count >= 3)
            {
                interactor.GetComponent<PlayerInventory>()?.ShowMessage("La cabina ya tiene tres investigadores.");
                return false;
            }

            if (_interiorRoot == null) BuildInterior();
            var firstOccupant = _occupants.Count == 0;
            if (firstOccupant)
            {
                SetVehicleVisible(false);
                _interiorRoot.SetActive(true);
                SetSuppliesVisible(true);
            }

            var slot = _occupants.Count;
            var entryRotation = VanCabinBuilder.EntryRotation(_vanTrigger);
            var offset = (slot - 1) * 0.75f;
            var returnPosition = GetEntryPosition() + entryRotation * (Vector3.right * offset);
            _occupants.Add(new PlayerReturnState(playerTransform, returnPosition, entryRotation));
            MovePlayer(playerTransform, VanCabinBuilder.InteriorPosition(_vanTrigger, slot), entryRotation);
            interactor.GetComponent<PlayerInventory>()?.ShowMessage("Cabina abierta · hasta tres investigadores pueden equiparse. E para salir.");
            return true;
        }

        public bool Leave(GameObject interactor)
        {
            if (interactor == null) return false;
            var player = interactor.transform;
            var occupantIndex = -1;
            for (var index = 0; index < _occupants.Count; index++)
                if (_occupants[index].Player == player) { occupantIndex = index; break; }
            if (occupantIndex < 0) return false;

            var state = _occupants[occupantIndex];
            _occupants.RemoveAt(occupantIndex);
            MovePlayer(player, state.ReturnPosition, state.ReturnRotation);
            interactor.GetComponent<PlayerInventory>()?.ShowMessage("Has salido de la camioneta.");
            if (_occupants.Count == 0)
            {
                SetSuppliesVisible(false);
                if (_interiorRoot != null) _interiorRoot.SetActive(false);
                SetVehicleVisible(true);
                if (_vanTrigger != null) _vanTrigger.enabled = _vanTriggerWasEnabled;
            }
            return true;
        }

        private void BuildInterior() => _interiorRoot = VanCabinBuilder.Build(this, _vanTrigger, _supplies);

        private Vector3 GetEntryPosition() => VanCabinBuilder.EntryPosition(_vanTrigger);

        private void MovePlayer(Transform player, Vector3 position, Quaternion rotation)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = true;
        }

        private void CacheSupplies()
        {
            _supplies.Clear();
            foreach (var pickup in GetComponentsInChildren<PickupInteractable>(true))
                if (pickup != null) _supplies.Add(pickup);
        }

        private void CacheVehicleParts()
        {
            _vehicleRenderers.Clear();
            _vehicleRendererStates.Clear();
            _vehicleColliderStates.Clear();
            _vehicleColliders.Clear();
            if (_vanTrigger == null) return;
            var bounds = _vanTrigger.bounds;
            var parent = transform.parent != null ? transform.parent : transform;
            foreach (var renderer in parent.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && !renderer.transform.IsChildOf(transform) &&
                    bounds.Contains(renderer.bounds.min) && bounds.Contains(renderer.bounds.max))
                {
                    _vehicleRenderers.Add(renderer);
                    _vehicleRendererStates.Add(renderer.enabled);
                }
            foreach (var collider in parent.GetComponentsInChildren<Collider>(true))
                if (collider != null && collider != _vanTrigger && !collider.transform.IsChildOf(transform) &&
                    bounds.Contains(collider.bounds.min) && bounds.Contains(collider.bounds.max))
                {
                    _vehicleColliders.Add(collider);
                    _vehicleColliderStates.Add(collider.enabled);
                }
        }

        private void SetVehicleVisible(bool visible)
        {
            for (var index = 0; index < _vehicleRenderers.Count; index++)
                if (_vehicleRenderers[index] != null) _vehicleRenderers[index].enabled = visible && _vehicleRendererStates[index];
            for (var index = 0; index < _vehicleColliders.Count; index++)
                if (_vehicleColliders[index] != null) _vehicleColliders[index].enabled = visible && _vehicleColliderStates[index];
        }

        private void SetSuppliesVisible(bool visible)
        {
            foreach (var pickup in _supplies)
            {
                if (pickup == null) continue;
                foreach (var renderer in pickup.GetComponentsInChildren<Renderer>(true)) renderer.enabled = visible;
                foreach (var collider in pickup.GetComponentsInChildren<Collider>(true)) collider.enabled = visible;
            }
        }

        private void OnDisable()
        {
            for (var index = _occupants.Count - 1; index >= 0; index--)
            {
                var state = _occupants[index];
                if (state.Player != null) MovePlayer(state.Player, state.ReturnPosition, state.ReturnRotation);
            }
            _occupants.Clear();
            SetSuppliesVisible(false);
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            SetVehicleVisible(true);
            if (_vanTrigger != null) _vanTrigger.enabled = _vanTriggerWasEnabled;
        }

        private readonly struct PlayerReturnState
        {
            public readonly Transform Player;
            public readonly Vector3 ReturnPosition;
            public readonly Quaternion ReturnRotation;

            public PlayerReturnState(Transform player, Vector3 returnPosition, Quaternion returnRotation)
            {
                Player = player;
                ReturnPosition = returnPosition;
                ReturnRotation = returnRotation;
            }
        }
    }
}
