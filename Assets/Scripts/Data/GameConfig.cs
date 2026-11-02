using UnityEngine;
namespace UltimaCinta.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "UltimaCinta/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Night")]
        [SerializeField, Min(0f)] private float realNightDurationMinutes;
        [SerializeField, Min(0f)] private float gameNightDurationHours;
        [SerializeField, Min(0f)] private float gameMinutesPerRealMinute = 2.7f;
        [Header("Camera resources")]
        [SerializeField, Min(0f)] private float batteryDurationMinutes;
        [SerializeField, Min(0f)] private float nightVisionBatteryDurationMinutes;
        [SerializeField, Min(0f)] private float tapeDurationMinutes;
        [SerializeField, Min(1f)] private float nightVisionBatteryMultiplier = 3f;
        [SerializeField, Min(1f)] private float cameraMaximumZoom = 2f;
        [SerializeField, Min(0f)] private float cameraZoomSensitivity = 0.01f;
        [SerializeField, Range(0f, 1f)] private float cameraLowResourceThreshold = 0.2f;
        [Header("Sanity")]
        [SerializeField, Min(0f)] private float startingSanity;
        [SerializeField, Min(0f)] private float maximumSanity;
        [Header("Player")]
        [SerializeField, Min(0f)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 5f;
        [SerializeField, Min(0f)] private float crouchSpeed = 1.7f;
        [SerializeField, Min(0.1f)] private float staminaCapacitySeconds = 8f;
        [SerializeField, Min(0f)] private float staminaDrainPerSecond = 1f;
        [SerializeField, Min(0f)] private float staminaRecoveryPerSecond = 0.8f;
        [SerializeField, Min(0f)] private float lookSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float standingHeight = 1.8f;
        [SerializeField, Min(0f)] private float crouchedHeight = 1.1f;
        [SerializeField, Min(0f)] private float crouchTransitionSpeed = 6f;
        [SerializeField] private float playerGravity = -20f;
        [SerializeField, Min(0f)] private float interactionReachMeters = 2.5f;
        [SerializeField, Min(0f)] private float doorOpenAngle = 90f;
        [SerializeField, Min(0f)] private float doorOpenSpeed = 120f;
        [SerializeField, Min(0f)] private float flashlightRange = 12f;
        [SerializeField, Min(0f)] private float flashlightIntensity = 3f;
        [SerializeField, Min(1f)] private float cameraFieldOfView = 60f;
        public float RealNightDurationMinutes => realNightDurationMinutes;
        public float GameNightDurationHours => gameNightDurationHours;
        public float GameMinutesPerRealMinute => gameMinutesPerRealMinute;
        public float BatteryDurationMinutes => batteryDurationMinutes;
        public float NightVisionBatteryDurationMinutes => nightVisionBatteryDurationMinutes;
        public float TapeDurationMinutes => tapeDurationMinutes;
        public float NightVisionBatteryMultiplier => nightVisionBatteryMultiplier;
        public float CameraMaximumZoom => cameraMaximumZoom;
        public float CameraZoomSensitivity => cameraZoomSensitivity;
        public float CameraLowResourceThreshold => cameraLowResourceThreshold;
        public float StartingSanity => startingSanity;
        public float MaximumSanity => maximumSanity;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float StaminaCapacitySeconds => staminaCapacitySeconds;
        public float StaminaDrainPerSecond => staminaDrainPerSecond;
        public float StaminaRecoveryPerSecond => staminaRecoveryPerSecond;
        public float LookSensitivity => lookSensitivity;
        public float StandingHeight => standingHeight;
        public float CrouchedHeight => crouchedHeight;
        public float CrouchTransitionSpeed => crouchTransitionSpeed;
        public float PlayerGravity => playerGravity;
        public float InteractionReachMeters => interactionReachMeters;
        public float DoorOpenAngle => doorOpenAngle;
        public float DoorOpenSpeed => doorOpenSpeed;
        public float FlashlightRange => flashlightRange;
        public float FlashlightIntensity => flashlightIntensity;
        public float CameraFieldOfView => cameraFieldOfView;
    }
}
