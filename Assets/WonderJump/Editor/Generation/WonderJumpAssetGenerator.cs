using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using WonderJump.EditorTools.Generation;

namespace WonderJump.EditorTools
{
    public sealed class GeneratorOptions
    {
        public const string DefaultGeneratedRoot = "Assets/WonderJumpGenerated";
        public const string DefaultContentRoot = "Assets/WonderJumpContent";

        /// <summary>Generator-owned output. Treat as read-only.</summary>
        public string GeneratedRoot = DefaultGeneratedRoot;
        /// <summary>Hand-authored assets. The generator only ever ADDS missing variants here.</summary>
        public string ContentRoot = DefaultContentRoot;
        /// <summary>Rebuild every generated asset, including hand-modified ones. Never touches ContentRoot.</summary>
        public bool Force;
        public bool CreateContentVariants = true;
        /// <summary>Show dialogs (menu use). Tests and batch mode set false.</summary>
        public bool Interactive = true;

        public string PrefabsRoot { get { return GeneratedRoot + "/Prefabs"; } }
        public string MaterialsRoot { get { return GeneratedRoot + "/Materials"; } }
        public string MeshesRoot { get { return GeneratedRoot + "/Meshes"; } }
        public string ManifestPath { get { return GeneratedRoot + "/" + GeneratorManifest.FileName; } }
        public string ContentPrefabsRoot { get { return ContentRoot + "/Prefabs"; } }
    }

    internal enum AssetAction { Create, Regenerate, UpToDate, SkipModified, SkipUntracked }

    /// <summary>
    /// Wonder Jump procedural placeholder generator, v2 (Phase 0 Foundation Repair).
    /// Replaces Asset Pack v1's generator; the menu path is unchanged.
    /// </summary>
    public static class WonderJumpAssetGenerator
    {
        public const int GeneratorVersion = 2;
        private const string Menu = "Wonder Jump/";

        [MenuItem(Menu + "Generate Complete 3D Asset Pack", false, 0)]
        public static void GenerateMenu()
        {
            GenerationReport report = Generate(new GeneratorOptions());
            Present(report, true);
        }

        [MenuItem(Menu + "Create Missing Content Variants", false, 1)]
        public static void CreateVariantsMenu()
        {
            var options = new GeneratorOptions();
            var report = new GenerationReport();
            CreateMissingVariants(options, report);
            AssetDatabase.SaveAssets();
            Present(report, true);
        }

        [MenuItem(Menu + "Advanced/Force Regenerate All Generated Assets", false, 100)]
        public static void ForceRegenerateMenu()
        {
            bool ok = EditorUtility.DisplayDialog("Wonder Jump - Force Regenerate",
                "This rebuilds EVERY asset under " + GeneratorOptions.DefaultGeneratedRoot +
                ", including generated prefabs you edited by hand (those edits will be lost).\n\n" +
                "Assets under " + GeneratorOptions.DefaultContentRoot + " are never touched.\n\nContinue?",
                "Force Regenerate", "Cancel");
            if (!ok) return;
            GenerationReport report = Generate(new GeneratorOptions { Force = true });
            Present(report, true);
        }

        // ------------------------------------------------------------------ API

