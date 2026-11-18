using System.IO;
using UltimaCinta.CameraSystem;
using UnityEngine;
namespace UltimaCinta.Interaction
{
    public sealed class VanMonitor : MonoBehaviour, IInteractable
    {
        private Renderer screen;
        private int channel;
        private bool replay;
        private string[] frames;
        private int frame;
        private float next;
        private float interval;
        private Texture2D playback;
        private Material screenMaterial;
        public string Prompt => "E — cambiar cámara / ver grabación";
        public static void Create(Transform parent, Vector3 position, VanSupplyInteractable van)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Monitor de grabaciones";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(0.08f, 0.8f, 1.42f);
            var monitor = go.AddComponent<VanMonitor>();
            monitor.interval = 1f / van.Config.Atmosphere.RecordingFramesPerSecond;
            monitor.screen = go.GetComponent<Renderer>();
            var sourceMaterial = Resources.Load<Material>("VanMonitorMaterial");
            if (sourceMaterial == null)
            {
                Debug.LogError("Falta Assets/Resources/VanMonitorMaterial.mat; se desactiva el monitor para evitar errores.", go);
                monitor.screen.enabled = false;
                return;
            }
            monitor.screenMaterial = new Material(sourceMaterial);
            monitor.screen.sharedMaterial = monitor.screenMaterial;
        }
        public bool Interact(GameObject player)
        {
            var sources = CamcorderRecording.Sources;
            if (sources.Count == 0) return false;
            replay = !replay;
            if (!replay) channel = (channel + 1) % sources.Count;
            else
            {
                channel %= sources.Count;
                var localChannel = System.Array.FindIndex(sources.ToArray(), source => source == null || source.IsLocalRecording);
                if (localChannel >= 0) channel = localChannel;
                frames = Directory.GetFiles(sources[channel].Folder, "*.jpg");
                System.Array.Sort(frames);
                frame = 0;
            }
            return true;
        }
        private void Update()
        {
            var sources = CamcorderRecording.Sources;
            if (sources.Count == 0 || screen == null) return;
            channel %= sources.Count;
            if (!replay) { screenMaterial.mainTexture = sources[channel].Recording ? sources[channel].LiveTexture : Texture2D.blackTexture; return; }
            if (frames == null || frames.Length == 0 || Time.unscaledTime < next) return;
            next = Time.unscaledTime + interval;
            if (playback == null) playback = new Texture2D(2, 2);
            try { playback.LoadImage(File.ReadAllBytes(frames[frame++ % frames.Length])); screenMaterial.mainTexture = playback; }
            catch (IOException) { replay = false; }
        }
        private void OnDestroy()
        {
            if (playback != null) Destroy(playback);
            if (screenMaterial != null) Destroy(screenMaterial);
        }
    }
}
