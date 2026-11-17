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
        [Networked] public byte MovementState { get; private set; }
        [Networked] public NetworkBool FlashlightOn { get; private set; }
        [Networked] public NetworkBool CameraPowered { get; private set; }
        [Networked] public NetworkBool NightVisionOn { get; private set; }
        [Networked] public float ViewPitch { get; private set; }
        [Networked] public NetworkBool IsDead { get; private set; }
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
                    behaviour is UltimaCinta.UI.PauseMenuController ||
                    behaviour is UltimaCinta.UI.VanTutorialGuide) continue;
                behaviour.enabled = isLocalPlayer;
            }
            GetComponent<UltimaCinta.Audio.PlayerAudioController>()?.ConfigureRole(isLocalPlayer);
            GetComponent<UltimaCinta.UI.PauseMenuController>()?.ConfigureRole(isLocalPlayer);
            GetComponent<UltimaCinta.UI.VanTutorialGuide>()?.ConfigureRole(isLocalPlayer);

            var cameras = GetComponentsInChildren<Camera>(true);
            foreach (var playerCamera in cameras) playerCamera.enabled = isLocalPlayer;
            var listeners = GetComponentsInChildren<AudioListener>(true);
            foreach (var listener in listeners) listener.enabled = isLocalPlayer;
            if (remoteBody != null) remoteBody.SetActive(!isLocalPlayer);
            ApplyRemoteVisuals();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;
            var movement = inputReader != null ? inputReader.Move : Vector2.zero;
            MovementState = inputReader != null && inputReader.CrouchHeld ? (byte)3 :
                movement.sqrMagnitude < 0.01f ? (byte)0 :
                inputReader.SprintHeld ? (byte)2 : (byte)1;
            FlashlightOn = flashlightController != null && flashlightController.IsOn;
            CameraPowered = cameraResources != null && cameraResources.IsPowered;
            NightVisionOn = cameraResources != null && cameraResources.NightVisionEnabled;
            if (viewPivot != null) ViewPitch = Mathf.DeltaAngle(0f, viewPivot.localEulerAngles.x);
        }

        public override void Render()
        {
            if (IsDead) ApplyDeath();
            if (Object.HasStateAuthority) return;
            ApplyRemoteVisuals();
        }

        public void RequestDeath()
        {
            if (Object.HasStateAuthority) IsDead = true;
            else RPC_RequestDeath();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestDeath()
        {
            if (!IsDead) IsDead = true;
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
            var scale = pose.localScale;
            var bob = MovementState == 0 ? 0f : Mathf.Sin(Time.time * 8f) * 0.035f;
            pose.localPosition = new Vector3(0f, 0.9f + bob, 0f);
            scale.y = MovementState == 3 ? 0.62f : 0.9f;
            pose.localScale = scale;
        }
    }
}
