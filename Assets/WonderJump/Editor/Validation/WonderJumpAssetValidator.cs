using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using WonderJump.Characters;
using WonderJump.Core;
using WonderJump.EditorTools.Generation;
using WonderJump.Enemies;
using WonderJump.Gameplay;
using WonderJump.Runtime;

namespace WonderJump.EditorTools.Validation
{
    public sealed class ValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public int ChecksRun;

        public bool Passed { get { return Errors.Count == 0; } }

        public void Check(bool condition, string failureMessage)
        {
            ChecksRun++;
            if (!condition) Errors.Add(failureMessage);
        }

        public void Warn(bool condition, string message)
        {
            ChecksRun++;
            if (!condition) Warnings.Add(message);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Wonder Jump validation: " + (Passed ? "PASS" : "FAIL") + " (" + ChecksRun + " checks, " +
                          Errors.Count + " errors, " + Warnings.Count + " warnings)");
            for (int i = 0; i < Errors.Count; i++) sb.Append("  ERROR   ").AppendLine(Errors[i]);
            for (int i = 0; i < Warnings.Count; i++) sb.Append("  WARNING ").AppendLine(Warnings[i]);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Static checks on generator output (no Play Mode needed). Trigger behaviour at runtime is covered
    /// by the PlayMode test CollectiblePickupTests.
    /// </summary>
    public static class WonderJumpAssetValidator
    {
        private const float Epsilon = 0.011f;

        [MenuItem("Wonder Jump/Validate Generated Assets", false, 20)]
        public static void ValidateMenu()
        {
            ValidationReport report = Validate(new GeneratorOptions());
            if (report.Passed) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Wonder Jump", report.Passed ? "Validation PASSED." : "Validation FAILED - see Console.", "OK");
        }

        public static ValidationReport Validate(GeneratorOptions options)
        {
            var r = new ValidationReport();

            // V01 pipeline + layers
            r.Warn(MaterialLibrary.IsUrpActive(), "V01 No URP asset assigned in Graphics/Quality settings: materials will render pink.");
            for (int i = 0; i < WonderJumpLayers.All.Length; i++)
                r.Check(WonderJumpLayers.Index(WonderJumpLayers.All[i]) >= 0, "V02 Missing layer '" + WonderJumpLayers.All[i] + "'.");

            // V03/V04 materials
            List<string> expectedMaterials = AssetCatalog.AllMaterialNames();
            var expectedSet = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < expectedMaterials.Count; i++)
            {
                string path = options.MaterialsRoot + "/" + expectedMaterials[i] + ".mat";
                expectedSet.Add(path);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                r.Check(m != null, "V03 Missing material " + path);
                if (m != null) r.Check(MaterialLibrary.HasUsableShader(m), "V03 Non-URP shader '" + (m.shader != null ? m.shader.name : "null") + "' on " + path);
            }
            foreach (string path in FindAssets("t:Material", options.MaterialsRoot))
                r.Warn(expectedSet.Contains(path), "V04 Unexpected/duplicate material " + path);

            // V05 procedural meshes
            ShapeKind[] shapes = { ShapeKind.Cone, ShapeKind.Torus, ShapeKind.Octahedron };
            for (int i = 0; i < shapes.Length; i++)
            {
                string path = options.MeshesRoot + "/" + ProceduralMeshes.AssetName(shapes[i]) + ".asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                r.Check(mesh != null && mesh.vertexCount > 0, "V05 Missing or empty procedural mesh " + path);
            }

            // V06..V11 prefabs
            var expectedPrefabs = new HashSet<string>(StringComparer.Ordinal);
            var seenCharacterIds = new HashSet<CharacterId>();
            for (int i = 0; i < AssetCatalog.Characters.Length; i++)
            {
                CharacterDef def = AssetCatalog.Characters[i];
                string path = options.PrefabsRoot + "/" + AssetCatalog.RelativePrefabPath(def);
                expectedPrefabs.Add(path);
                WithPrefab(path, r, root => ValidateCharacter(def, root, path, r, seenCharacterIds));
            }
            r.Check(seenCharacterIds.Count == CharacterIds.All.Length, "V07 Expected 6 unique character IDs, found " + seenCharacterIds.Count);

            for (int i = 0; i < AssetCatalog.SimpleAssets.Length; i++)
            {
                SimpleAssetDef def = AssetCatalog.SimpleAssets[i];
                string path = options.PrefabsRoot + "/" + AssetCatalog.RelativePrefabPath(def);
                expectedPrefabs.Add(path);
                WithPrefab(path, r, root => ValidateSimple(def, root, path, r));
            }

            // V12 stray prefabs (duplicates / leftovers)
            foreach (string path in FindAssets("t:Prefab", options.PrefabsRoot))
                r.Warn(expectedPrefabs.Contains(path), "V12 Prefab not in catalog (duplicate or leftover): " + path);

            // V13 manifest
            string manifestFull = AssetPaths.ToFullPath(options.ManifestPath);
            r.Check(File.Exists(manifestFull), "V13 Generator manifest missing: " + options.ManifestPath);
            if (File.Exists(manifestFull))
            {
                GeneratorManifest manifest = GeneratorManifest.Load(options.ManifestPath);
                foreach (string path in expectedPrefabs)
                {
                    GeneratorManifest.Entry e = manifest.Find(path);
                    r.Warn(e != null, "V13 Not tracked by manifest (will be skipped on regeneration): " + path);
                    if (e != null)
                        r.Warn(string.Equals(e.fileHash, GeneratorManifest.HashFile(path), StringComparison.Ordinal),
                            "V13 Modified by hand since generation (regeneration will skip it): " + path);
                }
            }

            // V14 content variants
            foreach (string relative in WonderJumpAssetGenerator.AllRelativePrefabPaths())
            {
                string variantPath = options.ContentPrefabsRoot + "/" + relative;
                var variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                if (variant == null) continue; // optional
                var source = PrefabUtility.GetCorrespondingObjectFromSource(variant);
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : null;
                r.Warn(sourcePath == options.PrefabsRoot + "/" + relative,
                    "V14 " + variantPath + " is not a variant of its generated prefab (source: " + (sourcePath ?? "none") + ")");
            }

            return r;
        }

        // ------------------------------------------------------------------ per category

        private static void ValidateCharacter(CharacterDef def, GameObject root, string path, ValidationReport r, HashSet<CharacterId> seen)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            r.Check(colliders.Length == 1, "V07 " + path + ": expected exactly 1 collider, found " + colliders.Length);
            var capsule = root.GetComponent<CapsuleCollider>();
            r.Check(capsule != null && !capsule.isTrigger, "V07 " + path + ": root needs a solid CapsuleCollider");

            Transform visual = root.transform.Find(PrefabBuilder.VisualRootName);
            r.Check(visual != null, "V07 " + path + ": missing VisualRoot");

            if (capsule != null && visual != null)
            {
                Bounds core = GeometryUtil.MeshBounds(root.transform, t => Array.IndexOf(PrefabBuilder.CoreBodyParts, t.name) >= 0);
                float bottom = capsule.center.y - capsule.height * 0.5f;
                r.Check(Mathf.Abs(core.min.y) < Epsilon, "V07 " + path + ": feet not at y=0 (min y " + core.min.y + ")");
                r.Check(Mathf.Abs(bottom) < Epsilon, "V07 " + path + ": capsule bottom not at y=0 (" + bottom + ") - unstable ground detection");
                r.Check(capsule.height <= core.size.y + Epsilon, "V07 " + path + ": capsule taller than body (" + capsule.height + " > " + core.size.y + ")");
                r.Check(capsule.radius <= Mathf.Max(core.extents.x, core.extents.z) + Epsilon, "V07 " + path + ": capsule wider than body");
                r.Check(capsule.radius > 0.1f && capsule.height > 1f, "V07 " + path + ": capsule implausibly small");
            }

            var body = root.GetComponent<Rigidbody>();
            r.Check(body != null && body.isKinematic, "V07 " + path + ": needs a kinematic Rigidbody (trigger events)");
            if (body != null)
                r.Check((body.constraints & RigidbodyConstraints.FreezePositionZ) != 0, "V07 " + path + ": Z position must be frozen (2.5D)");

            var identity = root.GetComponent<CharacterIdentity>();
            CharacterId id;
            bool valid = identity != null && identity.TryGetId(out id) && id == def.Id;
            r.Check(valid, "V07 " + path + ": CharacterIdentity missing or not '" + def.Id + "'");
            if (valid) seen.Add(def.Id);

            ValidateLayer(root, WonderJumpLayers.Player, path, r);
            ValidateRenderers(root, path, r);
        }

