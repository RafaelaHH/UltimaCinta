using UnityEngine;

namespace UltimaCinta.Data
{
    [System.Serializable]
    public sealed class GameAtmosphereConfig
    {
        [field: SerializeField, Min(0.1f)] public float DoorSlamRadius { get; private set; } = 3f;
        [field: SerializeField, Min(1f)] public float DoorSlamCooldown { get; private set; } = 12f;
        [field: SerializeField, Min(1f)] public float DoorSlamSpeed { get; private set; } = 850f;
        [field: SerializeField, Range(0f, 1f)] public float ImpactVolume { get; private set; } = 0.85f;
        [field: SerializeField, Min(1f)] public float HeartbeatRange { get; private set; } = 15f;
        [field: SerializeField, Min(1f)] public float FlickerRange { get; private set; } = 9f;
        [field: SerializeField, Min(0.01f)] public float FlickerInterval { get; private set; } = 0.12f;
        [field: SerializeField, Min(1f)] public float NavigationRefresh { get; private set; } = 4f;
        [field: SerializeField, Min(1f)] public float PropEventInterval { get; private set; } = 16f;
        [field: SerializeField, Min(1f)] public float CabinWidth { get; private set; } = 3.6f;
        [field: SerializeField, Min(1f)] public float CabinLength { get; private set; } = 6.4f;
        [field: SerializeField, Min(2f)] public float CabinHeight { get; private set; } = 2.8f;
        [field: SerializeField, Min(1f)] public float RecordingFramesPerSecond { get; private set; } = 2f;
        [field: SerializeField, Min(128)] public int RecordingWidth { get; private set; } = 320;
        [field: SerializeField, Range(25, 90)] public int RecordingJpegQuality { get; private set; } = 55;
        [field: SerializeField, Min(128)] public int RecordingBudgetMegabytes { get; private set; } = 2048;
        [field: SerializeField, Min(10f)] public float MonitorFarClip { get; private set; } = 45f;
        [field: SerializeField, Min(1f)] public float LightCullDistance { get; private set; } = 24f;
        [field: SerializeField, Min(30)] public int TargetFrameRate { get; private set; } = 60;
    }
}
