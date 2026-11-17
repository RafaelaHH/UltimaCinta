using UnityEngine;

namespace UltimaCinta.Data
{
    [System.Serializable]
    public sealed class GameAudioConfig
    {
        [Header("Options")]
        [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 0.8f;
        [SerializeField, Min(0.01f)] private float sensitivityMultiplierMinimum = 0.5f;
        [SerializeField, Min(0.01f)] private float sensitivityMultiplierMaximum = 2f;
        [SerializeField, Min(0.01f)] private float defaultSensitivityMultiplier = 1f;
        [SerializeField, Min(0f)] private float subtitleDurationSeconds = 2.5f;

        [Header("3D audio")]
        [SerializeField, Min(0.01f)] private float footstepIntervalSeconds = 0.52f;
        [SerializeField, Min(0.01f)] private float sprintFootstepIntervalSeconds = 0.34f;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.38f;
        [SerializeField, Min(0.1f)] private float footstepMinDistanceMeters = 1f;
        [SerializeField, Min(0.1f)] private float footstepMaxDistanceMeters = 12f;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.48f;
        [SerializeField, Range(0f, 1f)] private float milestoneVolume = 0.62f;
        [SerializeField, Range(0f, 1f)] private float doorVolume = 0.72f;
        [SerializeField, Min(0.1f)] private float doorMinDistanceMeters = 1f;
        [SerializeField, Min(0.1f)] private float doorMaxDistanceMeters = 16f;
        [SerializeField, Range(0f, 1f)] private float ghostVolume = 0.68f;
        [SerializeField, Min(0.1f)] private float ghostMinDistanceMeters = 1.5f;
        [SerializeField, Min(0.1f)] private float ghostMaxDistanceMeters = 22f;
        [SerializeField, Min(0.1f)] private float ghostHuntCueIntervalSeconds = 7f;

        [Header("Miedo")]
        [SerializeField, Min(0f)] private float heartbeatSanityThreshold = 45f;
        [SerializeField, Min(0.01f)] private float heartbeatIntervalAtThresholdSeconds = 1.35f;
        [SerializeField, Min(0.01f)] private float heartbeatIntervalAtZeroSeconds = 0.42f;
        [SerializeField, Range(0f, 1f)] private float heartbeatVolume = 0.62f;
        [SerializeField, Range(0f, 1f)] private float roomToneVolume = 0.07f;

        [Header("Interferencia de cámara")]
        [SerializeField, Range(0f, 1f)] private float maximumFilmGrain = 0.65f;
        [SerializeField, Range(0f, 1f)] private float maximumChromaticAberration = 0.22f;
        [SerializeField, Range(-1f, 1f)] private float maximumLensDistortion = -0.1f;

        public float DefaultMasterVolume => defaultMasterVolume;
        public float SensitivityMultiplierMinimum => sensitivityMultiplierMinimum;
        public float SensitivityMultiplierMaximum => sensitivityMultiplierMaximum;
        public float DefaultSensitivityMultiplier => defaultSensitivityMultiplier;
        public float SubtitleDurationSeconds => subtitleDurationSeconds;
        public float FootstepIntervalSeconds => footstepIntervalSeconds;
        public float SprintFootstepIntervalSeconds => sprintFootstepIntervalSeconds;
        public float FootstepVolume => footstepVolume;
        public float FootstepMinDistanceMeters => footstepMinDistanceMeters;
        public float FootstepMaxDistanceMeters => footstepMaxDistanceMeters;
        public float PickupVolume => pickupVolume;
        public float UiVolume => uiVolume;
        public float MilestoneVolume => milestoneVolume;
        public float DoorVolume => doorVolume;
        public float DoorMinDistanceMeters => doorMinDistanceMeters;
        public float DoorMaxDistanceMeters => doorMaxDistanceMeters;
        public float GhostVolume => ghostVolume;
        public float GhostMinDistanceMeters => ghostMinDistanceMeters;
        public float GhostMaxDistanceMeters => ghostMaxDistanceMeters;
        public float GhostHuntCueIntervalSeconds => ghostHuntCueIntervalSeconds;
        public float HeartbeatSanityThreshold => heartbeatSanityThreshold;
        public float HeartbeatIntervalAtThresholdSeconds => heartbeatIntervalAtThresholdSeconds;
        public float HeartbeatIntervalAtZeroSeconds => heartbeatIntervalAtZeroSeconds;
        public float HeartbeatVolume => heartbeatVolume;
        public float RoomToneVolume => roomToneVolume;
        public float MaximumFilmGrain => maximumFilmGrain;
        public float MaximumChromaticAberration => maximumChromaticAberration;
        public float MaximumLensDistortion => maximumLensDistortion;
    }
}
