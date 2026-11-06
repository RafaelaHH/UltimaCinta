using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UltimaCinta.Player;

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class HideoutInteractable : MonoBehaviour, IInteractable
    {
        private const float InteractionColliderMargin = 0.3f;
        private const float BedHiddenControllerHeight = 0.65f;
        private const float HiddenCameraHeight = 0.3f;
        private const float BedLookLimitDegrees = 60f;
        private const float OverlayFadeSeconds = 0.2f;
        private const int OverlayTextureSize = 256;
        [SerializeField] private bool underBed;
        [SerializeField] private string hideoutType = "armario";
        [SerializeField] private Vector2 roomMin;
        [SerializeField] private Vector2 roomMax;
        [SerializeField] private bool hasRoomBounds;

        private GameObject occupant;
        private PlayerInputReader input;
        private CharacterController characterController;
        private FirstPersonMovementController movement;
        private FirstPersonLookController look;
        private PlayerInteractionController interaction;
        private Camera playerCamera;
        private Quaternion originalRotation;
        private Vector3 originalCameraLocalPosition;
        private Quaternion originalCameraLocalRotation;
        private bool characterControllerWasEnabled;
        private float originalCharacterControllerHeight;
        private Vector3 originalCharacterControllerCenter;
        private bool movementWasEnabled;
        private bool lookWasEnabled;
        private bool interactionWasEnabled;
        private readonly List<(Renderer Renderer, bool Enabled)> occupantRenderers = new List<(Renderer, bool)>();
        private readonly List<(Renderer Renderer, bool Enabled)> hideoutRenderers = new List<(Renderer, bool)>();
        private Bounds hideoutBounds;
        private int enteredFrame;
        private Coroutine overlayFade;
        private readonly Collider[] capsuleHits = new Collider[32];
        private static GameObject overlayRoot;
        private static Canvas overlayCanvas;
        private static CanvasGroup overlayGroup;
        private static Image overlayImage;
        private static Sprite closetMask;
        private static Sprite bedMask;

        public string Prompt => occupant != null ? "E — salir" : "E — esconderse";
        public string HideoutType => hideoutType;
        public bool IsUnderBed => underBed;

        private void Awake()
        {
            EnsureInteractionCollider();
            if (!TryGetRendererBounds(out hideoutBounds))
                Debug.LogError($"Escondite {name}: no encuentra su punto de ocultación.", this);
        }

        public void SetUnderBed(bool value)
        {
            underBed = value;
            if (underBed) hideoutType = "cama";
        }

        public void SetHideoutType(string value)
        {
            hideoutType = string.IsNullOrWhiteSpace(value) ? "otro" : value;
        }

        public void SetRoomBounds(Vector2 min, Vector2 max)
        {
            roomMin = min;
            roomMax = max;
            hasRoomBounds = true;
        }

        public bool Interact(GameObject interactor)
        {
            if (occupant != null || interactor == null)
            {
                LogInteraction(false);
                return false;
            }

            var interactionCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>(true);
            if (interactionCollider == null || !interactionCollider.enabled)
            {
                Debug.LogError($"Escondite {name}: no encuentra su collider de interacción.", this);
                LogInteraction(false);
                return false;
            }
            if (!TryGetRendererBounds(out hideoutBounds))
            {
                Debug.LogError($"Escondite {name}: no encuentra su punto de ocultación.", this);
                LogInteraction(false);
                return false;
            }

            input = interactor.GetComponent<PlayerInputReader>();
            if (input == null)
            {
                LogInteraction(false);
                return false;
            }
            characterController = interactor.GetComponent<CharacterController>();
            movement = interactor.GetComponent<FirstPersonMovementController>();
            look = interactor.GetComponent<FirstPersonLookController>();
            interaction = interactor.GetComponent<PlayerInteractionController>();
            playerCamera = interactor.GetComponentInChildren<Camera>(true);
            if (underBed && playerCamera == null)
            {
                Debug.LogError($"Escondite {name}: no encuentra la cámara para el punto de ocultación de la cama.", this);
                LogInteraction(false);
                return false;
            }

            originalRotation = interactor.transform.rotation;
            originalCameraLocalPosition = playerCamera != null ? playerCamera.transform.localPosition : Vector3.zero;
            originalCameraLocalRotation = playerCamera != null ? playerCamera.transform.localRotation : Quaternion.identity;
            characterControllerWasEnabled = characterController != null && characterController.enabled;
            originalCharacterControllerHeight = characterController != null ? characterController.height : 1.8f;
            originalCharacterControllerCenter = characterController != null
                ? characterController.center
                : Vector3.up * (originalCharacterControllerHeight * 0.5f);
            movementWasEnabled = movement != null && movement.enabled;
            lookWasEnabled = look != null && look.enabled;
            interactionWasEnabled = interaction != null && interaction.enabled;

            if (characterController != null) characterController.enabled = false;
            if (movement != null) movement.enabled = false;
            if (look != null && (!underBed || !lookWasEnabled)) look.enabled = false;
            if (interaction != null) interaction.enabled = false;

            if (underBed && characterController != null)
            {
                characterController.height = BedHiddenControllerHeight;
                characterController.center = Vector3.up * (BedHiddenControllerHeight * 0.5f);
            }

            var hidePosition = new Vector3(hideoutBounds.center.x, hideoutBounds.min.y + 0.02f, hideoutBounds.center.z);
            if (underBed && !TryFindBedHidingPosition(out hidePosition))
            {
                Debug.LogError($"Escondite {name}: no encontró un espacio libre bajo la cama; se usó el centro como último recurso.", this);
            }
            interactor.transform.SetPositionAndRotation(hidePosition, transform.rotation);
            if (characterController != null) characterController.enabled = characterControllerWasEnabled;
            if (playerCamera != null && underBed)
            {
                var cameraPosition = playerCamera.transform.position;
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, hideoutBounds.min.x + 0.1f, hideoutBounds.max.x - 0.1f);
                cameraPosition.z = Mathf.Clamp(cameraPosition.z, hideoutBounds.min.z + 0.1f, hideoutBounds.max.z - 0.1f);
                cameraPosition.y = hideoutBounds.min.y + HiddenCameraHeight;
                playerCamera.transform.position = cameraPosition;
            }
            if (underBed && look != null && lookWasEnabled) look.BeginTemporaryLookLimit(BedLookLimitDegrees);

            occupant = interactor;
            enteredFrame = Time.frameCount;
            SetRenderers(interactor.GetComponentsInChildren<Renderer>(true), occupantRenderers, false);
            SetRenderers(GetComponentsInChildren<Renderer>(true), hideoutRenderers, false);
            SetOverlayVisible(true, underBed);
            LogInteraction(true);
            return true;
        }

        private void Update()
        {
            if (occupant == null || input == null || Time.frameCount == enteredFrame) return;
            if (input.InteractPressed) Exit();
        }

        private void OnGUI()
        {
            if (occupant == null) return;
            GUI.Label(new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.72f, 200f, 28f), "E — salir");
        }

        private void Exit()
        {
            Vector3 position;
            if (underBed)
            {
                if (!TryFindFreeBedExit(out position))
                {
                    Debug.LogError($"Escondite {name}: no encuentra un lado libre y seguro para salir de la cama.", this);
                    return;
                }
            }
            else
            {
                var distance = hideoutBounds.extents.z + 0.7f;
                position = transform.position + transform.forward * distance;
                position.y = hideoutBounds.min.y + 0.02f;
            }

            if (characterController != null) characterController.enabled = false;
            occupant.transform.SetPositionAndRotation(position, originalRotation);
            if (look != null) look.EndTemporaryLookLimit();
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = originalCameraLocalPosition;
                playerCamera.transform.localRotation = originalCameraLocalRotation;
            }
            if (characterController != null)
            {
                characterController.height = originalCharacterControllerHeight;
                characterController.center = originalCharacterControllerCenter;
                characterController.enabled = characterControllerWasEnabled;
            }
            RestoreRenderers(occupantRenderers);
            RestoreRenderers(hideoutRenderers);
            if (movement != null) movement.enabled = movementWasEnabled;
            if (look != null) look.enabled = lookWasEnabled;
            if (interaction != null) interaction.enabled = interactionWasEnabled;
            SetOverlayVisible(false, underBed);
            LogInteraction(false);
            occupant = null;
            input = null;
        }

        private bool TryFindFreeBedExit(out Vector3 position)
        {
            var radius = characterController != null ? characterController.radius : 0.3f;
            var bestBlockers = int.MaxValue;
            var foundGround = false;
            var bestPosition = hideoutBounds.center + transform.forward * (hideoutBounds.extents.z + radius + 0.15f);
            bestPosition.y = hideoutBounds.min.y + 0.02f;
            var wasEnabled = characterController != null && characterController.enabled;
            if (characterController != null) characterController.enabled = false;
            var directions = new[] { transform.right, -transform.right, transform.forward, -transform.forward };
            foreach (var direction in directions)
            {
                var extent = Mathf.Abs(direction.x) > 0.5f ? hideoutBounds.extents.x : hideoutBounds.extents.z;
                var candidate = hideoutBounds.center + direction * (extent + radius + 0.15f);
                candidate.y = hideoutBounds.min.y + 0.02f;
                if (hasRoomBounds && (candidate.x - radius < roomMin.x || candidate.x + radius > roomMax.x ||
                                      candidate.z - radius < roomMin.y || candidate.z + radius > roomMax.y)) continue;

                var rayOrigin = candidate + Vector3.up * 1.4f;
                if (!Physics.Raycast(rayOrigin, Vector3.down, out _, 1.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                foundGround = true;
                var blockers = CountCapsuleBlockers(candidate, radius, originalCharacterControllerHeight,
                    originalCharacterControllerCenter, originalRotation, occupant);
                if (blockers >= bestBlockers) continue;
                bestBlockers = blockers;
                bestPosition = candidate;
                if (blockers == 0) break;
            }

            if (characterController != null) characterController.enabled = wasEnabled;
            if (!foundGround)
            {
                Debug.LogError($"Escondite {name}: no encontró suelo libre; sale por el lado más despejado disponible.", this);
                var direction = transform.forward;
                bestPosition = hideoutBounds.center + direction * (hideoutBounds.extents.z + radius + 0.15f);
                bestPosition.y = hideoutBounds.min.y + 0.02f;
            }
            else if (bestBlockers > 0)
            {
                Debug.LogError($"Escondite {name}: todos los lados de la cama están ocupados; sale por el lado con menos obstáculos ({bestBlockers}).", this);
            }
            position = bestPosition;
            return true;
        }

        private bool TryFindBedHidingPosition(out Vector3 position)
        {
            var radius = characterController != null ? characterController.radius : 0.3f;
            var insetX = Mathf.Min(radius + 0.05f, hideoutBounds.extents.x * 0.8f);
            var insetZ = Mathf.Min(radius + 0.05f, hideoutBounds.extents.z * 0.8f);
            var minX = hideoutBounds.min.x + insetX;
            var maxX = hideoutBounds.max.x - insetX;
            var minZ = hideoutBounds.min.z + insetZ;
            var maxZ = hideoutBounds.max.z - insetZ;
            var y = hideoutBounds.min.y + 0.02f;
            var fractions = new[] { 0.5f, 0.25f, 0.75f };
            foreach (var xFraction in fractions)
            foreach (var zFraction in fractions)
            {
                var candidate = new Vector3(Mathf.Lerp(minX, maxX, xFraction), y, Mathf.Lerp(minZ, maxZ, zFraction));
                if (OutsideRoomBounds(candidate, radius)) continue;
                if (CountCapsuleBlockers(candidate, radius, BedHiddenControllerHeight,
                        Vector3.up * (BedHiddenControllerHeight * 0.5f), originalRotation, occupant) > 0) continue;
                position = candidate;
                return true;
            }
            position = default;
            return false;
        }

        private bool OutsideRoomBounds(Vector3 candidate, float radius)
        {
            return hasRoomBounds && (candidate.x - radius < roomMin.x || candidate.x + radius > roomMax.x ||
                                     candidate.z - radius < roomMin.y || candidate.z + radius > roomMax.y);
        }

        private void SetOverlayVisible(bool visible, bool bed)
        {
            if (!visible && overlayRoot == null) return;
            EnsureOverlay();
            overlayImage.sprite = bed ? bedMask : closetMask;
            overlayRoot.SetActive(true);
            if (overlayFade != null) StopCoroutine(overlayFade);
            overlayFade = StartCoroutine(FadeOverlay(visible));
        }

        private IEnumerator FadeOverlay(bool visible)
        {
            overlayCanvas.enabled = true;
            overlayGroup.alpha = visible ? 0f : overlayGroup.alpha;
            var start = overlayGroup.alpha;
            var target = visible ? 1f : 0f;
            var elapsed = 0f;
            while (elapsed < OverlayFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                overlayGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / OverlayFadeSeconds));
                yield return null;
            }
            overlayGroup.alpha = target;
            if (!visible) overlayRoot.SetActive(false);
            overlayFade = null;
        }

        private static void EnsureOverlay()
        {
            if (overlayRoot != null) return;
            if (closetMask != null) Destroy(closetMask.texture);
            if (bedMask != null) Destroy(bedMask.texture);
            if (closetMask != null) Destroy(closetMask);
            if (bedMask != null) Destroy(bedMask);

            overlayRoot = new GameObject("HideoutOverlay_Runtime");
            overlayCanvas = overlayRoot.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = -1000;
            overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;

            var imageObject = new GameObject("Mask");
            imageObject.transform.SetParent(overlayRoot.transform, false);
            overlayImage = imageObject.AddComponent<Image>();
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = false;
            var rect = overlayImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            closetMask = CreateMask(false);
            bedMask = CreateMask(true);
            overlayCanvas.enabled = false;
            overlayRoot.SetActive(false);
        }

        private static Sprite CreateMask(bool bed)
        {
            var texture = new Texture2D(OverlayTextureSize, OverlayTextureSize, TextureFormat.RGBA32, false, true)
            {
                name = bed ? "BedHideoutMask_Runtime" : "ClosetHideoutMask_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color[OverlayTextureSize * OverlayTextureSize];
            var halfWidth = bed ? 0.39f : 0.035f;
            var halfHeight = bed ? 0.13f : 0.46f;
            var centerY = bed ? 0.38f : 0.5f;
            var feather = 0.035f;
            for (var y = 0; y < OverlayTextureSize; y++)
            for (var x = 0; x < OverlayTextureSize; x++)
            {
                var dx = Mathf.Abs((x + 0.5f) / OverlayTextureSize - 0.5f) - halfWidth;
                var dy = Mathf.Abs((y + 0.5f) / OverlayTextureSize - centerY) - halfHeight;
                var outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                var inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                var alpha = Mathf.SmoothStep(0f, 0.94f, Mathf.InverseLerp(-feather, feather, outside + inside));
                pixels[y * OverlayTextureSize + x] = new Color(0f, 0f, 0f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, OverlayTextureSize, OverlayTextureSize), Vector2.one * 0.5f);
            sprite.name = bed ? "BedHideoutMask_Runtime" : "ClosetHideoutMask_Runtime";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private int CountCapsuleBlockers(Vector3 feetPosition, float radius, float height, Vector3 localCenter,
            Quaternion rotation, GameObject ignoreOccupant)
        {
            var center = feetPosition + rotation * localCenter;
            var halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            var pointA = center + Vector3.up * halfSegment;
            var pointB = center - Vector3.up * halfSegment;
            var count = Physics.OverlapCapsuleNonAlloc(pointA, pointB, radius, capsuleHits,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var blockers = 0;
            for (var index = 0; index < count; index++)
            {
                var collider = capsuleHits[index];
                capsuleHits[index] = null;
                if (collider == null || collider.transform == transform || collider.transform.IsChildOf(transform)) continue;
                if (ignoreOccupant != null && (collider.transform == ignoreOccupant.transform || collider.transform.IsChildOf(ignoreOccupant.transform))) continue;
                blockers++;
            }
            return blockers;
        }

        private bool TryGetRendererBounds(out Bounds bounds)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            var found = false;
            bounds = default;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        public bool GetDiagnosticState(out bool colliderValid, out bool anchorValid)
        {
            var box = GetComponent<BoxCollider>();
            colliderValid = box != null && box.enabled && box.isTrigger && box.gameObject.layer != LayerMask.NameToLayer("Ignore Raycast");
            anchorValid = TryGetRendererBounds(out _);
            return colliderValid && anchorValid;
        }

        public void EnsureRuntimeSetup()
        {
            EnsureInteractionCollider();
            if (!TryGetRendererBounds(out hideoutBounds))
                Debug.LogError($"Escondite {name}: no encuentra su punto de ocultación.", this);
        }

        private void EnsureInteractionCollider()
        {
            if (!TryGetRendererBounds(out var bounds))
            {
                Debug.LogError($"Escondite {name}: no encuentra su punto de ocultación para crear el collider.", this);
                return;
            }

            bounds.Expand(Vector3.one * (InteractionColliderMargin * 2f));

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

            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                Debug.LogError($"Escondite {name}: faltaba el BoxCollider de interacción; se creó durante Awake.", this);
                box = gameObject.AddComponent<BoxCollider>();
            }
            box.center = (minimum + maximum) * 0.5f;
            box.size = maximum - minimum;
            box.isTrigger = true;
            box.enabled = true;
            gameObject.layer = LayerMask.NameToLayer("Default");

            foreach (var childCollider in GetComponentsInChildren<Collider>(true))
                if (childCollider != box) childCollider.enabled = false;
        }

        private void LogInteraction(bool entering)
        {
            Debug.Log($"Escondite {name} tipo={hideoutType} entrar={entering.ToString().ToLowerInvariant()}", this);
        }

        private static void SetRenderers(Renderer[] renderers, List<(Renderer Renderer, bool Enabled)> states, bool enabled)
        {
            states.Clear();
            foreach (var renderer in renderers)
            {
                states.Add((renderer, renderer.enabled));
                renderer.enabled = enabled;
            }
        }

        private static void RestoreRenderers(List<(Renderer Renderer, bool Enabled)> states)
        {
            foreach (var state in states)
                if (state.Renderer != null) state.Renderer.enabled = state.Enabled;
            states.Clear();
        }
    }
}
