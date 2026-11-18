using UnityEngine;
using Fusion;
using UltimaCinta.Audio;
using UltimaCinta.UI;
namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DoorInteractable : NetworkBehaviour, IInteractable
    {
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        [SerializeField] private Transform hinge;
        [SerializeField] private string keyId = "NONE";
        [SerializeField] private float swingSign = 1f;
        [SerializeField] private float openAngle = -1f;
        private bool _open;
        private bool _slamming;
        private int _lastSlam;
        [Networked] private int NetworkSlamSequence { get; set; }
        private bool _unlocked;
        private Quaternion _closedRotation;
        private bool _networkStarted;
        private bool _stateInitialized;
        private bool _renderedOpen;
        private AudioSource _audioSource;
        [Networked] private NetworkBool NetworkOpen { get; set; }
        [Networked] private NetworkBool NetworkUnlocked { get; set; }
        public string Prompt => _open ? "E — close door" : !_unlocked ? $"E — unlock with {keyId}" : "E — open door";
        public void ForceSlamFromGhost()
        {
            if (!_unlocked || !_open) return;
            if (!_networkStarted) { _open = false; BeginSlam(); return; }
            if (HasStateAuthority) { NetworkOpen = false; NetworkSlamSequence++; }
            else RPC_ForceClosed();
        }
        public void UnlockForAct()
        {
            if (!_networkStarted) { _unlocked = true; return; }
            if (HasStateAuthority) NetworkUnlocked = true;
            else RPC_Unlock();
        }
        public string RequiredKey => keyId;
        private void Awake()
        {
            if (hinge == null) hinge = transform;
            _closedRotation = hinge.localRotation;
            _unlocked = keyId == "NONE";
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.dopplerLevel = 0f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
        public override void Spawned()
        {
            _networkStarted = true;
            if (HasStateAuthority)
            {
                NetworkOpen = _open;
                NetworkUnlocked = _unlocked;
            }
            ApplyNetworkState();
            _renderedOpen = _open;
            _lastSlam = NetworkSlamSequence;
            _stateInitialized = true;
        }
        public override void Render() { if (_networkStarted) ApplyNetworkState(); }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestToggle(NetworkBool unlockWithKey)
        {
            if (!NetworkUnlocked && !unlockWithKey) return;
            NetworkUnlocked = true;
            NetworkOpen = !NetworkOpen;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_Unlock() { NetworkUnlocked = true; }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_ForceClosed() { if (NetworkUnlocked && NetworkOpen) { NetworkOpen = false; NetworkSlamSequence++; } }

        private void ApplyNetworkState()
        {
            if (_lastSlam != NetworkSlamSequence) { _lastSlam = NetworkSlamSequence; BeginSlam(); }
            _open = NetworkOpen;
            _unlocked = NetworkUnlocked;
        }
        private void Update()
        {
            if (!_stateInitialized)
            {
                _renderedOpen = _open;
                _stateInitialized = true;
            }
            else if (_renderedOpen != _open)
            {
                _renderedOpen = _open;
                if (!_slamming) PlayDoorCue();
            }

            if (gameConfig == null) return;
            _audioSource.minDistance = gameConfig.Audio.DoorMinDistanceMeters;
            _audioSource.maxDistance = gameConfig.Audio.DoorMaxDistanceMeters;
            var angle = openAngle > 0f ? openAngle : gameConfig.DoorOpenAngle;
            var target = _open ? _closedRotation * Quaternion.Euler(0f, angle * swingSign, 0f) : _closedRotation;
            hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, target, (_slamming && !_open ? gameConfig.Atmosphere.DoorSlamSpeed : gameConfig.DoorOpenSpeed) * Time.deltaTime);
            if (Quaternion.Angle(hinge.localRotation, target) < 0.01f) _slamming = false;
        }

        private void BeginSlam()
        {
            _slamming = true;
            if (gameConfig == null || _audioSource == null) return;
            _audioSource.PlayOneShot(AudioCueLibrary.Impact(), gameConfig.Atmosphere.ImpactVolume);
            SubtitleOverlay.ShowAt(transform.position, gameConfig.Audio.DoorMaxDistanceMeters, "�Un portazo violento!");
        }

        private void PlayDoorCue()
        {
            if (gameConfig == null || _audioSource == null) return;
            _audioSource.PlayOneShot(AudioCueLibrary.Door(_open), gameConfig.Audio.DoorVolume);
            SubtitleOverlay.ShowAt(transform.position, gameConfig.Audio.DoorMaxDistanceMeters,
                _open ? "La puerta se abre con un gemido." : "La puerta se cierra de golpe.");
        }
        public bool Interact(GameObject interactor)
        {
            var inventory = interactor == null ? null : interactor.GetComponent<UltimaCinta.Player.PlayerInventory>();
            var consumesKey = !_unlocked && keyId != "NONE";
            if (consumesKey && !HasKey(inventory, keyId))
            {
                inventory?.ShowMessage($"Se necesita {keyId}");
                return false;
            }
            if (_networkStarted)
            {
                if (HasStateAuthority)
                {
                    NetworkUnlocked = true;
                    NetworkOpen = !NetworkOpen;
                }
                else RPC_RequestToggle(consumesKey);
            }
            else
            {
                _unlocked = true;
                _open = !_open;
            }
            if (consumesKey)
            {
                var remainingUses = CountLockedDoors(keyId);
                if (remainingUses == 0)
                {
                    if (inventory == null || !inventory.TryRemove(keyId))
                    {
                        Debug.LogError($"Door {name}: no se pudo retirar {keyId} del inventario tras desbloquear.", this);
                    }
                    else Debug.Log($"Llave {keyId} usada en puerta {DoorId}; eliminada del inventario", this);
                }
                else Debug.Log($"Llave {keyId} usada en puerta {DoorId}; quedan {remainingUses} usos", this);
            }
            return true;
        }

        private string DoorId
        {
            get
            {
                var separator = name.IndexOf(" [", System.StringComparison.Ordinal);
                return separator < 0 ? name : name.Substring(0, separator);
            }
        }

        private static bool HasKey(UltimaCinta.Player.PlayerInventory inventory, string requiredKey)
        {
            if (inventory == null) return false;
            for (var slot = 0; slot < inventory.Capacity; slot++)
            {
                var item = inventory.GetItem(slot);
                if (item != null && item.DisplayName == requiredKey) return true;
            }
            return false;
        }

        private static int CountLockedDoors(string requiredKey)
        {
            var count = 0;
            foreach (var door in FindObjectsOfType<DoorInteractable>(true))
                if (!door._unlocked && door.keyId == requiredKey) count++;
            return count;
        }
    }
}
