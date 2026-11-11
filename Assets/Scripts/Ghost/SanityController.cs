using System;
using UnityEngine;
using UltimaCinta.Data;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class SanityController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private float currentSanity = -1f;
        [SerializeField] private Camera playerCamera;
        private float _hallucinationTimer;
        private float _hallucinationDisplayTimer;
        private bool _dead;
        private string _hallucination;
        private Light[] _nearbyLights = System.Array.Empty<Light>();
        private float _lightRefreshTimer;
        private float _playerRefreshTimer;
        private UltimaCinta.Player.FirstPersonMovementController[] _players = System.Array.Empty<UltimaCinta.Player.FirstPersonMovementController>();
        private GhostAgent _ghost;
        private float _ghostRefreshTimer;
        private float _whisperTimer;
        private GameObject _hallucinationVisual;
        private AudioSource _fearAudio;
        private AudioSource _breathingAudio;
        private UltimaCinta.Player.StaminaResource _stamina;
        private static AudioClip _whisperClip;
        private static AudioClip _breathingClip;
        public float Value => currentSanity;
        public bool IsDead => _dead;
        public event Action<float> SanityChanged;

        public void Reduce(float amount) { if (!_dead && amount > 0f) { currentSanity = Mathf.Max(0f, currentSanity - amount); Debug.Log($"[Fase 3] Cordura reducida por estímulo externo: {currentSanity:0}.", this); } }

        private void Awake()
        {
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (gameConfig == null) gameConfig = Resources.Load<GameConfig>("GameConfig");
            if (currentSanity < 0f && gameConfig != null) currentSanity = gameConfig.StartingSanity;
            _fearAudio = GetComponent<AudioSource>();
            _stamina = GetComponent<UltimaCinta.Player.StaminaResource>();
            if (_fearAudio == null) _fearAudio = gameObject.AddComponent<AudioSource>();
            _fearAudio.playOnAwake = false;
            _fearAudio.loop = false;
            _fearAudio.spatialBlend = 0f;
            if (_whisperClip == null) _whisperClip = CreateWhisperClip();
            _breathingAudio = gameObject.AddComponent<AudioSource>();
            _breathingAudio.playOnAwake = false;
            _breathingAudio.loop = true;
            _breathingAudio.spatialBlend = 0f;
            if (_breathingClip == null) _breathingClip = CreateBreathingClip();
            _breathingAudio.clip = _breathingClip;
            _nearbyLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            _players = FindObjectsByType<UltimaCinta.Player.FirstPersonMovementController>(FindObjectsSortMode.None);
            _ghost = FindFirstObjectByType<GhostAgent>();
        }

        private void Update()
        {
            if (_dead || gameConfig == null) return;
            _hallucinationTimer += Time.deltaTime;
            _hallucinationDisplayTimer = Mathf.Max(0f, _hallucinationDisplayTimer - Time.deltaTime);
            _whisperTimer += Time.deltaTime;
            if (_hallucinationVisual != null && _hallucinationDisplayTimer <= 0f) Destroy(_hallucinationVisual);
            var nearGhost = false;
            _ghostRefreshTimer -= Time.deltaTime;
            if (_ghostRefreshTimer <= 0f)
            {
                _ghost = FindFirstObjectByType<GhostAgent>();
                _ghostRefreshTimer = gameConfig.GhostTargetRefreshSeconds;
            }
            var ghost = _ghost;
            if (ghost != null)
            {
                var distance = Vector3.Distance(transform.position, ghost.transform.position);
                nearGhost = distance < gameConfig.GhostInterferenceRange;
                if (nearGhost) currentSanity -= gameConfig.GhostSanityDrainPerSecond * Time.deltaTime;
            }
            _lightRefreshTimer -= Time.deltaTime;
            if (_lightRefreshTimer <= 0f)
            {
                _nearbyLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                _lightRefreshTimer = gameConfig.SanityLightRefreshSeconds;
            }
            var lit = false;
            foreach (var light in _nearbyLights)
                if (light.enabled && light.intensity > 0f && Vector3.Distance(light.transform.position, transform.position) < gameConfig.GhostInterferenceRange)
                { lit = true; break; }
            if (lit) currentSanity += gameConfig.SanityLightRecoveryPerSecond * Time.deltaTime;
            else currentSanity -= gameConfig.SanityDarknessDrainPerSecond * Time.deltaTime;
            _playerRefreshTimer -= Time.deltaTime;
            if (_playerRefreshTimer <= 0f)
            {
                _players = FindObjectsByType<UltimaCinta.Player.FirstPersonMovementController>(FindObjectsSortMode.None);
                _playerRefreshTimer = gameConfig.SanityPlayerRefreshSeconds;
            }
            if (CountNearbyPlayers() > 1) currentSanity += gameConfig.SanityCompanionRecoveryPerSecond * Time.deltaTime;
            currentSanity = Mathf.Clamp(currentSanity, 0f, gameConfig.MaximumSanity);

            var atZero = currentSanity <= 0.01f;
            var hallucinationInterval = atZero ? gameConfig.ZeroSanityHallucinationIntervalSeconds : gameConfig.HallucinationIntervalSeconds;
            if (currentSanity < gameConfig.HallucinationSanityThreshold && _hallucinationTimer >= hallucinationInterval)
            {
                _hallucinationTimer = 0f;
                _hallucinationDisplayTimer = atZero ? gameConfig.HallucinationDisplayAtZeroSeconds : gameConfig.HallucinationDisplaySeconds;
                _hallucination = UnityEngine.Random.Range(0, 3) switch
                {
                    0 => "NO ESTÁS SOLO",
                    1 => "MIRA DETRÁS DE TI",
                    _ => "SAL DE AQUÍ"
                };
                PlayWhisper();
                if (atZero) ShowHallucination();
                Debug.Log($"[Fase 3] Alucinación de cordura activada; cordura={currentSanity:0}; visible={!string.IsNullOrEmpty(_hallucination)}.", this);
            }
            if (currentSanity < gameConfig.HallucinationSanityThreshold && _whisperTimer >= gameConfig.SanityWhisperIntervalSeconds)
            {
                _whisperTimer = 0f;
                PlayWhisper();
            }
            UpdateBreathing();
            SanityChanged?.Invoke(currentSanity);
        }

        public bool RestoreFromPills()
        {
            if (_dead || gameConfig == null || currentSanity >= gameConfig.MaximumSanity) return false;
            currentSanity = Mathf.Min(gameConfig.MaximumSanity, currentSanity + gameConfig.SanityPillRestore);
            _hallucinationTimer = 0f;
            if (_hallucinationVisual != null) Destroy(_hallucinationVisual);
            _hallucinationDisplayTimer = 0f;
            if (_fearAudio != null) _fearAudio.Stop();
            UpdateBreathing();
            SanityChanged?.Invoke(currentSanity);
            return true;
        }

        private void UpdateBreathing()
        {
            if (_breathingAudio == null || gameConfig == null) return;
            var sanityDistress = currentSanity < gameConfig.SanityBreathingStartThreshold
                ? 1f - currentSanity / Mathf.Max(1f, gameConfig.SanityBreathingStartThreshold)
                : 0f;
            var staminaValue = _stamina != null ? _stamina.NormalizedValue : 1f;
            var staminaDistress = staminaValue < gameConfig.StaminaBreathingStartThreshold
                ? 1f - staminaValue / Mathf.Max(0.01f, gameConfig.StaminaBreathingStartThreshold)
                : 0f;
            var distress = Mathf.Max(sanityDistress, staminaDistress);
            if (distress <= 0f)
            {
                if (_breathingAudio.isPlaying) _breathingAudio.Stop();
                return;
            }
            var volume = Mathf.Max(sanityDistress * gameConfig.SanityBreathingMaximumVolume,
                staminaDistress * gameConfig.StaminaBreathingMaximumVolume);
            var maxRate = staminaDistress > sanityDistress ? gameConfig.StaminaBreathingMaximumRate : gameConfig.SanityBreathingMaximumRate;
            _breathingAudio.volume = volume;
            _breathingAudio.pitch = Mathf.Lerp(0.85f, maxRate, distress);
            if (!_breathingAudio.isPlaying) _breathingAudio.Play();
        }

        private static AudioClip CreateBreathingClip()
        {
            const int sampleRate = 22050;
            const int sampleCount = 44100;
            var samples = new float[sampleCount];
            var noise = 0f;
            for (var index = 0; index < sampleCount; index++)
            {
                var time = (float)index / sampleRate;
                var cycle = time / 2f;
                var inhale = cycle < 0.44f ? Mathf.Sin(Mathf.PI * cycle / 0.44f) : 0f;
                var exhalePhase = (cycle - 0.56f) / 0.44f;
                var exhale = exhalePhase > 0f ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(exhalePhase)) * 0.72f : 0f;
                noise = noise * 0.9f + (UnityEngine.Random.value * 2f - 1f) * 0.1f;
                var breathTone = Mathf.Sin(time * 2f * Mathf.PI * 92f) * 0.08f;
                samples[index] = (noise + breathTone) * Mathf.Max(inhale, exhale) * 0.32f;
            }
            var clip = AudioClip.Create("Respiración agitada", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void ShowHallucination()
        {
            if (_ghost == null || _ghost.Type == null || _ghost.Type.GhostPrefab == null || playerCamera == null) return;
            if (_hallucinationVisual != null) Destroy(_hallucinationVisual);
            _hallucinationVisual = Instantiate(_ghost.Type.GhostPrefab);
            _hallucinationVisual.name = "Alucinación de cordura";
            var forward = playerCamera.transform.forward;
            _hallucinationVisual.transform.position = playerCamera.transform.position + forward * gameConfig.HallucinationDistanceMeters
                - playerCamera.transform.right * 0.45f;
            _hallucinationVisual.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            foreach (var component in _hallucinationVisual.GetComponentsInChildren<MonoBehaviour>(true))
                if (component is GhostAgent || component is UnityEngine.AI.NavMeshAgent || component is GhostCameraInterference)
                    component.enabled = false;
            foreach (var collider in _hallucinationVisual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            SetLayerRecursively(_hallucinationVisual.transform, LayerMask.NameToLayer("Ghost"));
        }

        private void PlayWhisper()
        {
            if (_fearAudio == null || _whisperClip == null || gameConfig == null) return;
            _fearAudio.PlayOneShot(_whisperClip, gameConfig.SanityWhisperVolume);
        }

        private static AudioClip CreateWhisperClip()
        {
            const int sampleRate = 22050;
            const int sampleCount = 33075;
            var samples = new float[sampleCount];
            var noise = 0f;
            for (var index = 0; index < sampleCount; index++)
            {
                var time = (float)index / sampleRate;
                var envelope = Mathf.Sin(Mathf.PI * index / (sampleCount - 1)) * (0.55f + 0.45f * Mathf.Sin(time * 13f));
                noise = noise * 0.94f + (UnityEngine.Random.value * 2f - 1f) * 0.06f;
                var murmur = Mathf.Sin(time * 2f * Mathf.PI * 170f) * 0.22f + Mathf.Sin(time * 2f * Mathf.PI * 255f) * 0.12f;
                samples[index] = (noise + murmur) * envelope * 0.24f;
            }
            var clip = AudioClip.Create("Susurro espectral", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (layer < 0) return;
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }

        private void OnDisable()
        {
            if (_hallucinationVisual != null) Destroy(_hallucinationVisual);
            if (_breathingAudio != null) _breathingAudio.Stop();
        }

        public void Die()
        {
            if (_dead) return;
            _dead = true;
            Debug.Log($"[Fase 3] Jugador muerto; activa modo espectador con cordura={currentSanity:0}.", this);
            var movement = GetComponent<UltimaCinta.Player.FirstPersonMovementController>();
            var look = GetComponent<UltimaCinta.Player.FirstPersonLookController>();
            var interaction = GetComponent<UltimaCinta.Interaction.PlayerInteractionController>();
            if (movement != null) movement.enabled = false;
            if (look != null) look.enabled = false;
            if (interaction != null) interaction.enabled = false;
            var spectator = GetComponent<GhostSpectatorController>();
            if (spectator == null) spectator = gameObject.AddComponent<GhostSpectatorController>();
            spectator.SetGameConfig(gameConfig);
            spectator.enabled = true;
        }

        private int CountNearbyPlayers()
        {
            var count = 0;
            foreach (var player in _players)
                if (player != null && Vector3.Distance(transform.position, player.transform.position) <= gameConfig.SanityCompanionRange) count++;
            return count;
        }

        private void OnGUI()
        {
            if (gameConfig == null) return;
            var atZero = currentSanity <= 0.01f;
            if (atZero)
            {
                var pulse = 0.08f + Mathf.PingPong(Time.unscaledTime * 0.65f, 0.13f);
                GUI.color = new Color(0.32f, 0f, 0f, pulse);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            var sanityStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = atZero ? new Color(1f, 0.18f, 0.18f) : Color.white }
            };
            GUI.Label(new Rect(24f, 18f, 220f, 30f), $"CORDURA {currentSanity:0}", sanityStyle);
            if (_hallucinationDisplayTimer > 0f && !string.IsNullOrEmpty(_hallucination))
            {
                GUI.color = new Color(1f, 0.86f, 0.86f, Mathf.Clamp01(_hallucinationDisplayTimer));
                var messageStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = atZero ? 34 : 24,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true
                };
                GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.36f, Screen.width * 0.6f, 110f), _hallucination, messageStyle);
                GUI.color = Color.white;
            }
        }
    }
}
