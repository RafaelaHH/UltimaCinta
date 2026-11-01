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

        [Header("Sanity")]
        [SerializeField, Min(0f)] private float startingSanity;
        [SerializeField, Min(0f)] private float maximumSanity;

        public float RealNightDurationMinutes => realNightDurationMinutes;
        public float GameNightDurationHours => gameNightDurationHours;
        public float GameMinutesPerRealMinute => gameMinutesPerRealMinute;
        public float BatteryDurationMinutes => batteryDurationMinutes;
        public float NightVisionBatteryDurationMinutes => nightVisionBatteryDurationMinutes;
        public float TapeDurationMinutes => tapeDurationMinutes;
        public float StartingSanity => startingSanity;
        public float MaximumSanity => maximumSanity;
    }
}
