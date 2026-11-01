using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WonderJump.Core;
using WonderJump.EditorTools.Generation;

namespace WonderJump.EditorTools.Setup
{
    /// <summary>
    /// One-click, idempotent project setup. Running any of these twice produces the same result.
    /// </summary>
    public static class WonderJumpProjectSetup
    {
        private const string Menu = "Wonder Jump/Setup/";
        public const string ProductName = "Wonder Jump - Adventure Worlds";
        public const int MinAndroidApi = 26;
        public const string BootstrapScenePath = GeneratorOptions.DefaultContentRoot + "/Scenes/Bootstrap.unity";

        // ------------------------------------------------------------------ layers

        [MenuItem(Menu + "Ensure Layers", false, 200)]
        public static void EnsureLayersMenu()
        {
            var report = new GenerationReport();
            bool ok = EnsureLayers(report);
            Debug.Log("[WonderJump] Layers " + (ok ? "OK: " : "FAILED: ") + string.Join(", ", WonderJumpLayers.All) +
                      (report.Warnings.Count > 0 ? "\n" + string.Join("\n", report.Warnings.ToArray()) : ""));
        }

        /// <summary>Adds missing Wonder Jump layers (preferring slots 8..13). Never renames or removes existing layers.</summary>
        public static bool EnsureLayers(GenerationReport report)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                report.Warnings.Add("ProjectSettings/TagManager.asset not found.");
                return false;
            }

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                report.Warnings.Add("TagManager has no 'layers' array.");
                return false;
            }

            bool changed = false;
            for (int n = 0; n < WonderJumpLayers.All.Length; n++)
            {
                string name = WonderJumpLayers.All[n];
                if (FindLayer(layers, name) >= 0) continue;

                int preferred = 8 + n;
                int slot = IsEmpty(layers, preferred) ? preferred : FirstEmptyUserLayer(layers);
                if (slot < 0)
                {
                    report.Warnings.Add("No free user layer for '" + name + "'.");
                    return false;
                }
                layers.GetArrayElementAtIndex(slot).stringValue = name;
                changed = true;
            }

            if (changed)
            {
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            return true;
        }

        private static int FindLayer(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
                if (string.Equals(layers.GetArrayElementAtIndex(i).stringValue, name, StringComparison.Ordinal)) return i;
            return -1;
        }

        private static bool IsEmpty(SerializedProperty layers, int index)
        {
            return index < layers.arraySize && string.IsNullOrEmpty(layers.GetArrayElementAtIndex(index).stringValue);
        }

        private static int FirstEmptyUserLayer(SerializedProperty layers)
        {
            for (int i = 8; i < layers.arraySize && i < 32; i++)
                if (IsEmpty(layers, i)) return i;
            return -1;
        }

        // ------------------------------------------------------------------ Android player settings

        [MenuItem(Menu + "Apply Android Player Settings", false, 201)]
        public static void ApplyAndroidSettingsMenu()
        {
            string notes = ApplyAndroidSettings();
            Debug.Log("[WonderJump] Android Player Settings applied.\n" + notes);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Wonder Jump", "Android Player Settings applied.\n\n" + notes, "OK");
        }

        /// <summary>Applies TechnicalBlueprint section 5. Returns notes for anything the owner must still decide.</summary>
        public static string ApplyAndroidSettings()
        {
            var notes = new System.Text.StringBuilder();

            PlayerSettings.productName = ProductName;

            // Landscape only, both directions.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinAndroidApi;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            string id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(id) || id.IndexOf("DefaultCompany", StringComparison.OrdinalIgnoreCase) >= 0)
                notes.AppendLine("- Package name is still '" + id + "'. Set it (Open Question Q1) in Player Settings > Other Settings.");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                notes.AppendLine("- Active build target is " + EditorUserBuildSettings.activeBuildTarget + ". Switch to Android in File > Build Profiles.");

            if (!MaterialLibrary.IsUrpActive())
                notes.AppendLine("- No URP asset assigned in Graphics settings (materials will render pink).");

            notes.AppendLine("- Target API = highest installed (Auto). Confirm it meets Google Play's current requirement before release.");
            return notes.ToString();
        }

        // ------------------------------------------------------------------ bootstrap scene

        [MenuItem(Menu + "Create Bootstrap Scene", false, 202)]
        public static void CreateBootstrapSceneMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) != null)
            {
                Debug.Log("[WonderJump] Bootstrap scene already exists: " + BootstrapScenePath);
                AddSceneFirstInBuild(BootstrapScenePath);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            WonderJumpAssetGenerator.EnsureFolder(AssetPaths.Parent(BootstrapScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent<BootstrapDiagnostics>();
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            AddSceneFirstInBuild(BootstrapScenePath);
            Debug.Log("[WonderJump] Created " + BootstrapScenePath + " and set it as the first build scene.");
        }

        private static void AddSceneFirstInBuild(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
