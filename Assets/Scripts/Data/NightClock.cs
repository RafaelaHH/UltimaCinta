using System;
using Fusion;
using UnityEngine;
using UltimaCinta.Interaction;

namespace UltimaCinta.Data
{
    [DisallowMultipleComponent]
    public sealed class NightClock : NetworkBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [Networked] private float NetworkElapsedSeconds { get; set; }
        [Networked] private byte NetworkAct { get; set; }
        [Networked] private NetworkBool NetworkFinished { get; set; }
        [Networked] private NetworkBool NetworkVictory { get; set; }
        [Networked] private int NetworkDeadPlayers { get; set; }
        [Networked] private int NetworkTotalPlayers { get; set; }
        [Networked] private int NetworkDisconnectedMask { get; set; }
        private float _localElapsedSeconds;
        private NightAct _localAct;
        private bool _localFinished;
        private bool _networkStarted;
        private bool _lastStateAuthority;
        private NightAct _renderedAct;
        private bool _renderedFinished;
        private DoorInteractable[] _doors = Array.Empty<DoorInteractable>();

        private float ElapsedSeconds => _networkStarted ? NetworkElapsedSeconds : _localElapsedSeconds;
        private NightAct CurrentAct => _networkStarted ? (NightAct)NetworkAct : _localAct;
        private bool IsFinished => _networkStarted ? NetworkFinished : _localFinished;
        public float ElapsedGameHours => gameConfig == null || gameConfig.RealNightDurationMinutes <= 0f ? 0f :
            Mathf.Clamp01(ElapsedSeconds / (gameConfig.RealNightDurationMinutes * 60f)) * gameConfig.GameNightDurationHours;
        public float ClockHour => (gameConfig == null ? 22f : gameConfig.NightStartHour) + ElapsedGameHours;
        public NightAct Act => CurrentAct;
        public bool IsEveryoneDead => _networkStarted
            ? NetworkTotalPlayers > 0 && NetworkDeadPlayers >= NetworkTotalPlayers
            : IsLocalPartyDead();
        public event Action<NightAct> ActChanged;
        public event Action<bool> NightEnded;

        public void Configure(GameConfig config) { gameConfig = config; }

