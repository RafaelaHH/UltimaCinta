using Fusion;
using UnityEngine;
using UltimaCinta.CameraSystem;

namespace UltimaCinta.Player
{
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerAvatar : NetworkBehaviour
    {
        [SerializeField] private GameObject remoteBody;
        [SerializeField] private Light remoteFlashlight;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform viewPivot;
        [SerializeField] private FlashlightController flashlightController;
        [SerializeField] private CameraResourceController cameraResources;
        [Networked] private byte NetworkMovementState { get; set; }
        public byte MovementState => _networkStarted ? NetworkMovementState : (byte)0;
        [Networked] private NetworkBool NetworkFlashlightOn { get; set; }
        public bool FlashlightOn => _networkStarted && NetworkFlashlightOn;
        [Networked] private NetworkBool NetworkCameraPowered { get; set; }
        public bool CameraPowered => _networkStarted && NetworkCameraPowered;
        [Networked] private NetworkBool NetworkNightVisionOn { get; set; }
        public bool NightVisionOn => _networkStarted && NetworkNightVisionOn;
        [Networked] private float NetworkViewPitch { get; set; }
        public float ViewPitch => _networkStarted ? NetworkViewPitch : 0f;
        [Networked] private NetworkBool NetworkDead { get; set; }
        public bool IsDead => _networkStarted && NetworkDead;
        public static readonly System.Collections.Generic.List<NetworkPlayerAvatar> ActivePlayers = new();
        [Networked] private NetworkBool NetworkInsideVan { get; set; }
        public bool InsideVan => _networkStarted && NetworkInsideVan;
        [Networked] private float NetworkCameraFieldOfView { get; set; }
        public float CameraFieldOfView => _networkStarted ? NetworkCameraFieldOfView : 60f;
        public Transform ViewPivot => viewPivot;
        private Vector3 _bodyPosition;
        private Vector3 _bodyScale;
        private void Awake()
        {
            ActivePlayers.Add(this);
            if (remoteBody != null) { _bodyPosition = remoteBody.transform.localPosition; _bodyScale = remoteBody.transform.localScale; remoteBody.SetActive(false); }
        }
        private void OnDestroy() => ActivePlayers.Remove(this);
        public void SetInsideVan(bool inside) { if (_networkStarted && HasStateAuthority) NetworkInsideVan = inside; }
        private bool _networkStarted;
        private bool _deathReported;
        public bool IsNetworkStarted => _networkStarted;
        public bool IsLocalPlayer => _networkStarted && Object.HasStateAuthority;
        public NetworkRunner ActiveRunner => _networkStarted ? Runner : null;

        public override void Spawned()
        {
            _networkStarted = true;
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (flashlightController == null) flashlightController = GetComponent<FlashlightController>();
            if (cameraResources == null) cameraResources = GetComponent<CameraResourceController>();
            if (viewPivot == null) viewPivot = GetComponentInChildren<Camera>(true)?.transform.parent;

            var isLocalPlayer = Object.HasStateAuthority;
            var localBehaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in localBehaviours)
            {
                if (behaviour == this || behaviour is SimulationBehaviour ||
                    behaviour is UltimaCinta.Audio.PlayerAudioController ||
                    behaviour is CamcorderRecording ||
                    behaviour is UltimaCinta.UI.PauseMenuController ||
                    behaviour is UltimaCinta.UI.VanTutorialGuide) continue;
                behaviour.enabled = isLocalPlayer;
            }
            GetComponent<UltimaCinta.Audio.PlayerAudioController>()?.ConfigureRole(isLocalPlayer);
            GetComponent<UltimaCinta.CameraSystem.FoundFootageEffects>()?.ConfigureRole(isLocalPlayer);
            GetComponent<UltimaCinta.UI.PauseMenuController>()?.ConfigureRole(isLocalPlayer);
            GetComponent<UltimaCinta.UI.VanTutorialGuide>()?.ConfigureRole(isLocalPlayer);

            var cameras = GetComponentsInChildren<Camera>(true);
            foreach (var playerCamera in cameras) playerCamera.enabled = isLocalPlayer;
            var listeners = GetComponentsInChildren<AudioListener>(true);
            foreach (var listener in listeners) listener.enabled = isLocalPlayer;
            if (remoteBody != null)
            {
                remoteBody.SetActive(!isLocalPlayer);
                for (var i = 0; i < remoteBody.transform.childCount; i++)
                    remoteBody.transform.GetChild(i).gameObject.SetActive(i == Object.StateAuthority.AsIndex % remoteBody.transform.childCount);
            }
            ApplyRemoteVisuals();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _networkStarted = false;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;
            var movement = inputReader != null ? inputReader.Move : Vector2.zero;
            NetworkMovementState = inputReader != null && inputReader.CrouchHeld ? (byte)3 :
                movement.sqrMagnitude < 0.01f ? (byte)0 :
                inputReader.SprintHeld ? (byte)2 : (byte)1;
            NetworkFlashlightOn = flashlightController != null && flashlightController.IsOn;
            NetworkCameraPowered = cameraResources != null && cameraResources.IsPowered;
            NetworkCameraFieldOfView = cameraResources != null ? cameraResources.CurrentFieldOfView : 60f;
            NetworkNightVisionOn = cameraResources != null && cameraResources.NightVisionEnabled;
            if (viewPivot != null) NetworkViewPitch = Mathf.DeltaAngle(0f, viewPivot.localEulerAngles.x);
        }

        public override void Render()
        {
            if (IsDead) ApplyDeath();
            if (Object.HasStateAuthority) return;
            ApplyRemoteVisuals();
        }

        public void RequestDeath()
        {
            if (!_networkStarted) return;
            if (Object.HasStateAuthority) NetworkDead = true;
            else RPC_RequestDeath();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestDeath()
        {
            if (!NetworkDead) NetworkDead = true;
        }

        private void ApplyDeath()
        {
            if (HasStateAuthority && !_deathReported)
            {
                _deathReported = true;
                UnityEngine.Object.FindAnyObjectByType<UltimaCinta.Data.NightClock>()?.ReportDeadPlayer();
            }
            var sanity = GetComponent<UltimaCinta.Ghost.SanityController>();
            if (sanity != null && !sanity.IsDead) sanity.Die();
        }

        private void ApplyRemoteVisuals()
        {
            if (remoteFlashlight != null) remoteFlashlight.enabled = FlashlightOn;
            if (remoteFlashlight != null) remoteFlashlight.transform.localRotation = Quaternion.Euler(ViewPitch, 0f, 0f);
            if (remoteBody == null) return;
            var pose = remoteBody.transform;
            var scale = _bodyScale;
            var bob = MovementState == 0 ? 0f : Mathf.Sin(Time.time * 8f) * 0.035f;
            pose.localPosition = _bodyPosition + Vector3.up * bob;
            scale.y *= MovementState == 3 ? 0.69f : 1f;
            pose.localScale = scale;
        }
    }
}
