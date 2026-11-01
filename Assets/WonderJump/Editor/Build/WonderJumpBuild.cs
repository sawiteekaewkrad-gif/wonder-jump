using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using WonderJump.EditorTools.Generation;
using WonderJump.EditorTools.Setup;
using WonderJump.EditorTools.Validation;

namespace WonderJump.EditorTools.Build
{
    /// <summary>
    /// One pipeline for local and CI Android builds:
    /// URP → layers → generate assets → validate → showcase scene → Android settings → APK.
    /// Any failing step stops the build (no APK from a broken asset state).
    ///
    /// CI entry point (GameCI unity-builder "buildMethod"):
    ///   WonderJump.EditorTools.Build.WonderJumpBuild.BuildAndroidCI
    /// GameCI passes -customBuildPath, -androidVersionCode, -buildVersion on the command line.
    /// </summary>
    public static class WonderJumpBuild
    {
        /// <summary>Temporary package name until Q1 is answered. Override with env var WJ_ANDROID_PACKAGE.</summary>
        public const string PlaceholderPackage = "com.wonderjump.adventureworlds.dev";
        public const string DefaultOutput = "Builds/Android/WonderJump.apk";

        [MenuItem("Wonder Jump/Build/Build Android APK", false, 301)]
        public static void BuildMenu()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string error;
            string path = Run(DefaultOutput, null, null, out error);
            if (error != null)
            {
                Debug.LogError("[WonderJump] Android build FAILED: " + error);
                EditorUtility.DisplayDialog("Wonder Jump", "Android build FAILED:\n" + error, "OK");
            }
            else
            {
                Debug.Log("[WonderJump] APK: " + Path.GetFullPath(path));
                EditorUtility.RevealInFinder(path);
            }
        }

        /// <summary>Batch-mode entry point. Exits the editor with 0 on success, 1 on failure.</summary>
        public static void BuildAndroidCI()
        {
            string output = Arg("-customBuildPath") ?? DefaultOutput;
            if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                output = Path.Combine(output, "WonderJump.apk");
            string error;
            Run(output, Arg("-buildVersion"), Arg("-androidVersionCode"), out error);
            if (error != null)
            {
                Debug.LogError("[WonderJump] CI build FAILED: " + error);
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        public static string Run(string outputPath, string version, string versionCode, out string error)
        {
            error = null;
            try
            {
                Debug.Log("[WonderJump] 1/7 " + UrpSetup.Ensure());

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    Debug.Log("[WonderJump] 2/7 Switching build target to Android");
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                }

                var options = new GeneratorOptions { Interactive = false };
                GenerationReport gen = WonderJumpAssetGenerator.Generate(options);
                Debug.Log("[WonderJump] 3/7 " + gen.Summary());
                if (!gen.Succeeded) { error = "Asset generation failed (see log)."; return null; }

                ValidationReport validation = WonderJumpAssetValidator.Validate(options);
                Debug.Log("[WonderJump] 4/7 " + validation);
                if (!validation.Passed) { error = "Asset validation failed (" + validation.Errors.Count + " errors, see log)."; return null; }

                string scene = ShowcaseSceneBuilder.Build(options);
                Debug.Log("[WonderJump] 5/7 Showcase scene: " + scene);

                Debug.Log("[WonderJump] 6/7 " + WonderJumpProjectSetup.ApplyAndroidSettings());
                EnsurePackageName();
                if (!string.IsNullOrEmpty(version) && version != "none") PlayerSettings.bundleVersion = version;
                int code;
                if (int.TryParse(versionCode, out code) && code > 0) PlayerSettings.Android.bundleVersionCode = code;
                EditorUserBuildSettings.buildAppBundle = false; // APK for device testing
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true) };

                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var buildOptions = new BuildPlayerOptions
                {
                    scenes = new[] { scene },
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };
                BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
                BuildSummary summary = report.summary;
                Debug.Log("[WonderJump] 7/7 Build " + summary.result + ": " + summary.totalSize / (1024 * 1024) + " MB, " +
                          summary.totalErrors + " errors, " + summary.totalTime);
                if (summary.result != BuildResult.Succeeded) { error = "BuildPlayer result: " + summary.result; return null; }
                return outputPath;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return null;
            }
        }

        private static void EnsurePackageName()
        {
            string fromEnv = Environment.GetEnvironmentVariable("WJ_ANDROID_PACKAGE");
            string current = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            string target = !string.IsNullOrEmpty(fromEnv) ? fromEnv
                : (IsValidPackage(current) && current.IndexOf("DefaultCompany", StringComparison.OrdinalIgnoreCase) < 0 ? current : PlaceholderPackage);
            if (target != current) PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, target);
            Debug.Log("[WonderJump] Android package: " + target + (target == PlaceholderPackage ? " (PLACEHOLDER until Q1 is answered)" : ""));
        }

        /// <summary>Java package rules: 2+ segments, each starting with a letter, only letters/digits/underscore.</summary>
        public static bool IsValidPackage(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            string[] parts = id.Split('.');
            if (parts.Length < 2) return false;
            foreach (string p in parts)
            {
                if (p.Length == 0 || !char.IsLetter(p[0])) return false;
                foreach (char c in p) if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            }
            return true;
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.Ordinal)) return args[i + 1];
            return null;
        }
    }
}
