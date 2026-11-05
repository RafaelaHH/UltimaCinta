using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimaCinta.Interaction
{
    public sealed class SwitchInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Light controlledLight;
        [SerializeField] private GameObject controlledObject;
        [SerializeField] private GameObject controlledFixture;
        [SerializeField] private AudioClip failureClip;

        private readonly List<Material> emissionMaterials = new List<Material>();
        private readonly List<Color> originalEmissionColors = new List<Color>();
        private readonly List<bool> hasEmission = new List<bool>();
        private Coroutine flashRoutine;
        private bool _on = true;
        private bool yaFallo;

        public string Prompt => "E — toggle switch";

        private void Awake()
        {
            yaFallo = false;
            _on = true;
            if (controlledLight != null) controlledLight.enabled = true;
            if (controlledObject != null) controlledObject.SetActive(true);
            InitializeFixtureEmission();
            SetEmissionEnabled(true);
        }

        public void SetControlledFixture(GameObject fixture)
        {
            controlledFixture = fixture;
        }

        public void SetFailureClip(AudioClip clip)
        {
            failureClip = clip;
        }

        public bool Interact(GameObject interactor)
        {
            var firstFailure = !yaFallo;
            if (controlledLight == null)
                Debug.LogError($"Interruptor {name}: controlledLight es NULL.", this);

            if (flashRoutine != null)
            {
                LogLightingState(firstFailure);
                return false;
            }

            if (firstFailure)
            {
                StopActiveRoutines();
                yaFallo = true;
                _on = false;
                SetLightState(true);
                CreateSparks();
                if (failureClip != null) AudioSource.PlayClipAtPoint(failureClip, SparkPosition(), 1f);
                flashRoutine = StartCoroutine(FlashThenTurnOff());
            }
            else
            {
                StopActiveRoutines();
                _on = !_on;
                SetLightState(_on);
            }

            LogLightingState(firstFailure);
            return true;
        }

        private IEnumerator FlashThenTurnOff()
        {
            const int blinkCount = 6;
            const float blinkDuration = 0.04f;
            for (var blink = 0; blink < blinkCount; blink++)
            {
                var enabled = blink % 2 == 0;
                if (controlledLight != null) controlledLight.enabled = enabled;
                SetEmissionEnabled(enabled);
                yield return new WaitForSeconds(blinkDuration);
            }

            _on = false;
            SetLightState(false);
            flashRoutine = null;
        }

        private void StopActiveRoutines()
        {
            if (flashRoutine == null) return;
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        private void SetLightState(bool enabled)
        {
            if (controlledLight != null) controlledLight.enabled = enabled;
            if (controlledObject != null) controlledObject.SetActive(enabled);
            SetEmissionEnabled(enabled);
        }

        private void LogLightingState(bool firstFailure)
        {
            var lightName = controlledLight != null ? controlledLight.name : "NULL";
            var lightEnabled = controlledLight != null && controlledLight.enabled;
            Debug.Log($"Interruptor {name}: primera vez = {firstFailure.ToString().ToLowerInvariant()}, luz {lightName}, enabled={lightEnabled.ToString().ToLowerInvariant()}", this);

            var roomRoot = controlledLight != null ? controlledLight.transform.parent : transform.parent != null ? transform.parent.parent : null;
            if (roomRoot != null)
            {
                foreach (var roomLight in roomRoot.GetComponentsInChildren<Light>(true))
                    Debug.Log($"Luz recinto {roomRoot.name}: {roomLight.name}, enabled={roomLight.enabled}, intensidad={roomLight.intensity}", roomLight);
            }
            Debug.Log($"Iluminación ambiente: RenderSettings.ambientIntensity={RenderSettings.ambientIntensity}", this);
            foreach (var light in FindObjectsOfType<Light>(true))
                if (light.type == LightType.Directional)
                    Debug.Log($"Luz direccional {light.name}: enabled={light.enabled}, intensidad={light.intensity}", light);
        }

        private void OnDisable()
        {
            if (flashRoutine == null) return;
            StopActiveRoutines();
            _on = false;
            SetLightState(false);
        }

        private Vector3 SparkPosition()
        {
            if (controlledFixture != null)
            {
                var renderers = controlledFixture.GetComponentsInChildren<Renderer>(true);
                var foundBounds = false;
                var bounds = default(Bounds);
                foreach (var renderer in renderers)
                {
                    if (!renderer.enabled) continue;
                    if (!foundBounds) { bounds = renderer.bounds; foundBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (foundBounds) return bounds.center;
                return controlledFixture.transform.position;
            }
            if (controlledLight != null) return controlledLight.transform.position;
            return transform.position;
        }

        private void CreateSparks()
        {
            var sparkObject = new GameObject($"Chispas_{name}");
            sparkObject.transform.position = SparkPosition();
            var particles = sparkObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = 0.15f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
            main.startColor = Color.white;
            main.gravityModifier = 0.65f;
            main.maxParticles = 40;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.035f;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.28f, 0.02f), 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.08f), 0.55f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var material = new Material(shader) { name = $"RuntimeSparks_{name}" };
                sparkObject.GetComponent<ParticleSystemRenderer>().material = material;
                Destroy(material, 2f);
            }
            Destroy(sparkObject, 2f);
            particles.Play();
        }

        private void InitializeFixtureEmission()
        {
            if (controlledFixture == null) return;
            foreach (var renderer in controlledFixture.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    var supportsEmission = material != null && material.HasProperty("_EmissionColor");
                    var emission = supportsEmission ? material.GetColor("_EmissionColor") : Color.black;
                    var usesEmission = supportsEmission && (material.IsKeywordEnabled("_EMISSION") || emission.maxColorComponent > 0f);
                    emissionMaterials.Add(material);
                    hasEmission.Add(usesEmission);
                    originalEmissionColors.Add(emission.maxColorComponent > 0f ? emission : Color.white);
                    if (usesEmission) material.EnableKeyword("_EMISSION");
                }
                renderer.materials = materials;
            }
        }

        private void SetEmissionEnabled(bool enabled)
        {
            for (var index = 0; index < emissionMaterials.Count; index++)
            {
                var material = emissionMaterials[index];
                if (!hasEmission[index] || material == null) continue;
                material.SetColor("_EmissionColor", enabled ? originalEmissionColors[index] : Color.black);
            }
        }

        private void OnDestroy()
        {
            foreach (var material in emissionMaterials)
                if (material != null) Destroy(material);
        }
    }
}
