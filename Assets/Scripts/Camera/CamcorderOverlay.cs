using UnityEngine;
namespace UltimaCinta.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CamcorderOverlay : MonoBehaviour
    {
        [SerializeField] private CameraResourceController resources;
        private float _recordingStartTime;
        private void Awake() { if (resources == null) resources = GetComponent<CameraResourceController>(); _recordingStartTime = Time.timeSinceLevelLoad; }
        private void OnGUI()
        {
            if (resources != null && resources.IsPowered && resources.NightVisionEnabled)
            {
                GUI.color = new Color(0.2f, 1f, 0.3f, 0.22f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (resources == null || !resources.IsPowered) { GUI.Label(new Rect(24, 24, 400, 32), "CAMERA OFF  |  Press F to use flashlight"); return; }
            GUI.color = Color.white;
            if (Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0)
            {
                GUI.color = new Color(1f, 0.15f, 0.12f);
                GUI.Label(new Rect(24, 20, 180, 28), "● REC");
            }
            else GUI.Label(new Rect(24, 20, 180, 28), "● REC");
            GUI.color = Color.white;
            GUI.Label(new Rect(24, 52, 280, 24), RecordingTime());
            GUI.Label(new Rect(24, 78, 220, 24), $"BATTERY {resources.BatteryNormalized * 100f:0}%");
            GUI.Label(new Rect(24, 102, 220, 24), $"TAPE {resources.TapeNormalized * 100f:0}%");
            GUI.Label(new Rect(24, 126, 220, 24), resources.NightVisionEnabled ? "NIGHT VISION ON" : "NIGHT VISION OFF [N]");
            if (resources.BatteryNormalized < resources.WarningThreshold || resources.TapeNormalized < resources.WarningThreshold) GUI.Label(new Rect(24, 154, 300, 24), "LOW RESOURCE");
        }
        private string RecordingTime() { var elapsed = System.TimeSpan.FromSeconds(Time.timeSinceLevelLoad - _recordingStartTime); return $"TAPE {elapsed:hh\\:mm\\:ss}"; }
    }
}
