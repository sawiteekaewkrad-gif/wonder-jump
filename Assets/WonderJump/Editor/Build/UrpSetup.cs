using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WonderJump.EditorTools.Generation;

namespace WonderJump.EditorTools.Build
{
    /// <summary>
    /// Makes sure a mobile-tuned URP asset is assigned. Needed when the project was opened from this repo
    /// (or in CI) rather than created from the Universal 3D template. Never replaces an existing URP asset.
    /// </summary>
    public static class UrpSetup
    {
        public const string SettingsFolder = GeneratorOptions.DefaultContentRoot + "/Settings";
        public const string PipelinePath = SettingsFolder + "/WJ_Mobile_URP.asset";
        public const string RendererPath = SettingsFolder + "/WJ_Mobile_Renderer.asset";

        [MenuItem("Wonder Jump/Setup/Ensure URP Pipeline", false, 199)]
        public static void EnsureMenu()
        {
            Debug.Log("[WonderJump] " + Ensure());
        }

        /// <summary>Returns a human-readable note of what happened.</summary>
        public static string Ensure()
        {
            if (MaterialLibrary.IsUrpActive())
                return "URP already active: " + GraphicsSettings.defaultRenderPipeline;

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                WonderJumpAssetGenerator.EnsureFolder(SettingsFolder);
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);

                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                // Mobile defaults (TechnicalBlueprint section 6)
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 2;
                pipeline.renderScale = 1f;
                pipeline.shadowDistance = 30f;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                AssetDatabase.SaveAssets();
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
            return "Assigned " + PipelinePath + " as the render pipeline (default + all quality levels).";
        }
    }
}
