using UnityEngine;
namespace UltimaCinta.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool Interact(GameObject interactor);
    }
}
