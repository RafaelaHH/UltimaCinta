using System.Collections.Generic;
using System.IO;
using UltimaCinta.Data;
using UltimaCinta.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace UltimaCinta.CameraSystem
{
    public sealed class CamcorderRecording : MonoBehaviour
    {
        public static readonly List<CamcorderRecording> Sources = new();
        [SerializeField] private GameConfig gameConfig;
        private NetworkPlayerAvatar avatar;
        private CameraResourceController resources;
        private Camera source, capture;
        private RenderTexture target;
        private Texture2D pixels;
        private float nextFrame;
        private bool pending;
        private string folder;
        private long bytes;
        public Texture LiveTexture => target;
        public string Folder => folder;
        public bool Recording => avatar != null && avatar.IsNetworkStarted ? avatar.CameraPowered && !avatar.IsDead : resources != null && resources.IsPowered;
        public bool IsLocalRecording => avatar == null || !avatar.IsNetworkStarted || avatar.IsLocalPlayer;
        private void Start()
        {
            avatar = GetComponent<NetworkPlayerAvatar>();
            resources = GetComponent<CameraResourceController>();
            source = GetComponentInChildren<Camera>(true);
            if (source == null || gameConfig == null) { enabled = false; return; }
            var settings = gameConfig.Atmosphere;
            target = new RenderTexture(settings.RecordingWidth, settings.RecordingWidth * 9 / 16, 16);
            target.Create();
            pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            capture = new GameObject("Camcorder recording").AddComponent<Camera>();
            capture.CopyFrom(source);
            capture.enabled = false;
            capture.targetTexture = target;
            capture.farClipPlane = settings.MonitorFarClip;
            var data = capture.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            folder = Path.Combine(Application.persistentDataPath, "Grabaciones", System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            Sources.Add(this);
            RenderPipelineManager.endCameraRendering += FrameRendered;
        }
        private void LateUpdate()
        {
            if (capture == null || pending || !Recording || Time.unscaledTime < nextFrame) return;
            nextFrame = Time.unscaledTime + 1f / gameConfig.Atmosphere.RecordingFramesPerSecond;
            capture.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            if (avatar != null && avatar.IsNetworkStarted && !avatar.IsLocalPlayer)
                capture.transform.rotation = transform.rotation * Quaternion.Euler(avatar.ViewPitch, 0f, 0f);
            capture.fieldOfView = avatar != null && avatar.IsNetworkStarted ? avatar.CameraFieldOfView : source.fieldOfView;
            capture.cullingMask = source.cullingMask | (1 << LayerMask.NameToLayer("Ghost"));
            capture.enabled = true;
        }
        private void FrameRendered(ScriptableRenderContext context, Camera camera)
        {
            if (camera != capture || pending) return;
            capture.enabled = false;
            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                var previous = RenderTexture.active; RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                RenderTexture.active = previous; SaveFrame(pixels.EncodeToJPG(gameConfig.Atmosphere.RecordingJpegQuality));
                return;
            }
            pending = true;
            AsyncGPUReadback.Request(target, 0, TextureFormat.RGB24, request =>
            {
                pending = false;
                if (this == null || request.hasError || pixels == null) return;
                pixels.LoadRawTextureData(request.GetData<byte>());
                SaveFrame(pixels.EncodeToJPG(gameConfig.Atmosphere.RecordingJpegQuality));
            });
        }
        private void SaveFrame(byte[] jpg)
        {
            if (avatar != null && avatar.IsNetworkStarted && !avatar.IsLocalPlayer) return;
            if (bytes + jpg.Length > gameConfig.Atmosphere.RecordingBudgetMegabytes * 1024L * 1024L) return;
            bytes += jpg.Length;
            try { File.WriteAllBytes(Path.Combine(folder, System.DateTime.UtcNow.Ticks + ".jpg"), jpg); }
            catch (IOException e) { Debug.LogWarning("Recording save failed: " + e.Message); }
        }
        private void OnDestroy()
        {
            Sources.Remove(this);
            RenderPipelineManager.endCameraRendering -= FrameRendered;
            if (capture != null) Destroy(capture.gameObject);
            if (target != null) { target.Release(); Destroy(target); }
            if (pixels != null) Destroy(pixels);
        }
    }
}
