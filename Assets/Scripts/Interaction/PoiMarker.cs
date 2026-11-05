using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PoiMarker : MonoBehaviour
    {
        private const float SphereDiameter = 0.3f;

        public static string TypeLabel(string markerName)
        {
            if (markerName.StartsWith("POI_SalaFavorita", System.StringComparison.OrdinalIgnoreCase)) return "Sala favorita";
            if (markerName.StartsWith("POI_ObjetoRitual", System.StringComparison.OrdinalIgnoreCase) ||
                markerName.StartsWith("POI_Ritual", System.StringComparison.OrdinalIgnoreCase)) return "Objeto de ritual";
            if (markerName.StartsWith("POI_Evidencia", System.StringComparison.OrdinalIgnoreCase)) return "Evidencia";
            return "POI";
        }

        private void OnDrawGizmos()
        {
            var label = TypeLabel(name);
            Gizmos.color = label == "Evidencia" ? new Color(0.2f, 0.85f, 1f) :
                label == "Objeto de ritual" ? new Color(1f, 0.3f, 0.75f) : new Color(0.35f, 1f, 0.35f);
            Gizmos.DrawSphere(transform.position, SphereDiameter * 0.5f);
#if UNITY_EDITOR
            Handles.color = Gizmos.color;
            Handles.Label(transform.position + Vector3.up * (SphereDiameter * 0.65f), name);
#endif
        }
    }
}
