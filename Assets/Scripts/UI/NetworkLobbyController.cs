using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UltimaCinta.Interaction;
using UltimaCinta.Player;

namespace UltimaCinta.UI
{
    [DisallowMultipleComponent]
    public sealed class NetworkLobbyController : MonoBehaviour, INetworkRunnerCallbacks
    {
        private const int MaximumPlayers = 4;
        private const float SessionListTimeoutSeconds = 12f;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private string mansionScenePath = "Assets/Scenes/Mansion_Generated.unity";
        private string _roomName = "UltimaCinta";
        private string _status = "Crea una sala o únete por nombre.";
        private bool _busy;
        private bool _connected;
        private NetworkRunner _runner;
        private TaskCompletionSource<List<SessionInfo>> _sessionListSource;
        private bool _localPlayerJoined;
        private bool _localPlayerSpawned;
        private bool _sceneLoaded;
        private bool _testDropAttempted;
        private bool _testPickAttempted;

        private void Start()
        {
            if (HasCommandLineArgument("-ucPickTest"))
                NetworkDroppedItem.NetworkItemSpawned += OnNetworkItemSpawned;
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                var create = string.Equals(arguments[index], "-ucCreateRoom", StringComparison.OrdinalIgnoreCase);
                var join = string.Equals(arguments[index], "-ucJoinRoom", StringComparison.OrdinalIgnoreCase);
                if ((!create && !join) || string.IsNullOrWhiteSpace(arguments[index + 1])) continue;
                _roomName = arguments[index + 1];
                BeginSessionRequest(create);
                return;
            }
        }

