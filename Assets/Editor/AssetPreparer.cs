#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UltimaCinta.Editor
{
    public static class AssetPreparer
    {
        private const string MenuPath = "Tools/UltimaCinta/1. Preparar assets";
        private const string GeneratedRoot = "Assets/Generated";
        private const string ExcludedMaterialsFolder = "Assets/Generated/MansionMaterials/";
        private const string TextureFolderName = "Texturas";

        [MenuItem(MenuPath)]
        public static void PrepareAssets()
        {
            var processed = 0;
            var removedMaterials = 0;
            var createdFolders = 0;
            var extractedTextures = 0;
            var respectedModels = new List<string>();
            var noTextureModels = new List<string>();
            var failures = new List<string>();
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { GeneratedRoot });

            foreach (var guid in fbxGuids)
            {
                var modelPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!modelPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                    modelPath.StartsWith(ExcludedMaterialsFolder, StringComparison.OrdinalIgnoreCase)) continue;

                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null)
                {
                    failures.Add(modelPath + " (ModelImporter no disponible)");
                    continue;
                }

                var directory = Path.GetDirectoryName(modelPath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(directory))
                {
                    failures.Add(modelPath + " (ruta de carpeta inválida)");
                    continue;
                }

                var textureRoot = directory + "/" + TextureFolderName;
                var modelTextureFolder = textureRoot + "/" + NormalizeModelName(Path.GetFileNameWithoutExtension(modelPath));
                createdFolders += EnsureFolder(modelTextureFolder);

                var textureDirectory = Path.Combine(projectRoot, modelTextureFolder.Replace('/', Path.DirectorySeparatorChar));
                if (CountTextures(textureDirectory) > 0)
                {
                    respectedModels.Add(modelPath);
                }
                else
                {
                    var before = CountTextures(textureDirectory);
                    try
                    {
                        importer.ExtractTextures(modelTextureFolder);
                    }
                    catch (Exception exception)
                    {
                        failures.Add(modelPath + " (extracción: " + exception.Message + ")");
                    }

                    var after = CountTextures(textureDirectory);
                    extractedTextures += Math.Max(0, after - before);
                    if (after == 0) noTextureModels.Add(modelPath);
                }

                try
                {
                    var remaps = importer.GetExternalObjectMap();
                    var materialKeys = new List<AssetImporter.SourceAssetIdentifier>();
                    foreach (var remap in remaps)
                        if (remap.Key.type == typeof(Material)) materialKeys.Add(remap.Key);

                    foreach (var key in materialKeys)
                    {
                        importer.RemoveRemap(key);
                        removedMaterials++;
                    }

                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.SaveAndReimport();
                    processed++;
                }
                catch (Exception exception)
                {
                    failures.Add(modelPath + " (remaps/reimportación: " + exception.Message + ")");
                }
            }

            AssetDatabase.Refresh();
            var noTextureText = noTextureModels.Count == 0 ? "ninguno" : string.Join(", ", noTextureModels);
            var respectedText = respectedModels.Count == 0 ? "ninguno" : string.Join(", ", respectedModels);
            var failureText = failures.Count == 0 ? "ninguno" : string.Join("; ", failures);
            Debug.Log($"Assets preparados: FBX procesados {processed}, materiales quitados {removedMaterials}, carpetas creadas {createdFolders}, texturas extraídas {extractedTextures}, FBX respetados (ya tenían texturas) {respectedModels.Count} [{respectedText}], FBX sin textura [{noTextureText}], incidencias [{failureText}].");
        }

        private static int EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return 0;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);
            var created = 0;
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) created += EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
                created++;
            }
            return created;
        }

        private static string NormalizeModelName(string name)
        {
            var decomposed = name.Normalize(NormalizationForm.FormD);
            var result = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(character)) result.Append(char.ToLowerInvariant(character));
            }
            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        private static int CountTextures(string directory)
        {
            if (!Directory.Exists(directory)) return 0;
            var count = 0;
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                var extension = Path.GetExtension(file);
                if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".tga", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".psd", StringComparison.OrdinalIgnoreCase) || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".exr", StringComparison.OrdinalIgnoreCase)) count++;
            }
            return count;
        }
    }
}
#endif
