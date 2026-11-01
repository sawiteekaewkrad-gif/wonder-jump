using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace WonderJump.EditorTools.Generation
{
    /// <summary>
    /// URP material creation and repair.
    ///  - Only URP shaders are used. If none can be found, generation stops instead of writing
    ///    Standard-shader materials that would turn pink later (Asset Pack v1 defect D4).
    ///  - Existing materials with a valid URP shader are left untouched (manual colour tweaks survive).
    ///  - Existing materials with a missing/non-URP/error shader are repaired IN PLACE (same GUID,
    ///    so every prefab reference stays valid).
    ///  - One material per name, deterministic paths: re-running never creates duplicates.
    /// </summary>
    public static class MaterialLibrary
    {
        public const string UrpPrefix = "Universal Render Pipeline/";

        /// <summary>Tried in order. Simple Lit/Unlit are cheaper fallbacks if Lit is stripped.</summary>
        public static readonly string[] ShaderChain =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit"
        };

        public static Shader ResolveShader()
        {
            for (int i = 0; i < ShaderChain.Length; i++)
            {
                Shader s = Shader.Find(ShaderChain[i]);
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>True when a URP asset is assigned in Graphics or Quality settings.</summary>
        public static bool IsUrpActive()
        {
            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
            return rp != null && rp.GetType().FullName.IndexOf("Universal", StringComparison.Ordinal) >= 0;
        }

        public static bool HasUsableShader(Material material)
        {
            if (material == null || material.shader == null) return false;
            string n = material.shader.name;
            return n.StartsWith(UrpPrefix, StringComparison.Ordinal);
        }

        public static Dictionary<string, Material> EnsureAll(string folder, Shader shader, GenerationReport report)
        {
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            for (int i = 0; i < AssetCatalog.BaseMaterials.Length; i++)
            {
                MaterialDef d = AssetCatalog.BaseMaterials[i];
                result[d.Name] = Ensure(folder, d, shader, report);
            }
            for (int i = 0; i < AssetCatalog.Characters.Length; i++)
            {
                CharacterDef c = AssetCatalog.Characters[i];
                var outfit = new MaterialDef(c.OutfitMaterial, c.Outfit, 0f, .3f);
                var hair = new MaterialDef(c.HairMaterial, c.Hair, 0f, .35f);
                result[outfit.Name] = Ensure(folder, outfit, shader, report);
                result[hair.Name] = Ensure(folder, hair, shader, report);
            }
            return result;
        }

        public static Material Ensure(string folder, MaterialDef def, Shader shader, GenerationReport report)
        {
            string path = folder + "/" + def.Name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (HasUsableShader(existing))
                {
                    report.MaterialsPreserved.Add(path);
                    return existing;
                }
                string oldShader = existing.shader != null ? existing.shader.name : "<none>";
                existing.shader = shader;
                Apply(existing, def);
                EditorUtility.SetDirty(existing);
                report.MaterialsRepaired.Add(path + " (was " + oldShader + ")");
                return existing;
            }

            var created = new Material(shader) { name = def.Name };
            Apply(created, def);
            AssetDatabase.CreateAsset(created, path);
            report.MaterialsCreated.Add(path);
            return created;
        }

        public static void Apply(Material m, MaterialDef def)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", def.Color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", def.Color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", def.Metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", def.Smoothness);
            m.enableInstancing = false; // SRP Batcher is the batching path on URP mobile
        }
    }
}
