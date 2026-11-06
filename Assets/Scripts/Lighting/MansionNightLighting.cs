using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UltimaCinta.Lighting
{
    [DisallowMultipleComponent]
    public sealed class MansionNightLighting : MonoBehaviour
    {
        private const float DirectionalIntensity = 0.03f;
        private const float AmbientIntensity = 0.25f;
        private const float ReflectionIntensity = 0.08f;
        private static readonly Color NightBlue = new Color(0.035f, 0.045f, 0.075f, 1f);
        private static readonly Color DirectionalBlue = new Color(0.35f, 0.45f, 0.7f, 1f);
        private static readonly Color CameraClearColor = new Color(0.006f, 0.009f, 0.018f, 1f);

        private void Awake()
        {
            ApplyToScene(gameObject.scene);
        }

        public static void EnsureForScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<MansionNightLighting>(true) != null) return;

            var lightingRoot = new GameObject("MansionNightLighting");
            SceneManager.MoveGameObjectToScene(lightingRoot, scene);
            lightingRoot.AddComponent<MansionNightLighting>();
        }

        public static void ApplyToScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = NightBlue;
            RenderSettings.ambientIntensity = AmbientIntensity;
            RenderSettings.reflectionIntensity = ReflectionIntensity;
            RenderSettings.skybox = null;

            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.gameObject.scene != scene || light.type != LightType.Directional) continue;
                light.color = DirectionalBlue;
                light.intensity = DirectionalIntensity;
                light.shadows = LightShadows.None;
            }

            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (camera.gameObject.scene != scene) continue;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = CameraClearColor;
            }

            Debug.Log($"Iluminación nocturna aplicada en {scene.name}: direccional={DirectionalIntensity:0.00}, " +
                      $"ambiente={NightBlue} x {AmbientIntensity:0.00}, reflejos={ReflectionIntensity:0.00}, skybox=NULL.");
        }
    }
}