        private void Awake()
        {
            _doors = FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None);
        }

        public override void Spawned()
        {
            _networkStarted = true;
            _lastStateAuthority = HasStateAuthority;
            _renderedAct = CurrentAct;
            if (HasStateAuthority)
            {
                NetworkElapsedSeconds = 0f;
                NetworkAct = (byte)NightAct.Arrival;
                NetworkFinished = false;
                NetworkVictory = false;
                NetworkDeadPlayers = 0;
                NetworkTotalPlayers = 1;
                NetworkDisconnectedMask = 0;
            }
            ApplyAct(CurrentAct);
        }

        public override void FixedUpdateNetwork()
        {
            if (!_networkStarted) return;
            if (HasStateAuthority != _lastStateAuthority)
            {
                _lastStateAuthority = HasStateAuthority;
                Debug.Log($"[Fusion] Autoridad del reloj transferida: activa={_lastStateAuthority}.", this);
            }
            if (!HasStateAuthority || NetworkFinished || gameConfig == null ||
                gameConfig.RealNightDurationMinutes <= 0f) return;

            NetworkElapsedSeconds += Runner.DeltaTime;
            var nextAct = CalculateAct();
            if (nextAct != (NightAct)NetworkAct) NetworkAct = (byte)nextAct;
            if (NetworkElapsedSeconds >= gameConfig.RealNightDurationMinutes * 60f)
            {
                NetworkFinished = true;
                NetworkVictory = false;
            }
        }

        public override void Render()
        {
            if (!_networkStarted) return;
            var nextAct = CurrentAct;
            if (nextAct != _renderedAct)
            {
                _renderedAct = nextAct;
                ApplyAct(nextAct);
                ActChanged?.Invoke(nextAct);
            }
            if (IsFinished && !_renderedFinished)
            {
                _renderedFinished = true;
                NightEnded?.Invoke(NetworkVictory);
            }
        }

        private void Update()
        {
            if (_networkStarted || _localFinished || gameConfig == null || gameConfig.RealNightDurationMinutes <= 0f) return;
            _localElapsedSeconds += Time.deltaTime;
            var nextAct = CalculateAct();
            if (nextAct != _localAct)
            {
                _localAct = nextAct;
                ApplyAct(nextAct);
                ActChanged?.Invoke(nextAct);
            }
            if (_localElapsedSeconds >= gameConfig.RealNightDurationMinutes * 60f)
            {
                _localFinished = true;
                NightEnded?.Invoke(false);
            }
        }

        private NightAct CalculateAct()
        {
            var hours = ElapsedGameHours;
            return hours >= HoursFromStart(gameConfig.RitualStartHour) ? NightAct.Ritual :
                hours >= HoursFromStart(gameConfig.BasementUnlockHour) ? NightAct.Identification :
                hours >= HoursFromStart(gameConfig.UpperFloorUnlockHour) ? NightAct.UpperFloor :
                hours >= HoursFromStart(gameConfig.GroundFloorStartHour) ? NightAct.GroundFloor : NightAct.Arrival;
        }

        private void ApplyAct(NightAct act)
        {
            foreach (var door in _doors)
            {
                if (door == null) continue;
                if (door.RequiredKey == "KEY_UPPER" && (int)act >= (int)NightAct.UpperFloor) door.UnlockForAct();
                if (door.RequiredKey == "KEY_BASEMENT" && (int)act >= (int)NightAct.Identification) door.UnlockForAct();
            }
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
            GUI.Label(new Rect(24f, 188f, 360f, 30f), $"{hours:00}:{minutes:00}  |  ACTO {ActLabel(CurrentAct)}");
        }

        private static string ActLabel(NightAct act) => act switch
        {
            NightAct.Arrival => "LLEGADA", NightAct.GroundFloor => "PLANTA BAJA",
            NightAct.UpperFloor => "PRIMER PISO", NightAct.Identification => "IDENTIFICACION", _ => "RITUAL"
        };

        public void FinishVictory() => Finish(true);
        public void FinishDeath() => Finish(false);

        public void RefreshPlayerCount(int count)
        {
            if (_networkStarted && HasStateAuthority) NetworkTotalPlayers = Mathf.Max(1, count);
        }

        public void ReportDeadPlayer()
        {
            if (!_networkStarted || NetworkFinished) return;
            if (!HasStateAuthority)
            {
                RPC_ReportDeadPlayer();
                return;
            }
            ApplyDeadPlayer();
        }

        public void ReportDisconnectedPlayer(int playerId)
        {
            if (!_networkStarted || NetworkFinished) return;
            if (HasStateAuthority) ApplyDisconnectedPlayer(playerId);
            else RPC_ReportDisconnectedPlayer(playerId);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_ReportDeadPlayer()
        {
            ApplyDeadPlayer();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_ReportDisconnectedPlayer(int playerId)
        {
            ApplyDisconnectedPlayer(playerId);
        }

        private void ApplyDisconnectedPlayer(int playerId)
        {
            if (!HasStateAuthority || NetworkFinished || playerId < 0 || playerId >= 31) return;
            var bit = 1 << playerId;
            if ((NetworkDisconnectedMask & bit) != 0) return;
            NetworkDisconnectedMask |= bit;
            ApplyDeadPlayer();
        }

        private void ApplyDeadPlayer()
        {
            if (!HasStateAuthority || NetworkFinished) return;
            NetworkDeadPlayers++;
            if (NetworkDeadPlayers >= Mathf.Max(1, NetworkTotalPlayers)) Finish(false);
        }

        private bool IsLocalPartyDead()
        {
            var players = FindObjectsByType<UltimaCinta.Ghost.SanityController>(FindObjectsSortMode.None);
            if (players.Length == 0) return false;
            foreach (var player in players) if (player != null && !player.IsDead) return false;
            return true;
        }

        private void Finish(bool victory)
        {
            if (_networkStarted)
            {
                if (!HasStateAuthority || NetworkFinished) return;
                NetworkVictory = victory;
                NetworkFinished = true;
                return;
            }
            if (_localFinished) return;
            _localFinished = true;
            NightEnded?.Invoke(victory);
        }
    }
}
