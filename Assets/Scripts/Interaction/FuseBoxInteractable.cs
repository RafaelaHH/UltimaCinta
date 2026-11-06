using UnityEngine;

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class FuseBoxInteractable : MonoBehaviour, IInteractable
    {
        private const string FuseItemId = "FUSE";

        [SerializeField] private MansionPowerController generator;

        public string Prompt => generator != null && generator.IsPowered
            ? "Generador activado"
            : "Falta un fusible";

        private void Awake()
        {
            EnsureInteractionCollider();
        }

        public string GetPrompt(GameObject interactor)
        {
            if (generator != null && generator.IsPowered) return "Generador activado";
            var inventory = interactor != null ? interactor.GetComponent<UltimaCinta.Player.PlayerInventory>() : null;
            if (inventory != null)
            {
                for (var slot = 0; slot < inventory.Capacity; slot++)
                {
                    var item = inventory.GetItem(slot);
                    if (item != null && item.DisplayName == FuseItemId) return "E — colocar fusible";
                }
            }
            return "Falta un fusible";
        }

        public void SetGenerator(MansionPowerController controller)
        {
            generator = controller;
        }

        public bool GetDiagnosticState(out bool colliderValid)
        {
            var collider = GetComponent<Collider>();
            colliderValid = collider != null && collider.enabled && gameObject.layer != LayerMask.NameToLayer("Ignore Raycast");
            return colliderValid && generator != null;
        }

        public bool Interact(GameObject interactor)
        {
            if (generator == null)
            {
                Debug.LogError($"Caja eléctrica {name}: no tiene generador asignado.", this);
                return false;
            }
            if (generator.IsPowered)
            {
                Debug.Log($"Caja eléctrica {name}: el generador ya está activado.", this);
                return true;
            }

            var inventory = interactor != null ? interactor.GetComponent<UltimaCinta.Player.PlayerInventory>() : null;
            if (inventory == null || !inventory.TryRemove(FuseItemId))
            {
                inventory?.ShowMessage("Se necesita FUSE");
                Debug.Log($"Caja eléctrica {name}: falta FUSE; el generador sigue apagado.", this);
                return false;
            }

            Debug.Log($"Fusible {FuseItemId} consumido en caja eléctrica {name}.", this);
            generator.Activate();
            return true;
        }

        private void EnsureInteractionCollider()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                Debug.LogError($"Caja eléctrica {name}: faltaba el BoxCollider de interacción; se creó durante Awake.", this);
                box = gameObject.AddComponent<BoxCollider>();
            }
            var renderers = GetComponentsInChildren<Renderer>(true);
            var foundBounds = false;
            var bounds = default(Bounds);
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled) continue;
                if (!foundBounds) { bounds = renderer.bounds; foundBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }

            if (foundBounds)
            {
                var minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                for (var x = 0; x < 2; x++)
                for (var y = 0; y < 2; y++)
                for (var z = 0; z < 2; z++)
                {
                    var corner = new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                        y == 0 ? bounds.min.y : bounds.max.y, z == 0 ? bounds.min.z : bounds.max.z);
                    var local = transform.InverseTransformPoint(corner);
                    minimum = Vector3.Min(minimum, local);
                    maximum = Vector3.Max(maximum, local);
                }
                box.center = (minimum + maximum) * 0.5f;
                box.size = maximum - minimum;
            }
            box.enabled = true;
            box.isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer("Default");
            Debug.Log($"Caja eléctrica {name}: collider de interacción {(foundBounds ? "ajustado" : "conservado")}, trigger={box.isTrigger}, capa={LayerMask.LayerToName(gameObject.layer)}.", this);
        }
    }
}
