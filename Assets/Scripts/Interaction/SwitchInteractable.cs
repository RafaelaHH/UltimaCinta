using UnityEngine;
namespace UltimaCinta.Interaction
{
    public sealed class SwitchInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Light controlledLight;
        [SerializeField] private GameObject controlledObject;
        private bool _on = true;
        public string Prompt => "E — toggle switch";
        public bool Interact(GameObject interactor) { _on = !_on; if (controlledLight != null) controlledLight.enabled = _on; if (controlledObject != null) controlledObject.SetActive(_on); return true; }
    }
}
