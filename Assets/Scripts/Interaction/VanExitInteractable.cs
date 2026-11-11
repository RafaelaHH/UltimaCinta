using UnityEngine;

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class VanExitInteractable : MonoBehaviour, IInteractable
    {
        private VanSupplyInteractable _van;

        public string Prompt => "E — salir de la camioneta";

        public void SetVan(VanSupplyInteractable van) => _van = van;

        public bool Interact(GameObject interactor) => _van != null && _van.Leave(interactor);
    }
}
