using System.Collections.Generic;
using UnityEngine;
using UltimaCinta.Ghost;
using UltimaCinta.Interaction;
using UltimaCinta.Player;
namespace UltimaCinta.Data
{
    [DisallowMultipleComponent]
    public sealed class RitualController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private GhostWorldController ghostWorld;
        [SerializeField] private GhostJournal journal;
        [SerializeField] private NightClock nightClock;
        [SerializeField] private Transform[] ritualItemPoints;
        [SerializeField] private Transform ritualCircle;
        [SerializeField] private Transform[] candlePoints;
        private GhostType _preparedType;
        private int _placedItems;
        private int _litCandles;
        private int _wave;
        private float _waveTimer;
        private bool _ritualActive;
        private bool _ritualFailed;
        private bool _ritualComplete;
        private float _deathCheckTimer;
        private SanityController[] _players = System.Array.Empty<SanityController>();
        private string _status = "Encuentra los tres objetos rituales y llevalos al sotano.";
        public GhostType PreparedType => _preparedType;
        public int PlacedItems => _placedItems;
        public int LitCandles => _litCandles;
        public void Configure(GameConfig config, GhostWorldController world, GhostJournal selectedJournal, NightClock clock,
            Transform[] itemPoints, Transform circle, Transform[] candles)
        {
            if (nightClock != null) nightClock.ActChanged -= OnActChanged;
            gameConfig = config; ghostWorld = world; journal = selectedJournal; nightClock = clock;
            ritualItemPoints = itemPoints; ritualCircle = circle; candlePoints = candles;
            if (nightClock != null) nightClock.ActChanged += OnActChanged;
        }
        private void OnEnable()
        {
            if (nightClock != null) nightClock.ActChanged += OnActChanged;
        }
        private void OnDisable()
        {
            if (nightClock != null) nightClock.ActChanged -= OnActChanged;
        }
        private void OnActChanged(NightAct act)
        {
            if (act == NightAct.Ritual && _placedItems == 3 && _litCandles == 3 && !_ritualActive && !_ritualComplete)
                StartRitual();
        }
        private void Awake()
        {
            if (journal == null) journal = FindFirstObjectByType<GhostJournal>();
            _players = FindObjectsByType<SanityController>(FindObjectsSortMode.None);
        }
        private void Update()
        {
            PrepareItemsWhenGhostChosen();
            if (!_ritualActive && !_ritualComplete && _placedItems == 3 && _litCandles == 3 &&
                nightClock != null && nightClock.Act == NightAct.Ritual)
                StartRitual();
            if (_ritualActive) UpdateWaves();
            if (nightClock == null) return;
            _deathCheckTimer -= Time.deltaTime;
            if (_deathCheckTimer <= 0f)
            {
                _deathCheckTimer = 1f;
                var alive = 0;
                foreach (var player in _players) if (player != null && !player.IsDead) alive++;
                if (_players.Length > 0 && alive == 0) nightClock.FinishDeath();
            }
        }
        private void PrepareItemsWhenGhostChosen()
        {
            var type = ghostWorld == null ? null : ghostWorld.CurrentType;
            if (type == null || type == _preparedType || type.RitualItems == null || type.RitualItems.Count != 3) return;
            _preparedType = type;
            if (ritualItemPoints == null || ritualItemPoints.Length < 3)
            {
                Debug.LogError("RitualController necesita al menos tres puntos POI_ObjetoRitual.", this);
                return;
            }
            var available = new List<Transform>(ritualItemPoints);
            for (var index = 0; index < type.RitualItems.Count; index++)
            {
                var pointIndex = Random.Range(0, available.Count);
                var point = available[pointIndex];
                available.RemoveAt(pointIndex);
                var pickupObject = new GameObject("ObjetoRitual_" + type.RitualItems[index].DisplayName);
                pickupObject.transform.SetPositionAndRotation(point.position, point.rotation);
                var collider = pickupObject.AddComponent<SphereCollider>();
                collider.radius = 0.7f;
                var pickup = pickupObject.AddComponent<PickupInteractable>();
                pickup.SetItem(type.RitualItems[index]);
            }
        }
        public bool TryPlace(GameObject interactor)
        {
            if (_preparedType == null || interactor == null) return false;
            var inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory == null) return false;
            if (_placedItems < 3)
            {
                var required = _preparedType.RitualItems[_placedItems];
                if (!inventory.TryRemove(required.DisplayName))
                {
                    inventory.ShowMessage("Falta " + required.DisplayName + " para el circulo.");
                    return false;
                }
                var visualRoot = new GameObject("Ofrenda_" + required.DisplayName);
                visualRoot.transform.SetParent(ritualCircle, false);
                visualRoot.transform.localPosition = Vector3.up * 0.12f + Quaternion.Euler(0f, 120f * _placedItems, 0f) * Vector3.forward * 0.65f;
                ItemVisualFactory.CreateVisual(required, visualRoot.transform, 0.35f);
                _placedItems++;
                _status = $"Objetos colocados: {_placedItems}/3";
                inventory.ShowMessage(_status);
                return true;
            }
            if (_litCandles < 3)
            {
                _litCandles++;
                if (candlePoints != null && _litCandles <= candlePoints.Length)
                {
                    var candleLight = candlePoints[_litCandles - 1].GetComponentInChildren<Light>(true);
                    if (candleLight != null) candleLight.enabled = true;
                    else CreateCandleLight(candlePoints[_litCandles - 1]);
                }
                _status = $"Velas encendidas: {_litCandles}/3";
                inventory.ShowMessage(_status);
                if (_litCandles == 3) StartRitual();
                return true;
            }
            return false;
        }
        private void CreateCandleLight(Transform point)
        {
            var lightObject = new GameObject("LlamaVela");
            lightObject.transform.SetParent(point, false);
            lightObject.transform.localPosition = Vector3.up * 0.45f;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.range = gameConfig.RitualCandleLightRange;
            light.intensity = gameConfig.RitualCandleLightIntensity;
        }
        private void StartRitual()
        {
            if (nightClock == null || nightClock.Act != NightAct.Ritual)
            {
                _status = "El circulo esta listo; espera al acto del ritual.";
                return;
            }
            if (ghostWorld == null || ghostWorld.Agent == null || gameConfig == null) return;
            _ritualActive = true;
            _wave = 0;
            _waveTimer = 0f;
            _status = "OLEADA 1/3 - Resiste dentro del circulo.";
            StartWave();
        }
        private void UpdateWaves()
        {
            _waveTimer += Time.deltaTime;
            if (_waveTimer < gameConfig.RitualWaveDurationSeconds) return;
            _waveTimer = 0f;
            _wave++;
            if (_wave >= 3)
            {
                _ritualActive = false;
                _ritualComplete = true;
                if (journal != null && journal.SelectedGhost == _preparedType)
                {
                    _status = "EL FANTASMA FUE EXPULSADO - VICTORIA";
                    nightClock.FinishVictory();
                }
                else
                {
                    _ritualFailed = true;
                    _status = "RITUAL FALLIDO - EL FANTASMA SE HA ENFURECIDO";
                    ghostWorld.Agent?.Enrage(gameConfig.RitualFailureSpeedMultiplier);
                }
                return;
            }
            _status = $"OLEADA {_wave + 1}/3 - La caceria se intensifica.";
            StartWave();
        }
        private void StartWave()
        {
            var ghost = ghostWorld == null ? null : ghostWorld.Agent;
            if (ghost == null) return;
            var scale = _wave + 1f;
            ghost.TriggerRitualHunt(gameConfig.RitualWaveHuntDurationMultiplier * scale,
                gameConfig.RitualWaveSpeedMultiplier * scale);
        }
        private void OnGUI()
        {
            if (nightClock == null || nightClock.Act != NightAct.Ritual) return;
            GUI.color = Color.white;
            GUI.Label(new Rect(24f, 56f, 620f, 28f), _status);
            if (_ritualFailed) GUI.Label(new Rect(24f, 84f, 620f, 24f), "Escapa o sobrevive hasta el amanecer.");
        }
    }
}
