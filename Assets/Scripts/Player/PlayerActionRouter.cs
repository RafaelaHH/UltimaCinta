using UnityEngine;
namespace UltimaCinta.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerActionRouter : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private UltimaCinta.CameraSystem.FlashlightController flashlight;
        [SerializeField] private PlayerInventory inventory;
        private void Awake() { if (input == null) input = GetComponent<PlayerInputReader>(); if (flashlight == null) flashlight = GetComponentInChildren<UltimaCinta.CameraSystem.FlashlightController>(); if (inventory == null) inventory = GetComponent<PlayerInventory>(); }
        private void Update()
        {
            if (input == null) return;
            if (input.FlashlightPressed && flashlight != null) flashlight.Toggle();
            if (input.InventorySlotPressed > 0 && inventory != null) inventory.Select(input.InventorySlotPressed - 1);
            if (input.DropItemPressed && inventory != null) inventory.DropSelected();
            if (input.UseItemPressed && inventory != null) inventory.UseSelected();
        }
    }
}
