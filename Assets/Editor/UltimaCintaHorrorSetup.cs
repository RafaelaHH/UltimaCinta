#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UltimaCinta.Editor
{
    internal static class UltimaCintaHorrorSetup
    {
        private static readonly string[] ConfigPaths =
        {
            "Assets/Scripts/Data/GameConfig.asset",
            "Assets/Scripts/Data/GameConfig_PhaseFourTest.asset"
        };

        [MenuItem("Tools/UltimaCinta/Aplicar terror de apariciones")]
        public static void ApplyScreamerProfile()
        {
            foreach (var path in ConfigPaths)
            {
                var config = AssetDatabase.LoadAssetAtPath<UltimaCinta.Data.GameConfig>(path);
                if (config == null)
                {
                    Debug.LogWarning($"No se encontró configuración: {path}");
                    continue;
                }

                var serializedConfig = new SerializedObject(config);
                serializedConfig.FindProperty("ghostScreamerRangeMeters").floatValue = 6f;
                serializedConfig.FindProperty("ghostScreamerDurationSeconds").floatValue = 0.9f;
                serializedConfig.FindProperty("ghostScreamerAudioVolume").floatValue = 0.9f;
                serializedConfig.FindProperty("ghostScreamerFovKickDegrees").floatValue = 18f;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                Debug.Log($"Perfil de apariciones aplicado a {path}.");
            }

            AssetDatabase.SaveAssets();
        }
    }
}
#endif