        private static void ValidateSimple(SimpleAssetDef def, GameObject root, string path, ValidationReport r)
        {
            Transform visual = root.transform.Find(PrefabBuilder.VisualRootName);
            r.Check(visual != null, "V08 " + path + ": missing VisualRoot");
            if (visual != null)
                r.Check(visual.GetComponentsInChildren<Collider>(true).Length == 0, "V08 " + path + ": VisualRoot must not contain colliders");

            Bounds visualBounds = GeometryUtil.MeshBounds(root.transform, null);
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

            switch (def.Category)
            {
                case AssetCategory.Item:
                {
                    r.Check(colliders.Length == 1, "V09 " + path + ": expected 1 collider, found " + colliders.Length);
                    var sphere = root.GetComponent<SphereCollider>();
                    r.Check(sphere != null && sphere.isTrigger, "V09 " + path + ": needs a trigger SphereCollider on the root");
                    var c = root.GetComponent<Collectible>();
                    r.Check(c != null && c.Type == def.ItemType, "V09 " + path + ": Collectible missing or wrong type");
                    if (sphere != null)
                        r.Check(sphere.radius <= Mathf.Max(visualBounds.extents.x, visualBounds.extents.y) * PrefabBuilder.ItemTriggerFactor + Epsilon,
                            "V09 " + path + ": pickup radius oversized");
                    break;
                }
                case AssetCategory.Enemy:
                case AssetCategory.Boss:
                {
                    Collider solid = null;
                    Collider[] rootColliders = root.GetComponents<Collider>();
                    for (int i = 0; i < rootColliders.Length; i++) if (!rootColliders[i].isTrigger) solid = rootColliders[i];
                    r.Check(rootColliders.Length == 1 && solid != null, "V10 " + path + ": root needs exactly one solid body collider");
                    Transform hitbox = root.transform.Find(PrefabBuilder.HitboxName);
                    Collider hit = hitbox != null ? hitbox.GetComponent<Collider>() : null;
                    r.Check(hit != null && hit.isTrigger, "V10 " + path + ": needs a trigger collider on child 'Hitbox'");
                    r.Check(colliders.Length == 2, "V10 " + path + ": expected 2 colliders (body + hitbox), found " + colliders.Length);
                    if (solid != null) CheckWithin(GeometryUtil.ColliderBounds(root.transform, solid), visualBounds, path + " body", r);
                    if (hit != null) CheckWithin(GeometryUtil.ColliderBounds(root.transform, hit), visualBounds, path + " hitbox", r);
                    var identity = root.GetComponent<EnemyIdentity>();
                    r.Check(identity != null && identity.World == def.World && identity.IsBoss == (def.Category == AssetCategory.Boss),
                        "V10 " + path + ": EnemyIdentity missing or wrong world/boss flag");
                    var body = root.GetComponent<Rigidbody>();
                    r.Check(body != null && body.isKinematic, "V10 " + path + ": needs a kinematic Rigidbody");
                    r.Check(Mathf.Abs(visualBounds.min.y) < Epsilon, "V10 " + path + ": feet not at y=0");
                    break;
                }
                default:
                {
                    if (def.Collider == ColliderRole.None)
                    {
                        r.Check(colliders.Length == 0, "V11 " + path + ": decorative prop must have no colliders");
                    }
                    else
                    {
                        r.Check(colliders.Length == 1, "V11 " + path + ": expected 1 collider, found " + colliders.Length);
                        if (colliders.Length == 1)
                        {
                            r.Check(colliders[0].isTrigger == (def.Collider == ColliderRole.Trigger),
                                "V11 " + path + ": collider trigger flag should be " + (def.Collider == ColliderRole.Trigger));
                            CheckWithin(GeometryUtil.ColliderBounds(root.transform, colliders[0]), visualBounds, path, r);
                        }
                    }
                    if (def.Pivot == PivotMode.Top)
                        r.Check(Mathf.Abs(visualBounds.max.y) < Epsilon, "V11 " + path + ": top surface not at y=0");
                    break;
                }
            }

            if (def.Layer != null) ValidateLayer(root, def.Layer, path, r);
            ValidateRenderers(root, path, r);
        }

