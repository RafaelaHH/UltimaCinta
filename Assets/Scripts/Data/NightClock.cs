using System;
using UnityEngine;
using UltimaCinta.Interaction;
namespace UltimaCinta.Data
{
    [DisallowMultipleComponent]
    public sealed class NightClock : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        private float _elapsedSeconds;
        private NightAct _act;
        private bool _finished;
        private DoorInteractable[] _doors = System.Array.Empty<DoorInteractable>();
        public float ElapsedGameHours => gameConfig == null || gameConfig.RealNightDurationMinutes <= 0f ? 0f :
            Mathf.Clamp01(_elapsedSeconds / (gameConfig.RealNightDurationMinutes * 60f)) * gameConfig.GameNightDurationHours;
        public float ClockHour => (gameConfig == null ? 22f : gameConfig.NightStartHour) + ElapsedGameHours;
        public NightAct Act => _act;
        public event Action<NightAct> ActChanged;
        public event Action<bool> NightEnded;
        public void Configure(GameConfig config) { gameConfig = config; }
        private void Awake() { _doors = FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None); }
        private void Update()
        {
            if (_finished || gameConfig == null || gameConfig.RealNightDurationMinutes <= 0f) return;
            _elapsedSeconds += Time.deltaTime;
            UpdateAct();
            if (_elapsedSeconds >= gameConfig.RealNightDurationMinutes * 60f)
            {
                _finished = true;
                NightEnded?.Invoke(false);
            }
        }
        private void UpdateAct()
        {
            var hours = ElapsedGameHours;
            var next = hours >= HoursFromStart(gameConfig.RitualStartHour) ? NightAct.Ritual :
                hours >= HoursFromStart(gameConfig.BasementUnlockHour) ? NightAct.Identification :
                hours >= HoursFromStart(gameConfig.UpperFloorUnlockHour) ? NightAct.UpperFloor :
                hours >= HoursFromStart(gameConfig.GroundFloorStartHour) ? NightAct.GroundFloor : NightAct.Arrival;
            if (next == _act) return;
            _act = next;
            foreach (var door in _doors)
            {
                if (door.RequiredKey == "KEY_UPPER" && (int)next >= (int)NightAct.UpperFloor) door.UnlockForAct();
                if (door.RequiredKey == "KEY_BASEMENT" && (int)next >= (int)NightAct.Identification) door.UnlockForAct();
            }
            ActChanged?.Invoke(next);
        }
        private float HoursFromStart(float hour)
        {
            var delta = (hour - gameConfig.NightStartHour + 24f) % 24f;
            return delta == 0f ? gameConfig.GameNightDurationHours : delta;
        }
        private void OnGUI()
        {
            var wrapped = ClockHour % 24f;
            var hours = Mathf.FloorToInt(wrapped);
            var minutes = Mathf.FloorToInt((wrapped - hours) * 60f);
            GUI.color = Color.white;
            GUI.Label(new Rect(24f, 188f, 360f, 30f), $"{hours:00}:{minutes:00}  |  ACTO {ActLabel(_act)}");
        }
        private static string ActLabel(NightAct act) => act switch
        {
            NightAct.Arrival => "LLEGADA", NightAct.GroundFloor => "PLANTA BAJA",
            NightAct.UpperFloor => "PRIMER PISO", NightAct.Identification => "IDENTIFICACION", _ => "RITUAL"
        };
        public void FinishVictory() { if (_finished) return; _finished = true; NightEnded?.Invoke(true); }
        public void FinishDeath() { if (_finished) return; _finished = true; NightEnded?.Invoke(false); }
    }
}
