#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UltimaCinta.Editor
{
    public static class UltimaCintaGeneratedMansionRestore
    {
        [MenuItem("Tools/UltimaCinta/Restaurar y validar mansión generada")]
        public static void RestoreAndVerify()
        {
            UltimaCintaPhaseThreeSetup.CompletePlayableSceneSetup();
            UltimaCintaPhaseFourSetup.ConfigureMansion();
            global::UltimaCintaHorrorSetup.Prepare();
            UltimaCintaPhaseFiveSetup.PreparePhaseFive();
            UltimaCintaPhaseSixSetup.PreparePhaseSix();
            MansionNavMeshBaker.BakeMansionNavMesh();
            UltimaCintaPhaseFourSetup.CreateAcceleratedTestScene();
            UltimaCintaRitualVerification.VerifyAllRituals();
            UltimaCintaPhaseSixSetup.VerifyPhaseSix();
            AssetDatabase.SaveAssets();
            Debug.Log("Mansión generada restaurada; reloj, fantasma, ritual, multiplayer, audio, cámara y pruebas QA listos.");
        }

        [MenuItem("Tools/UltimaCinta/Construir juego Windows")]
        public static void BuildWindowsPlayer()
        {
            var outputPath = "Builds/PhaseSix/UltimaCinta.exe";
            var arguments = System.Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
                if (arguments[index] == "-buildOutput") { outputPath = arguments[index + 1]; break; }

            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);
            if (scenes.Count == 0) throw new System.InvalidOperationException("No hay escenas activas en Build Settings.");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("No se pudo construir la build Windows: " + report.summary.result);
            Debug.Log($"Build Windows lista: {report.summary.outputPath} | escenas={scenes.Count}");
        }
    }
}
#endif
