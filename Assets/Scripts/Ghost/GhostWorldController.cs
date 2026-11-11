using UnityEngine;
using UnityEngine.AI;
using UltimaCinta.Data;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostWorldController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private GhostType[] ghostTypes;
        [SerializeField] private GhostRoomMarker[] favoriteRoomMarkers;
        [SerializeField] private GhostType forcedGhostForTesting;
        [SerializeField] private bool randomizeAtStart = true;
        private GhostAgent _agent;
        private Transform[] _players = System.Array.Empty<Transform>();
        private bool _activated;
        private float _playerRefreshTimer;

        public GhostType CurrentType { get; private set; }
        public GhostRoomMarker FavoriteRoom { get; private set; }
        public float Aggression { get; private set; }
        public GhostAgent Agent => _agent;

        private void Start()
        {
            if (randomizeAtStart) InitializeSession();
        }

        private void Update()
        {
            if (_activated || FavoriteRoom == null || gameConfig == null) return;
            _playerRefreshTimer -= Time.deltaTime;
            if (_playerRefreshTimer <= 0f)
            {
                var found = FindObjectsByType<UltimaCinta.Player.FirstPersonMovementController>(FindObjectsSortMode.None);
                _players = new Transform[found.Length];
                for (var index = 0; index < found.Length; index++) _players[index] = found[index].transform;
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

            CurrentType = forcedGhostForTesting != null ? forcedGhostForTesting : ghostTypes[Random.Range(0, ghostTypes.Length)];
            FavoriteRoom = favoriteRoomMarkers[Random.Range(0, favoriteRoomMarkers.Length)];
            Aggression = gameConfig == null ? 0.5f : Random.Range(gameConfig.GhostHuntStartAggressionMinimum, gameConfig.GhostHuntStartAggressionMaximum);
            Debug.Log($"[Fase 3] Inicio elegido: fantasma={CurrentType.DisplayName}, sala={FavoriteRoom.RoomName}, agresividad={Aggression:0.00}, forzado={(forcedGhostForTesting != null)}.", this);
            if (CurrentType.GhostPrefab == null)
            {
                Debug.LogError($"GhostType {CurrentType.DisplayName} no tiene prefab asignado.", CurrentType);
                return;
            }

            _activated = false;
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
            var ghost = Instantiate(CurrentType.GhostPrefab, position, rotation);
            _agent = ghost.GetComponent<GhostAgent>();
            if (_agent == null) _agent = ghost.AddComponent<GhostAgent>();
            _agent.Initialize(CurrentType, gameConfig, FavoriteRoom, Aggression);
            Debug.Log($"[Fase 3] El jugador entró a {FavoriteRoom.RoomName}; el fantasma ya se manifiesta.", this);
            Debug.Log($"[Fase 3] GhostAgent inicializado en {FavoriteRoom.RoomName}; spawn={position}.", _agent);
        }
    }
}