        // ------------------------------------------------------------------ helpers

        private static void CheckWithin(Bounds collider, Bounds visual, string label, ValidationReport r)
        {
            Vector3 pad = Vector3.one * Epsilon;
            bool inside = collider.min.x >= visual.min.x - pad.x && collider.min.y >= visual.min.y - pad.y &&
                          collider.max.x <= visual.max.x + pad.x && collider.max.y <= visual.max.y + pad.y;
            r.Check(inside, "V08 " + label + ": collider exceeds visual bounds (oversized)");
        }

        private static void ValidateLayer(GameObject root, string layerName, string path, ValidationReport r)
        {
            int expected = WonderJumpLayers.Index(layerName);
            r.Check(expected >= 0 && root.layer == expected, "V06 " + path + ": root layer should be '" + layerName + "'");
        }

        private static void ValidateRenderers(GameObject root, string path, ValidationReport r)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
                r.Check(filters[i].sharedMesh != null, "V06 " + path + ": missing mesh on '" + filters[i].name + "'");
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            r.Check(renderers.Length > 0, "V06 " + path + ": no renderers");
            for (int i = 0; i < renderers.Length; i++)
                r.Check(MaterialLibrary.HasUsableShader(renderers[i].sharedMaterial),
                    "V06 " + path + ": '" + renderers[i].name + "' has a missing or non-URP material");
        }

        private static void WithPrefab(string path, ValidationReport r, Action<GameObject> validate)
        {
            r.Check(File.Exists(AssetPaths.ToFullPath(path)), "V06 Missing prefab " + path);
            if (!File.Exists(AssetPaths.ToFullPath(path))) return;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                validate(root);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static IEnumerable<string> FindAssets(string filter, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) yield break;
            string[] guids = AssetDatabase.FindAssets(filter, new[] { folder });
            for (int i = 0; i < guids.Length; i++) yield return AssetDatabase.GUIDToAssetPath(guids[i]);
        }
    }
}
