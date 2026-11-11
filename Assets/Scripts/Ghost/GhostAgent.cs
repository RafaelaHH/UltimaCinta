using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UltimaCinta.Data;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GhostAgent : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent navigation;
        private GhostType _type;
        private GameConfig _config;
        private Transform[] _players = Array.Empty<Transform>();
        private UltimaCinta.CameraSystem.CameraResourceController[] _cameraResources = Array.Empty<UltimaCinta.CameraSystem.CameraResourceController>();
        private SanityController[] _sanityControllers = Array.Empty<SanityController>();
        private UltimaCinta.Player.PlayerInventory[] _inventories = Array.Empty<UltimaCinta.Player.PlayerInventory>();
        private UltimaCinta.Interaction.HideoutInteractable[] _hideouts = Array.Empty<UltimaCinta.Interaction.HideoutInteractable>();
        private float _aggression;
        private float _stateTimer;
        private float _wanderTimer;
        private float _huntCooldown;
        private float _playerScanTimer;
        private float _behaviorTickTimer;
        private float _activeHuntDuration;
        private float _sessionTimer;
        private float _restDuration;
        private Light[] _lights = Array.Empty<Light>();
        private bool[] _lightStates = Array.Empty<bool>();
        private UltimaCinta.Interaction.DoorInteractable[] _doors = Array.Empty<UltimaCinta.Interaction.DoorInteractable>();
        private Vector3 _homePosition;
        private bool _isInitialized;
        private bool _navigationWarningLogged;
        private bool _hasHunted;
        private readonly Collider[] _nearbyObjectBuffer = new Collider[32];

        public GhostType Type => _type;
        public GhostState State { get; private set; } = GhostState.Wandering;
        public float Interference { get; private set; }
        public event Action<GhostAgent, Transform> PlayerKilled;
        public event Action<GhostAgent, GhostState> StateChanged;
        public event Action<GhostEvidence> EvidenceProduced;

        public void Initialize(GhostType type, GameConfig config, GhostRoomMarker favoriteRoom, float aggression)
        {
            _type = type;
            _config = config;
            _aggression = Mathf.Clamp01(aggression);
            _homePosition = favoriteRoom != null ? favoriteRoom.transform.position : transform.position;
            if (navigation == null) navigation = GetComponent<NavMeshAgent>();
            if (navigation != null && type != null)
                navigation.speed = type.MovementSpeed * (_config != null ? _config.GhostMovementSpeedMultiplier : 1f);
            _isInitialized = true;
            Debug.Log($"[Fase 3] IA lista: {_type.DisplayName}; sala favorita={favoriteRoom?.RoomName ?? "sin sala"}; agresividad={_aggression:0.00}; velocidad={(navigation != null ? navigation.speed : 0f):0.00}.", this);
            _lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            _lightStates = new bool[_lights.Length];
            _doors = FindObjectsByType<UltimaCinta.Interaction.DoorInteractable>(FindObjectsSortMode.None);
            ChangeState(GhostState.Wandering);
            ProduceEvidence(GhostEvidence.Apparition);
        }

        private void Awake()
        {
            if (navigation == null) navigation = GetComponent<NavMeshAgent>();
            // Ghosts attack through hunt logic; their body must not physically shove players.
            foreach (var ghostCollider in GetComponentsInChildren<Collider>(true))
                ghostCollider.isTrigger = true;
        }

        private void Update()
        {
            if (!_isInitialized || _config == null) return;
            _stateTimer += Time.deltaTime;
            _sessionTimer += Time.deltaTime;
            _wanderTimer += Time.deltaTime;
            _huntCooldown = Mathf.Max(0f, _huntCooldown - Time.deltaTime);
            _playerScanTimer -= Time.deltaTime;
            if (_playerScanTimer <= 0f)
            {
                CachePlayers();
                _playerScanTimer = _config != null ? _config.GhostTargetRefreshSeconds : 0.5f;
            }
            UpdateInterference();
            _behaviorTickTimer += Time.deltaTime;
            if (_behaviorTickTimer >= _config.GhostTargetRefreshSeconds)
            {
                var elapsed = _behaviorTickTimer;
                _behaviorTickTimer = 0f;
                UpdateBehavior(elapsed);
            }

            switch (State)
            {
                case GhostState.Wandering: UpdateWandering(); break;
                case GhostState.Activity: UpdateActivity(); break;
                case GhostState.Hunting: UpdateHunting(); break;
                case GhostState.Resting: UpdateResting(); break;
            }
        }

        public void HearNoise(Vector3 position, float loudness)
        {
            if (_type == null || (_type.Behavior & GhostBehavior.HuntsBySound) == 0) return;
            if (Vector3.Distance(transform.position, position) > _config.GhostHearingRange * Mathf.Clamp(loudness, 0.1f, 2f)) return;
            SetDestination(position);
            ChangeState(GhostState.Activity);
            Debug.Log($"[Fase 3] {_type.DisplayName} oyó ruido ({loudness:0.00}) en {position}.", this);
            if ((_type.Behavior & GhostBehavior.NoiseReducesSanity) != 0)
            for (var index = 0; index < _players.Length; index++)
                if (_players[index] != null && Vector3.Distance(_players[index].position, position) <= _config.GhostHearingRange)
                    _sanityControllers[index]?.Reduce(_config.GhostSanityDrainPerSecond);
        }

        public bool HasEvidence(GhostEvidence evidence) => _type != null && _type.Evidences.Contains(evidence);

        public void TriggerHunt()
        {
            if (!_isInitialized || State == GhostState.Hunting) return;
            var vampiricDarknessRule = (_type.Behavior & GhostBehavior.HuntsOnlyInDarkness) != 0 && AnyRoomLightOn();
            if (vampiricDarknessRule || _huntCooldown > 0f) return;
            ChangeState(GhostState.Hunting);
            _hasHunted = true;
            _stateTimer = 0f;
            _activeHuntDuration = UnityEngine.Random.Range(_config.GhostHuntDurationMinimumSeconds, _config.GhostHuntDurationMaximumSeconds);
            Debug.Log($"[Fase 3] Cacería iniciada por {_type.DisplayName}; duración={_activeHuntDuration:0.0}s; agresividad={_aggression:0.00}.", this);
            for (var index = 0; index < _lights.Length; index++)
            {
                _lightStates[index] = _lights[index] != null && _lights[index].enabled;
                if (_lights[index] != null) _lights[index].enabled = false;
            }
            ProduceEvidence(GhostEvidence.EmfLevelFive);
        }

        private void UpdateWandering()
        {
            if (_stateTimer >= _config.GhostActivityIntervalSeconds)
            {
                ChangeState(GhostState.Activity);
                ProduceEvidence(GhostEvidence.Writing);
                return;
            }
            if (_wanderTimer >= _config.GhostWanderIntervalSeconds)
            {
                _wanderTimer = 0f;
                if (_type != null && (_type.Behavior & GhostBehavior.TeleportsNearPlayers) != 0 && _players.Length > 0)
                    TeleportNear(_players[UnityEngine.Random.Range(0, _players.Length)]);
                else WanderNearHome();
            }
            if (ShouldHunt()) TriggerHunt();
        }

        private void UpdateActivity()
        {
            if (_stateTimer >= _config.GhostRestDurationSeconds)
            {
                _restDuration = _config.GhostRestDurationSeconds;
                ChangeState(GhostState.Resting);
                ProduceEvidence(GhostEvidence.FreezingTemperature);
            }
            if (ShouldHunt()) TriggerHunt();
        }

        private void UpdateHunting()
        {
            if (_stateTimer >= _activeHuntDuration)
            {
                _huntCooldown = _config.GhostHuntCooldownSeconds *
                    ((_type.Behavior & GhostBehavior.FrequentShortHunts) != 0 ? _config.PoltergeistHuntCooldownMultiplier : 1f);
                RestoreLights();
                RetreatFromNearestPlayer();
                Debug.Log($"[Fase 3] Cacería terminada: {_type.DisplayName} se retira por {_config.GhostPostHuntRetreatSeconds:0.0}s.", this);
                _restDuration = _config.GhostPostHuntRetreatSeconds;
                ChangeState(GhostState.Resting);
                return;
            }
            var target = FindNearestPlayer();
            if (target == null)
            {
                if (navigation != null && navigation.isOnNavMesh) navigation.ResetPath();
                return;
            }
            SetDestination(target.position);
            if (Vector3.Distance(transform.position, target.position) <= _config.GhostHuntDistance)
            {
                var sanity = target.GetComponent<SanityController>();
                if (sanity != null && !sanity.IsDead) { PlayerKilled?.Invoke(this, target); sanity.Die(); }
            }
        }

        private void UpdateResting()
        {
            if (_stateTimer < _restDuration) return;
            ChangeState(GhostState.Wandering);
        }

        private void RetreatFromNearestPlayer()
        {
            var target = FindNearestPlayer();
            if (navigation == null || !navigation.isOnNavMesh) return;

            var retreatPoint = _homePosition;
            if (target != null)
            {
                var direction = transform.position - target.position;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f) direction = -target.forward;
                retreatPoint = transform.position + direction.normalized * _config.GhostHearingRange;
            }

            if (NavMesh.SamplePosition(retreatPoint, out var hit, Mathf.Max(1f, _config.GhostHearingRange), NavMesh.AllAreas))
                navigation.SetDestination(hit.position);
            else
                navigation.ResetPath();
        }

        private void UpdateBehavior(float elapsedSeconds)
        {
            if (_type == null) return;
            if ((_type.Behavior & GhostBehavior.DrainsBatteryNearby) != 0)
                for (var index = 0; index < _players.Length; index++)
                {
                    if (_players[index] == null || _cameraResources[index] == null) continue;
                    var localInterference = InterferenceAt(_players[index].position);
                    if (localInterference > 0.5f) _cameraResources[index].DrainBatteryForGhost(elapsedSeconds * localInterference);
                }
            if ((_type.Behavior & GhostBehavior.PassesThroughWalls) != 0 && navigation != null)
                navigation.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            if ((_type.Behavior & GhostBehavior.RepelledByLight) != 0 && State != GhostState.Hunting)
                MoveAwayFromNearestLight();
            if ((_type.Behavior & GhostBehavior.RepelledByGarlic) != 0 && State != GhostState.Hunting)
                MoveAwayFromGarlicCarrier();
        }

        private bool ShouldHunt()
        {
            if (!_hasHunted && _sessionTimer < _config.GhostInitialHuntGraceSeconds) return false;
            if (_huntCooldown > 0f || _players.Length == 0) return false;
            if (_type != null && (_type.Behavior & GhostBehavior.HuntsOnlyInDarkness) != 0 && AnyRoomLightOn()) return false;
            var nearest = FindNearestPlayer();
            if (nearest == null) return false;
            var distance = Vector3.Distance(transform.position, nearest.position);
            var extended = AveragePlayerSanity() <= _config.ExtendedDetectionSanityThreshold;
            return _stateTimer >= _config.GhostActivityIntervalSeconds * (1f - _aggression) &&
                   distance <= _config.GhostHuntStartDistance * (extended ? 1.5f : 1f);
        }

        private void CachePlayers()
        {
            var found = FindObjectsByType<UltimaCinta.Player.FirstPersonMovementController>(FindObjectsSortMode.None);
            _hideouts = FindObjectsByType<UltimaCinta.Interaction.HideoutInteractable>(FindObjectsSortMode.None);
            if (_players.Length != found.Length)
            {
                _players = new Transform[found.Length];
                _cameraResources = new UltimaCinta.CameraSystem.CameraResourceController[found.Length];
                _sanityControllers = new SanityController[found.Length];
                _inventories = new UltimaCinta.Player.PlayerInventory[found.Length];
            }
            for (var index = 0; index < found.Length; index++)
            {
                _players[index] = found[index].transform;
                _cameraResources[index] = found[index].GetComponentInChildren<UltimaCinta.CameraSystem.CameraResourceController>();
                _sanityControllers[index] = found[index].GetComponent<SanityController>();
                _inventories[index] = found[index].GetComponent<UltimaCinta.Player.PlayerInventory>();
            }
        }

        private Transform FindNearestPlayer()
        {
            Transform nearest = null;
            var nearestDistance = float.PositiveInfinity;
            for (var index = 0; index < _players.Length; index++)
            {
                var player = _players[index];
                if (player == null) continue;
                if (_sanityControllers[index] != null && _sanityControllers[index].IsDead) continue;
                if (IsHidden(player.gameObject)) continue;
                var distance = (player.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance) { nearestDistance = distance; nearest = player; }
            }
            return nearest;
        }

        private bool IsHidden(GameObject player)
        {
            foreach (var hideout in _hideouts)
                if (hideout != null && hideout.IsOccupant(player)) return true;
            return false;
        }

        private float AveragePlayerSanity()
        {
            if (_players.Length == 0) return 100f;
            var sum = 0f;
            var count = 0;
            for (var index = 0; index < _players.Length; index++)
                if (_players[index] != null && _sanityControllers[index] != null && !_sanityControllers[index].IsDead)
                { sum += _sanityControllers[index].Value; count++; }
            return count == 0 ? 100f : sum / count;
        }

        private void UpdateInterference()
        {
            var nearest = FindNearestPlayer();
            Interference = nearest == null ? 0f : 1f - Mathf.Clamp01(Vector3.Distance(transform.position, nearest.position) / Mathf.Max(0.01f, _config.GhostInterferenceRange));
        }

        private void WanderNearHome()
        {
            var radius = Mathf.Max(1f, _config.GhostHearingRange);
            var point = _homePosition + UnityEngine.Random.insideUnitSphere * radius;
            point.y = _homePosition.y;
            SetDestination(point);
        }

        private void TeleportNear(Transform target)
        {
            if (target == null) return;
            var offset = UnityEngine.Random.insideUnitSphere * _config.GhostTeleportRadius;
            offset.y = 0f;
            var destination = target.position + offset;
            if (!NavMesh.SamplePosition(destination, out var hit, _config.GhostTeleportRadius, NavMesh.AllAreas)) return;
            if (navigation != null && navigation.isOnNavMesh) navigation.Warp(hit.position);
            else transform.position = hit.position;
        }

        private void SetDestination(Vector3 point)
        {
            if (navigation != null && navigation.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(point, out var hit, _config.GhostHearingRange, NavMesh.AllAreas))
                    navigation.SetDestination(hit.position);
                return;
            }

            if (_type != null && (_type.Behavior & GhostBehavior.PassesThroughWalls) != 0)
            {
                transform.position = Vector3.MoveTowards(transform.position, point, _type.MovementSpeed * Time.deltaTime);
                return;
            }

            if (!_navigationWarningLogged)
            {
                _navigationWarningLogged = true;
                Debug.LogWarning($"{_type?.DisplayName ?? name} no se mueve porque no está conectado al NavMesh.", this);
            }
        }

        private void PushLooseObject()
        {
            var hitCount = Physics.OverlapSphereNonAlloc(transform.position, _config.PoltergeistObjectThrowRadius,
                _nearbyObjectBuffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (var index = 0; index < hitCount; index++)
            {
                var hit = _nearbyObjectBuffer[index];
                _nearbyObjectBuffer[index] = null;
                if (hit == null || hit.GetComponentInParent<GhostAgent>() != null ||
                    hit.GetComponentInParent<UltimaCinta.Interaction.PickupInteractable>() != null) continue;
                if (Mathf.Max(hit.bounds.size.x, Mathf.Max(hit.bounds.size.y, hit.bounds.size.z)) > _config.PoltergeistObjectMaximumSize) continue;

                var body = hit.attachedRigidbody;
                if (body == null)
                {
                    body = hit.gameObject.AddComponent<Rigidbody>();
                    body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    hit.isTrigger = false;
                }
                if (body.isKinematic) continue;
                body.AddForce((hit.transform.position - transform.position + Vector3.up) * _config.PoltergeistObjectThrowForce,
                    ForceMode.Impulse);
                break;
            }
        }

        private bool AnyRoomLightOn()
        {
            foreach (var light in _lights)
                if (light != null && light.enabled && light.intensity > 0f && Vector3.Distance(light.transform.position, transform.position) <= _config.GhostHearingRange) return true;
            return false;
        }

        private void MoveAwayFromNearestLight()
        {
            var closest = float.PositiveInfinity;
            Light source = null;
            foreach (var light in _lights)
            {
                if (light == null || !light.enabled || light.intensity <= 0f) continue;
                var distance = (light.transform.position - transform.position).sqrMagnitude;
                if (distance < closest) { closest = distance; source = light; }
            }
            if (source != null && closest < _config.GhostHearingRange * _config.GhostHearingRange)
            {
                SetDestination(transform.position + (transform.position - source.transform.position).normalized * _config.GhostHearingRange);
            }
        }

        private void MoveAwayFromGarlicCarrier()
        {
            Transform nearest = null;
            var shortest = float.PositiveInfinity;
            for (var index = 0; index < _players.Length; index++)
            {
                var player = _players[index];
                if (player == null || !CarriesGarlic(player)) continue;
                var distance = (player.position - transform.position).sqrMagnitude;
                if (distance < shortest) { shortest = distance; nearest = player; }
            }
            if (nearest != null && shortest < _config.GhostHearingRange * _config.GhostHearingRange)
            {
                SetDestination(transform.position + (transform.position - nearest.position).normalized * _config.GhostHearingRange);
            }
        }

        private bool CarriesGarlic(Transform player)
        {
            var playerIndex = Array.IndexOf(_players, player);
            var inventory = playerIndex >= 0 ? _inventories[playerIndex] : null;
            if (inventory == null) return false;
            for (var slot = 0; slot < inventory.Capacity; slot++)
                if (inventory.GetItem(slot) != null && inventory.GetItem(slot).DisplayName == "Ajo") return true;
            return false;
        }

        private void SlamNearbyDoors()
        {
            foreach (var door in _doors)
                if (door != null && Vector3.Distance(transform.position, door.transform.position) <= _config.GhostHearingRange)
                    door.ForceSlamFromGhost();
        }

        private void ProduceEvidence(GhostEvidence evidence)
        {
            if (!HasEvidence(evidence)) return;
            Debug.Log($"[Fase 3] Evidencia producida por {_type.DisplayName}: {evidence}.", this);
            EvidenceProduced?.Invoke(evidence);
        }

        private void ChangeState(GhostState state)
        {
            State = state;
            _stateTimer = 0f;
            Debug.Log($"[Fase 3] {_type?.DisplayName ?? name}: estado IA -> {state}.", this);
            StateChanged?.Invoke(this, state);
            if (state == GhostState.Activity && _type != null)
            {
                if ((_type.Behavior & GhostBehavior.ThrowsObjects) != 0) PushLooseObject();
                if ((_type.Behavior & GhostBehavior.SlamsDoors) != 0) SlamNearbyDoors();
            }
        }

        private void RestoreLights()
        {
            for (var index = 0; index < _lights.Length; index++)
                if (_lights[index] != null) _lights[index].enabled = _lightStates[index];
        }

        public float InterferenceAt(Vector3 position)
        {
            return _config == null ? 0f : 1f - Mathf.Clamp01(Vector3.Distance(position, transform.position) / Mathf.Max(0.01f, _config.GhostInterferenceRange));
        }

        private void OnDisable() => RestoreLights();
    }
}