        public static GenerationReport Generate(GeneratorOptions options)
        {
            var report = new GenerationReport();

            if (!Setup.WonderJumpProjectSetup.EnsureLayers(report))
            {
                report.Errors.Add("Required physics layers could not be created (see warnings). Generation aborted.");
                report.Aborted = true;
                return report;
            }

            Shader shader = MaterialLibrary.ResolveShader();
            if (shader == null)
            {
                report.Errors.Add("No URP shader found (" + string.Join(", ", MaterialLibrary.ShaderChain) +
                                  "). Install the Universal RP package and assign a URP asset, then run again. " +
                                  "Nothing was generated, so no pink materials were written.");
                report.Aborted = true;
                return report;
            }
            if (!MaterialLibrary.IsUrpActive())
            {
                report.Warnings.Add("No URP asset is assigned in Project Settings > Graphics/Quality. " +
                                    "Generated materials are correct but will render pink until a URP asset is assigned.");
            }
            if (AssetDatabase.IsValidFolder(options.PrefabsRoot + "/Worlds"))
            {
                report.Warnings.Add("Found the Asset Pack v1 folder '" + options.PrefabsRoot +
                                    "/Worlds'. v2 does not use it; review and delete it manually.");
            }

            EnsureFolder(options.GeneratedRoot);
            EnsureFolder(options.MaterialsRoot);
            EnsureFolder(options.MeshesRoot);
            EnsureFolder(options.PrefabsRoot);

            GeneratorManifest manifest = GeneratorManifest.Load(options.ManifestPath);
            var written = new List<KeyValuePair<string, string>>(); // path -> definition

            try
            {
                Dictionary<string, Material> materials = MaterialLibrary.EnsureAll(options.MaterialsRoot, shader, report);
                AssetDatabase.SaveAssets();

                Dictionary<ShapeKind, Mesh> meshes = EnsureMeshes(options, manifest, report, written);
                var ctx = new BuildContext { Materials = materials, Meshes = meshes, Report = report };

                for (int i = 0; i < AssetCatalog.Characters.Length; i++)
                {
                    CharacterDef def = AssetCatalog.Characters[i];
                    string path = options.PrefabsRoot + "/" + AssetCatalog.RelativePrefabPath(def);
                    string definition = def.Fingerprint() + "|b" + PrefabBuilder.BuildVersion;
                    ProcessPrefab(path, definition, () => PrefabBuilder.BuildCharacter(def, ctx), options, manifest, report, written);
                }

                for (int i = 0; i < AssetCatalog.SimpleAssets.Length; i++)
                {
                    SimpleAssetDef def = AssetCatalog.SimpleAssets[i];
                    string path = options.PrefabsRoot + "/" + AssetCatalog.RelativePrefabPath(def);
                    string definition = def.Fingerprint() + "|b" + PrefabBuilder.BuildVersion;
                    ProcessPrefab(path, definition, () => PrefabBuilder.BuildSimple(def, ctx), options, manifest, report, written);
                }
            }
            catch (Exception ex)
            {
                report.Errors.Add("Unexpected exception: " + ex);
                report.Aborted = true;
            }
            finally
            {
                // Hash AFTER SaveAssets so the recorded hash matches what Unity actually wrote to disk.
                AssetDatabase.SaveAssets();
                for (int i = 0; i < written.Count; i++)
                {
                    string hash = GeneratorManifest.HashFile(written[i].Key);
                    if (hash != null) manifest.Record(written[i].Key, hash, written[i].Value);
                }
                manifest.generatorVersion = GeneratorVersion;
                manifest.Save(options.ManifestPath);
                AssetDatabase.ImportAsset(options.ManifestPath);
            }

            if (options.CreateContentVariants && report.Succeeded) CreateMissingVariants(options, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return report;
        }

        /// <summary>Creates a Prefab Variant in ContentRoot for every generated prefab that has none. Never modifies existing files.</summary>
        public static void CreateMissingVariants(GeneratorOptions options, GenerationReport report)
        {
            foreach (string relative in AllRelativePrefabPaths())
            {
                string basePath = options.PrefabsRoot + "/" + relative;
                string variantPath = options.ContentPrefabsRoot + "/" + relative;
                if (File.Exists(AssetPaths.ToFullPath(variantPath))) continue;

                var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
                if (basePrefab == null)
                {
                    report.Warnings.Add("No generated prefab for variant " + variantPath);
                    continue;
                }

                EnsureFolder(AssetPaths.Parent(variantPath));
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                try
                {
                    bool ok;
                    PrefabUtility.SaveAsPrefabAsset(instance, variantPath, out ok);
                    if (ok) report.VariantsCreated.Add(variantPath);
                    else report.Errors.Add("Failed to create variant " + variantPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        public static IEnumerable<string> AllRelativePrefabPaths()
        {
            for (int i = 0; i < AssetCatalog.Characters.Length; i++)
                yield return AssetCatalog.RelativePrefabPath(AssetCatalog.Characters[i]);
            for (int i = 0; i < AssetCatalog.SimpleAssets.Length; i++)
                yield return AssetCatalog.RelativePrefabPath(AssetCatalog.SimpleAssets[i]);
        }

        // ------------------------------------------------------------------ internals

        internal static AssetAction Classify(string path, string definition, GeneratorManifest manifest, bool force)
        {
            if (!File.Exists(AssetPaths.ToFullPath(path))) return AssetAction.Create;
            if (force) return AssetAction.Regenerate;
            GeneratorManifest.Entry entry = manifest.Find(path);
            if (entry == null) return AssetAction.SkipUntracked;
            string hash = GeneratorManifest.HashFile(path);
            if (!string.Equals(hash, entry.fileHash, StringComparison.Ordinal)) return AssetAction.SkipModified;
            if (!string.Equals(definition, entry.definition, StringComparison.Ordinal)) return AssetAction.Regenerate;
            return AssetAction.UpToDate;
        }

        private static void ProcessPrefab(string path, string definition, Func<GameObject> build, GeneratorOptions options,
            GeneratorManifest manifest, GenerationReport report, List<KeyValuePair<string, string>> written)
        {
            AssetAction action = Classify(path, definition, manifest, options.Force);
            if (!Track(action, path, report)) return;

            EnsureFolder(AssetPaths.Parent(path));
            GameObject go = build();
            try
            {
                bool ok;
                // Saving onto an existing path keeps the asset GUID, so scene references and variants stay linked.
                PrefabUtility.SaveAsPrefabAsset(go, path, out ok);
                if (!ok)
                {
                    report.Errors.Add("Failed to save prefab " + path);
                    return;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
            (action == AssetAction.Create ? report.Created : report.Regenerated).Add(path);
            written.Add(new KeyValuePair<string, string>(path, definition));
        }

        /// <summary>Returns true when the asset must be (re)written.</summary>
        private static bool Track(AssetAction action, string path, GenerationReport report)
        {
            switch (action)
            {
                case AssetAction.UpToDate: report.UpToDate.Add(path); return false;
                case AssetAction.SkipModified: report.SkippedModified.Add(path); return false;
                case AssetAction.SkipUntracked: report.SkippedUntracked.Add(path); return false;
                default: return true;
            }
        }

        private static Dictionary<ShapeKind, Mesh> EnsureMeshes(GeneratorOptions options, GeneratorManifest manifest,
            GenerationReport report, List<KeyValuePair<string, string>> written)
        {
            var result = new Dictionary<ShapeKind, Mesh>();
            ShapeKind[] shapes = { ShapeKind.Cone, ShapeKind.Torus, ShapeKind.Octahedron };
            for (int i = 0; i < shapes.Length; i++)
            {
                ShapeKind shape = shapes[i];
                string name = ProceduralMeshes.AssetName(shape);
                string path = options.MeshesRoot + "/" + name + ".asset";
                string definition = "mesh|" + name + "|v" + ProceduralMeshes.Version;
                AssetAction action = Classify(path, definition, manifest, options.Force);
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                if (Track(action, path, report))
                {
                    MeshData data = ProceduralMeshes.Build(shape);
                    if (mesh == null)
                    {
                        mesh = new Mesh();
                        ProceduralMeshes.WriteTo(mesh, data, name);
                        AssetDatabase.CreateAsset(mesh, path);
                        report.Created.Add(path);
                    }
                    else
                    {
                        ProceduralMeshes.WriteTo(mesh, data, name); // in place: GUID kept
                        EditorUtility.SetDirty(mesh);
                        report.Regenerated.Add(path);
                    }
                    AssetDatabase.SaveAssetIfDirty(mesh);
                    written.Add(new KeyValuePair<string, string>(path, definition));
                }

                if (mesh == null) report.Errors.Add("Procedural mesh missing and could not be created: " + path);
                result[shape] = mesh;
            }
            return result;
        }

        public static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = AssetPaths.Parent(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
        }

        private static void Present(GenerationReport report, bool interactive)
        {
            string summary = report.Summary();
            if (!report.Succeeded) Debug.LogError(summary);
            else if (report.SkippedModified.Count > 0 || report.SkippedUntracked.Count > 0 || report.Warnings.Count > 0) Debug.LogWarning(summary);
            else Debug.Log(summary);

            if (interactive && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Wonder Jump",
                    (report.Succeeded ? "Generation complete." : "Generation FAILED - see Console.") +
                    "\nCreated " + report.Created.Count + ", regenerated " + report.Regenerated.Count +
                    ", up to date " + report.UpToDate.Count + ", skipped " + (report.SkippedModified.Count + report.SkippedUntracked.Count) +
                    ", variants " + report.VariantsCreated.Count + ".\n\nRun 'Wonder Jump/Validate Generated Assets' next.", "OK");
            }
        }
    }
}
