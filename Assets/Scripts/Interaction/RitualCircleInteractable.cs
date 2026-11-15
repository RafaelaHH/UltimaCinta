using UnityEngine;
using UltimaCinta.Data;
namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class RitualCircleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private RitualController ritualController;
        public string Prompt => ritualController == null ? "" : ritualController.PlacedItems < 3
            ? "E - colocar objeto del ritual" : ritualController.LitCandles < 3 ? "E - encender vela" : "Ritual en curso";
        public void Configure(RitualController controller) => ritualController = controller;
        public bool Interact(GameObject interactor) => ritualController != null && ritualController.TryPlace(interactor);
    }
}