        private void OnDestroy()
        {
            NetworkDroppedItem.NetworkItemSpawned -= OnNetworkItemSpawned;
            if (_runner != null) _runner.RemoveCallbacks(this);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != "Lobby") return;
            if (_connected) return;
            var previousColor = GUI.color;
            var width = Mathf.Min(520f, Screen.width - 32f);
            var panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.2f, width, 250f);
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            var title = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 36f), "ÚLTIMA CINTA · LOBBY", title);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 66f, 125f, 28f), "Nombre de sala");
            GUI.enabled = !_busy && !_connected;
            _roomName = GUI.TextField(new Rect(panel.x + 150f, panel.y + 66f, panel.width - 178f, 30f), _roomName, 32);
            var buttonWidth = (panel.width - 72f) * 0.5f;
            if (GUI.Button(new Rect(panel.x + 28f, panel.y + 112f, buttonWidth, 38f), "Crear sala"))
                BeginSessionRequest(create: true);
            if (GUI.Button(new Rect(panel.x + 44f + buttonWidth, panel.y + 112f, buttonWidth, 38f), "Unirse"))
                BeginSessionRequest(create: false);
            GUI.enabled = true;
            GUI.Label(new Rect(panel.x + 28f, panel.y + 158f, panel.width - 56f, 54f), _status);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 212f, panel.width - 56f, 24f), $"Shared Mode · hasta {MaximumPlayers} jugadores");
            GUI.color = previousColor;
        }

        public async void ReturnToTitle()
        {
            var runner = _runner;
            _runner = null;
            _connected = false;
            _busy = false;
            _localPlayerJoined = false;
            _localPlayerSpawned = false;
            _sceneLoaded = false;
            if (runner != null)
            {
                runner.RemoveCallbacks(this);
                await runner.Shutdown();
                Destroy(runner.gameObject);
            }
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
        }

        private void BeginSessionRequest(bool create)
        {
            if (_busy || _connected) return;
            _roomName = _roomName.Trim();
            if (string.IsNullOrWhiteSpace(_roomName))
            {
                _status = "Escribe un nombre de sala.";
                return;
            }
            if (playerPrefab == null)
            {
                _status = "Falta asignar el prefab de jugador de red.";
                return;
            }
            if (PhotonAppSettings.Global == null || string.IsNullOrWhiteSpace(PhotonAppSettings.Global.AppSettings.AppIdFusion))
            {
                _status = "Falta el App ID de Fusion en Tools > Fusion > Fusion Hub.";
                return;
            }
            _busy = true;
            _status = create ? "Buscando el nombre antes de crear la sala..." : "Buscando la sala...";
            _sessionListSource = new TaskCompletionSource<List<SessionInfo>>();
            CreateOrJoinAsync(create);
        }

        private async void CreateOrJoinAsync(bool create)
        {
            var runnerObject = new GameObject("NetworkRunner");
            DontDestroyOnLoad(runnerObject);
            _runner = runnerObject.AddComponent<NetworkRunner>();
            _runner.AddCallbacks(this);
            var sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = runnerObject.AddComponent<NetworkObjectProviderDefault>();

            try
            {
                await _runner.JoinSessionLobby(SessionLobby.Shared, null, null, null, CancellationToken.None, true);
                var completed = await Task.WhenAny(_sessionListSource.Task, Task.Delay(TimeSpan.FromSeconds(SessionListTimeoutSeconds)));
                if (completed != _sessionListSource.Task)
                {
                    await FailAndResetAsync("No llegó la lista de salas de Photon. Revisa la conexión y vuelve a intentar.");
                    return;
                }

                var sessions = _sessionListSource.Task.Result;
                SessionInfo found = null;
                foreach (var session in sessions)
                {
                    if (string.Equals(session.Name, _roomName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = session;
                        break;
                    }
                }

                if (create && found != null)
                {
                    await FailAndResetAsync("Ya existe una sala con ese nombre. Elige otro nombre o únete a ella.");
                    return;
                }
                if (!create && (found == null || !found.IsOpen || found.PlayerCount >= found.MaxPlayers))
                {
                    await FailAndResetAsync(found == null ? "No existe una sala abierta con ese nombre." : "La sala está cerrada o llena.");
                    return;
                }

                var buildIndex = SceneUtility.GetBuildIndexByScenePath(mansionScenePath);
                if (buildIndex < 0)
                {
                    await FailAndResetAsync("Mansion_Generated no está incluida en Build Settings.");
                    return;
                }

                var sceneInfo = new NetworkSceneInfo();
                sceneInfo.AddSceneRef(SceneRef.FromIndex(buildIndex));
                _status = create ? "Creando sala y cargando la mansión..." : "Uniéndose y cargando la mansión...";
                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = _roomName,
                    PlayerCount = MaximumPlayers,
                    Scene = sceneInfo,
                    SceneManager = sceneManager,
                    ObjectProvider = objectProvider
                });
                if (!result.Ok)
                {
                    await FailAndResetAsync($"No se pudo iniciar la sesión: {result.ShutdownReason}");
                    return;
                }

                _connected = true;
                _busy = false;
                _status = $"Conectado a {_roomName}.";
                TrySpawnLocalPlayer();
            }
            catch (Exception exception)
            {
                await FailAndResetAsync($"Error al conectar: {exception.Message}");
            }
        }

        private async Task FailAndResetAsync(string message)
        {
            _status = message;
            _busy = false;
            _connected = false;
            _localPlayerJoined = false;
            _localPlayerSpawned = false;
            _sceneLoaded = false;
            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                await _runner.Shutdown();
                Destroy(_runner.gameObject);
                _runner = null;
            }
        }

        private void TrySpawnLocalPlayer()
        {
            if (!_sceneLoaded || !_localPlayerJoined || _localPlayerSpawned || _runner == null || !_runner.IsRunning) return;
            var localPlayer = _runner.LocalPlayer;
            if (_runner.GetPlayerObject(localPlayer) != null)
            {
                _localPlayerSpawned = true;
                return;
            }

            var spawnPoint = FindFirstObjectByType<PlayerSpawnPoint>();
            var spawnPosition = spawnPoint != null ? spawnPoint.GetPosition(localPlayer) : Vector3.zero;
            var spawnRotation = spawnPoint != null ? spawnPoint.GetRotation(localPlayer) : Quaternion.identity;
            var playerObject = _runner.Spawn(playerPrefab, spawnPosition, spawnRotation, localPlayer);
            if (playerObject == null)
            {
                _status = "Fusion no pudo crear el jugador de red.";
                return;
            }

            _runner.SetPlayerObject(localPlayer, playerObject);
            _localPlayerSpawned = true;
            TryRunNetworkDropTest(playerObject);
        }

        private void TryRunNetworkDropTest(NetworkObject playerObject)
        {
            if (_testDropAttempted || !HasCommandLineArgument("-ucDropTest")) return;
            var inventory = playerObject.GetComponent<PlayerInventory>();
            var catalog = Resources.Load<UltimaCinta.Data.NetworkItemCatalog>("NetworkItemCatalog");
            var item = catalog != null ? catalog.GetItem(0) : null;
            _testDropAttempted = true;
            if (inventory == null || item == null || !inventory.TryAdd(item))
            {
                Debug.LogError("[Fusion QA] No se pudo preparar el objeto de prueba para soltarlo.");
                return;
            }

            var dropped = inventory.DropSelected();
            Debug.Log($"[Fusion QA] Creador soltó {item.DisplayName}: éxito={dropped}.");
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            RefreshNetworkPlayerCount(runner);
            if (player != runner.LocalPlayer) return;
            _localPlayerJoined = true;
            TrySpawnLocalPlayer();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            var clock = FindAnyObjectByType<UltimaCinta.Data.NightClock>();
            if (clock != null) clock.ReportDisconnectedPlayer(player.PlayerId);
        }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            _sessionListSource?.TrySetResult(sessionList);
        }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            _sceneLoaded = true;
            RefreshNetworkPlayerCount(runner);
            TrySpawnLocalPlayer();
            TryRunNetworkPickupTest(FindFirstObjectByType<NetworkDroppedItem>());
            if (HasCommandLineArgument("-ucSpawnGhost"))
                FindFirstObjectByType<UltimaCinta.Ghost.GhostWorldController>()?.ActivateForRitual();
        }

        private void OnNetworkItemSpawned(NetworkDroppedItem droppedItem)
        {
            TryRunNetworkPickupTest(droppedItem);
        }

        private void TryRunNetworkPickupTest(NetworkDroppedItem droppedItem)
        {
            if (_testPickAttempted || !_sceneLoaded || droppedItem == null || _runner == null || !_runner.IsRunning) return;
            var localPlayerObject = _runner.GetPlayerObject(_runner.LocalPlayer);
            if (localPlayerObject == null) return;
            _testPickAttempted = true;
            var picked = droppedItem.Interact(localPlayerObject.gameObject);
            Debug.Log($"[Fusion QA] Invitado interactuó con objeto de red: éxito={picked}.");
        }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        private static void RefreshNetworkPlayerCount(NetworkRunner runner)
        {
            var clock = FindAnyObjectByType<UltimaCinta.Data.NightClock>();
            if (clock == null || !clock.HasStateAuthority) return;
            var count = 0;
            foreach (var player in runner.ActivePlayers) count++;
            clock.RefreshPlayerCount(count);
        }

        private static bool HasCommandLineArgument(string argument)
        {
            foreach (var commandLineArgument in Environment.GetCommandLineArgs())
                if (string.Equals(commandLineArgument, argument, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
