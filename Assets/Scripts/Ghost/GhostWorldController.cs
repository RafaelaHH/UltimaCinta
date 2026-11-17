using UnityEngine;
using UnityEngine.AI;
using Fusion;
using System;
using System.Linq;
using UltimaCinta.Data;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostWorldController : NetworkBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private GhostType[] ghostTypes;
        [SerializeField] private GhostRoomMarker[] favoriteRoomMarkers;
        [SerializeField] private GhostType forcedGhostForTesting;
        [SerializeField] private bool randomizeAtStart = true;
        [Networked] private int NetworkTypeIndex { get; set; }
        [Networked] private int NetworkRoomIndex { get; set; }
        [Networked] private float NetworkAggression { get; set; }
        [Networked] private int NetworkEvidenceMask { get; set; }
        [Networked] private int NetworkSelectedGhostIndex { get; set; }
        [Networked] private byte NetworkRitualPlacedItems { get; set; }
        [Networked] private byte NetworkRitualLitCandles { get; set; }
        [Networked] private byte NetworkRitualWave { get; set; }
        [Networked] private float NetworkRitualWaveElapsed { get; set; }
        [Networked] private NetworkBool NetworkRitualActive { get; set; }
        [Networked] private NetworkBool NetworkRitualWavesComplete { get; set; }
        [Networked] private NetworkBool NetworkRitualFailed { get; set; }
        [Networked] private NetworkBool NetworkRitualVictory { get; set; }
        [Networked] private NetworkBool NetworkRitualItem0Picked { get; set; }
        [Networked] private NetworkBool NetworkRitualItem1Picked { get; set; }
        [Networked] private NetworkBool NetworkRitualItem2Picked { get; set; }
        private GhostAgent _agent;
        private GhostType _localType;
        private GhostRoomMarker _localFavoriteRoom;
        private float _localAggression;
        private Transform[] _players = System.Array.Empty<Transform>();
        private bool _activated;
        private bool _networkStarted;
        private bool _lastStateAuthority;
        private float _playerRefreshTimer;
        private int _localEvidenceMask;
        private GhostType _localSelectedGhost;
        private byte _localRitualPlacedItems;
        private byte _localRitualLitCandles;

        public GhostType CurrentType => _networkStarted ? GetTypeAt(NetworkTypeIndex) : _localType;
        public GhostRoomMarker FavoriteRoom => _networkStarted ? GetRoomAt(NetworkRoomIndex) : _localFavoriteRoom;
        public float Aggression => _networkStarted ? NetworkAggression : _localAggression;
        public GhostType SelectedGhost => _networkStarted ? GetTypeAt(NetworkSelectedGhostIndex) : _localSelectedGhost;
        public int MarkedEvidenceMask => _networkStarted ? NetworkEvidenceMask : _localEvidenceMask;
        public bool IsNetworkSession => _networkStarted;
        public bool HasRitualAuthority => !_networkStarted || HasStateAuthority;
        public int RitualPlacedItems => _networkStarted ? NetworkRitualPlacedItems : _localRitualPlacedItems;
        public int RitualLitCandles => _networkStarted ? NetworkRitualLitCandles : _localRitualLitCandles;
        public int RitualWave => _networkStarted ? NetworkRitualWave : 0;
        public bool RitualActive => _networkStarted && NetworkRitualActive;
        public bool RitualWavesComplete => _networkStarted && NetworkRitualWavesComplete;
        public bool RitualFailed => _networkStarted && NetworkRitualFailed;
        public bool RitualVictory => _networkStarted && NetworkRitualVictory;
        public bool IsRitualItemPicked(int index) => index switch
        {
            0 => _networkStarted && NetworkRitualItem0Picked,
            1 => _networkStarted && NetworkRitualItem1Picked,
            2 => _networkStarted && NetworkRitualItem2Picked,
            _ => true
        };
        public GhostAgent Agent => _agent;
        public GameConfig Config => gameConfig;

        private void Start()
        {
            if (randomizeAtStart && !_networkStarted) InitializeSession();
        }

        public override void Spawned()
        {
            _networkStarted = true;
            _lastStateAuthority = HasStateAuthority;
            if (HasStateAuthority)
            {
                NetworkEvidenceMask = 0;
                NetworkSelectedGhostIndex = -1;
                InitializeSession();
            }
            _activated = false;
        }

        public override void FixedUpdateNetwork()
        {
            if (!_networkStarted) return;
            if (HasStateAuthority != _lastStateAuthority)
            {
                _lastStateAuthority = HasStateAuthority;
                Debug.Log($"[Fusion] Autoridad del estado del fantasma transferida: activa={_lastStateAuthority}.", this);
            }
            if (!HasStateAuthority) return;
            var clock = FindAnyObjectByType<NightClock>();
            if (!NetworkRitualActive && !NetworkRitualWavesComplete &&
                NetworkRitualPlacedItems >= 3 && NetworkRitualLitCandles >= 3 &&
                clock != null && clock.Act == NightAct.Ritual)
            {
                ActivateForRitual();
                NetworkRitualActive = true;
                NetworkRitualWave = 0;
                NetworkRitualWaveElapsed = 0f;
                TriggerNetworkRitualWave();
            }
            if (NetworkRitualActive && gameConfig != null && gameConfig.RitualWaveDurationSeconds > 0f)
            {
                NetworkRitualWaveElapsed += Runner.DeltaTime;
                if (NetworkRitualWaveElapsed >= gameConfig.RitualWaveDurationSeconds)
                {
                    NetworkRitualWaveElapsed = 0f;
                    if (NetworkRitualWave < 2) NetworkRitualWave++;
                    else
                    {
                        NetworkRitualActive = false;
                        NetworkRitualWavesComplete = true;
                    }
                    if (NetworkRitualActive) TriggerNetworkRitualWave();
                }
            }
            if (!NetworkRitualWavesComplete || NetworkRitualFailed || NetworkRitualVictory) return;
            NetworkRitualVictory = SelectedGhost == CurrentType;
            NetworkRitualFailed = !NetworkRitualVictory;
            if (NetworkRitualVictory) clock?.FinishVictory();
            else Agent?.Enrage(gameConfig != null ? gameConfig.RitualFailureSpeedMultiplier : 1f);
        }

        private void Update()
        {
            if (_networkStarted && !HasStateAuthority) return;
            if (_activated || FavoriteRoom == null || gameConfig == null) return;
            _playerRefreshTimer -= Time.deltaTime;
            if (_playerRefreshTimer <= 0f)
            {
                if (_networkStarted)
                {
                    var avatars = FindObjectsByType<UltimaCinta.Player.NetworkPlayerAvatar>(FindObjectsSortMode.None);
                    _players = new Transform[avatars.Length];
                    for (var index = 0; index < avatars.Length; index++) _players[index] = avatars[index].transform;
                }
                else
                {
                    var found = FindObjectsByType<UltimaCinta.Player.FirstPersonMovementController>(FindObjectsSortMode.None);
                    _players = new Transform[found.Length];
                    for (var index = 0; index < found.Length; index++) _players[index] = found[index].transform;
                }
                _playerRefreshTimer = gameConfig.GhostTargetRefreshSeconds;
            }
            var radiusSquared = gameConfig.GhostRoomActivationRadius * gameConfig.GhostRoomActivationRadius;
            foreach (var player in _players)
            {
                if (player == null || (player.position - FavoriteRoom.transform.position).sqrMagnitude > radiusSquared) continue;
                ActivateAtFavoriteRoom();
                break;
            }
        }

        public void InitializeSession()
        {
            if (ghostTypes == null || ghostTypes.Length == 0 || favoriteRoomMarkers == null || favoriteRoomMarkers.Length == 0)
            {
                Debug.LogError("GhostWorldController necesita GhostType y GhostRoomMarker asignados.", this);
                return;
            }

            var typeIndex = forcedGhostForTesting != null ? System.Array.IndexOf(ghostTypes, forcedGhostForTesting) : UnityEngine.Random.Range(0, ghostTypes.Length);
            if (typeIndex < 0) typeIndex = UnityEngine.Random.Range(0, ghostTypes.Length);
            var roomIndex = UnityEngine.Random.Range(0, favoriteRoomMarkers.Length);
            var aggression = gameConfig == null ? 0.5f : UnityEngine.Random.Range(gameConfig.GhostHuntStartAggressionMinimum, gameConfig.GhostHuntStartAggressionMaximum);

            if (_networkStarted)
            {
                if (!HasStateAuthority) return;
                NetworkTypeIndex = typeIndex;
                NetworkRoomIndex = roomIndex;
                NetworkAggression = aggression;
            }
            _localType = ghostTypes[typeIndex];
            _localFavoriteRoom = favoriteRoomMarkers[roomIndex];
            _localAggression = aggression;
            _activated = false;

            if (CurrentType.GhostPrefab == null)
                Debug.LogError($"GhostType {CurrentType.DisplayName} no tiene prefab asignado.", CurrentType);
        }

        public void RegisterAgent(GhostAgent agent)
        {
            _agent = agent;
            if (agent != null) _activated = true;
            if (agent != null && CurrentType != null && !_agent.IsInitialized)
                agent.Initialize(CurrentType, gameConfig, FavoriteRoom, Aggression);
        }

        public void MarkEvidence(GhostEvidence evidence)
        {
            if (!_networkStarted)
            {
                _localEvidenceMask |= 1 << (int)evidence;
                return;
            }
            if (HasStateAuthority) AddEvidence(evidence);
            else RPC_MarkEvidence((byte)evidence);
        }

        public void SelectGhost(GhostType ghost)
        {
            var index = ghostTypes == null ? -1 : Array.IndexOf(ghostTypes, ghost);
            if (index < 0) return;
            if (!_networkStarted) _localSelectedGhost = ghost;
            else if (HasStateAuthority) NetworkSelectedGhostIndex = index;
            else RPC_SelectGhost(index);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_MarkEvidence(byte evidenceIndex)
        {
            var evidence = (GhostEvidence)evidenceIndex;
            if (Enum.IsDefined(typeof(GhostEvidence), evidence) && CurrentType != null && CurrentType.Evidences.Contains(evidence))
                AddEvidence(evidence);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_SelectGhost(int typeIndex)
        {
            if (GetTypeAt(typeIndex) != null) NetworkSelectedGhostIndex = typeIndex;
        }

        private void AddEvidence(GhostEvidence evidence) => NetworkEvidenceMask |= 1 << (int)evidence;

        public void RequestRitualItemPlacement()
        {
            if (!_networkStarted) { _localRitualPlacedItems = (byte)Mathf.Min(3, _localRitualPlacedItems + 1); return; }
            if (HasStateAuthority) AddRitualItem(); else RPC_PlaceRitualItem();
        }

        public void RequestRitualCandleLighting()
        {
            if (!_networkStarted) { _localRitualLitCandles = (byte)Mathf.Min(3, _localRitualLitCandles + 1); return; }
            if (HasStateAuthority) AddRitualCandle(); else RPC_LightRitualCandle();
        }

        public void RequestRitualItemPickup(int index)
        {
            if (index < 0 || index >= 3 || !_networkStarted) return;
            if (HasStateAuthority) MarkRitualItemPicked(index);
            else RPC_PickRitualItem(index);
        }

        public void BeginRitual()
        {
            if (!_networkStarted || !HasStateAuthority || NetworkRitualActive || NetworkRitualWavesComplete) return;
            if (NetworkRitualPlacedItems < 3 || NetworkRitualLitCandles < 3) return;
            ActivateForRitual();
            NetworkRitualActive = true;
            NetworkRitualWave = 0;
            NetworkRitualWaveElapsed = 0f;
            TriggerNetworkRitualWave();
        }

        public void AdvanceRitual(float deltaTime, float waveDuration)
        {
            if (!_networkStarted || !HasStateAuthority || !NetworkRitualActive || waveDuration <= 0f) return;
            NetworkRitualWaveElapsed += deltaTime;
            if (NetworkRitualWaveElapsed < waveDuration) return;
            NetworkRitualWaveElapsed = 0f;
            if (NetworkRitualWave < 2) NetworkRitualWave++;
            else
            {
                NetworkRitualActive = false;
                NetworkRitualWavesComplete = true;
            }
            if (NetworkRitualActive) TriggerNetworkRitualWave();
        }

        public void ActivateForRitual()
        {
            if (_networkStarted && !HasStateAuthority) return;
            ActivateAtFavoriteRoom();
        }

        private void TriggerNetworkRitualWave()
        {
            var waveScale = NetworkRitualWave + 1f;
            var durationScale = gameConfig != null ? gameConfig.RitualWaveHuntDurationMultiplier : 1f;
            var speedScale = gameConfig != null ? gameConfig.RitualWaveSpeedMultiplier : 1f;
            Agent?.TriggerRitualHunt(durationScale * waveScale, speedScale * waveScale);
        }

        public void ResolveRitual(bool victory)
        {
            if (!_networkStarted || !HasStateAuthority || !NetworkRitualWavesComplete) return;
            NetworkRitualVictory = victory;
            NetworkRitualFailed = !victory;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_PlaceRitualItem() => AddRitualItem();

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_LightRitualCandle() => AddRitualCandle();

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_PickRitualItem(int index) => MarkRitualItemPicked(index);

        private void MarkRitualItemPicked(int index)
        {
            if (!HasStateAuthority) return;
            switch (index)
            {
                case 0: NetworkRitualItem0Picked = true; break;
                case 1: NetworkRitualItem1Picked = true; break;
                case 2: NetworkRitualItem2Picked = true; break;
            }
        }

        private void AddRitualItem()
        {
            if (HasStateAuthority && NetworkRitualPlacedItems < 3) NetworkRitualPlacedItems++;
        }

        private void AddRitualCandle()
        {
            if (HasStateAuthority && NetworkRitualPlacedItems >= 3 && NetworkRitualLitCandles < 3)
                NetworkRitualLitCandles++;
        }

        private void ActivateAtFavoriteRoom()
        {
            if (_activated || FavoriteRoom == null || CurrentType == null || CurrentType.GhostPrefab == null) return;
            _activated = true;
            var position = FavoriteRoom.transform.position;
            if (gameConfig != null && gameConfig.GhostHearingRange > 0f &&
                NavMesh.SamplePosition(position, out var navMeshHit, gameConfig.GhostHearingRange, NavMesh.AllAreas))
                position = navMeshHit.position;
            var rotation = FavoriteRoom.transform.rotation;
            if (_networkStarted)
            {
                var ghostObject = Runner.Spawn(CurrentType.GhostPrefab, position, rotation, PlayerRef.None,
                    (runner, spawnedObject) => spawnedObject.GetComponent<GhostAgent>()?.Initialize(CurrentType, gameConfig, FavoriteRoom, Aggression));
                _agent = ghostObject != null ? ghostObject.GetComponent<GhostAgent>() : null;
            }
            else
            {
                var ghost = Instantiate(CurrentType.GhostPrefab, position, rotation);
                _agent = ghost.GetComponent<GhostAgent>();
                if (_agent == null) _agent = ghost.AddComponent<GhostAgent>();
                _agent.Initialize(CurrentType, gameConfig, FavoriteRoom, Aggression);
            }
        }

        private GhostType GetTypeAt(int index) => ghostTypes != null && index >= 0 && index < ghostTypes.Length ? ghostTypes[index] : null;
        private GhostRoomMarker GetRoomAt(int index) => favoriteRoomMarkers != null && index >= 0 && index < favoriteRoomMarkers.Length ? favoriteRoomMarkers[index] : null;
    }
}
