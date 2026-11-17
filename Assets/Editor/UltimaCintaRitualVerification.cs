#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UltimaCinta.Data;
using UltimaCinta.Ghost;
using UltimaCinta.Interaction;
using UltimaCinta.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UltimaCinta.Editor
{
    internal static class UltimaCintaRitualVerification
    {
        private const string ScenePath = "Assets/Scenes/Mansion_Generated.unity";
        private const string ConfigPath = "Assets/Scripts/Data/GameConfig_PhaseFourTest.asset";
        private static readonly string[] GhostPaths =
        {
            "Assets/Scripts/Data/Ghosts/Ghoul.asset",
            "Assets/Scripts/Data/Ghosts/Vampiro.asset",
            "Assets/Scripts/Data/Ghosts/Poltergeist.asset",
            "Assets/Scripts/Data/Ghosts/Espectro.asset"
        };

        [MenuItem("Tools/UltimaCinta/Verificar rituales de los cuatro fantasmas")]
        public static void VerifyAllRituals()
        {
            var results = new List<string>();
            foreach (var ghostPath in GhostPaths)
            {
                var ghostType = AssetDatabase.LoadAssetAtPath<GhostType>(ghostPath);
                if (ghostType == null) throw new InvalidOperationException($"Falta el GhostType {ghostPath}.");
                RunCase(ghostType, ghostType, results);
                var wrongChoice = AssetDatabase.LoadAssetAtPath<GhostType>(
                    GhostPaths[(Array.IndexOf(GhostPaths, ghostPath) + 1) % GhostPaths.Length]);
                RunCase(ghostType, wrongChoice, results);
            }

            var logPath = Path.GetFullPath("Logs/ritual-verification-results.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllLines(logPath, results);
            foreach (var result in results) Debug.Log(result);
            Debug.Log($"Prueba de rituales completada: {results.Count} casos. Resultado: {logPath}");
        }

        private static void RunCase(GhostType expected, GhostType selection, List<string> results)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var world = UnityEngine.Object.FindFirstObjectByType<GhostWorldController>();
            var ritual = UnityEngine.Object.FindFirstObjectByType<RitualController>();
            var clock = UnityEngine.Object.FindFirstObjectByType<NightClock>();
            if (config == null || world == null || ritual == null || clock == null)
                throw new InvalidOperationException("La escena de mansión no tiene config, fantasma, reloj o ritual asignado.");

            var worldSerialized = new SerializedObject(world);
            worldSerialized.FindProperty("forcedGhostForTesting").objectReferenceValue = expected;
            worldSerialized.FindProperty("randomizeAtStart").boolValue = false;
            worldSerialized.ApplyModifiedPropertiesWithoutUndo();
            world.InitializeSession();
            if (world.CurrentType != expected)
                throw new InvalidOperationException($"Se esperaba {expected.DisplayName}, pero se preparó {world.CurrentType?.DisplayName ?? "ninguno"}.");

            SetField(ritual, "_preparedType", expected);
            SetField(clock, "_localAct", NightAct.Ritual);
            clock.Configure(config);
            var journalObject = new GameObject("Journal_RitualTest");
            var journal = journalObject.AddComponent<GhostJournal>();
            SetField(journal, "_localSelectedGhost", selection);
            ritual.Configure(config, world, journal, clock,
                (Transform[])GetField(ritual, "ritualItemPoints"),
                (Transform)GetField(ritual, "ritualCircle"),
                (Transform[])GetField(ritual, "candlePoints"));

            var playerObject = new GameObject("Player_RitualTest");
            var inventory = playerObject.AddComponent<PlayerInventory>();
            SetField(inventory, "_items", new InventoryItem[3]);
            foreach (var item in expected.RitualItems)
            {
                if (!inventory.TryAdd(item)) throw new InvalidOperationException($"No se pudo añadir {item.DisplayName} para {expected.DisplayName}.");
            }

            for (var index = 0; index < 3; index++)
                if (!ritual.TryPlace(playerObject)) throw new InvalidOperationException($"No se pudo colocar el objeto {index + 1} de {expected.DisplayName}.");
            for (var index = 0; index < 3; index++)
                if (!ritual.TryPlace(playerObject)) throw new InvalidOperationException($"No se pudo encender la vela {index + 1} de {expected.DisplayName}.");

            if (!(bool)GetField(ritual, "_ritualActive"))
                throw new InvalidOperationException($"El ritual de {expected.DisplayName} no inició tras colocar sus objetos y velas.");

            var advanceWaves = typeof(RitualController).GetMethod("UpdateWaves", BindingFlags.Instance | BindingFlags.NonPublic);
            for (var wave = 0; wave < 3; wave++)
            {
                SetField(ritual, "_waveTimer", config.RitualWaveDurationSeconds);
                advanceWaves.Invoke(ritual, null);
            }

            var victoryExpected = expected == selection;
            var complete = (bool)GetField(ritual, "_ritualComplete");
            var failed = (bool)GetField(ritual, "_ritualFailed");
            var nightFinished = (bool)GetField(clock, "_localFinished");
            if (!complete || failed == victoryExpected || nightFinished != victoryExpected)
                throw new InvalidOperationException($"Resultado incorrecto en ritual {expected.DisplayName} con diario {selection.DisplayName}: complete={complete}, failed={failed}, nightFinished={nightFinished}.");

            results.Add($"PASS | Fantasma={expected.DisplayName} | Objetos=3/3 | Velas=3/3 | Oleadas=3 | Diario={selection.DisplayName} | Resultado={(victoryExpected ? "victoria" : "fallo/enfurecimiento")}");
        }

        private static object GetField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().Name, fieldName);
            return field.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().Name, fieldName);
            field.SetValue(target, value);
        }
    }
}
#endif
