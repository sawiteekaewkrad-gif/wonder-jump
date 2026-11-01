using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WonderJump.Core;
using WonderJump.EditorTools.Generation;
using WonderJump.Worlds;

namespace WonderJump.EditorTools.Build
{
    /// <summary>
    /// Builds the Phase 0 asset showcase scene: all 6 characters, every collectible and every enemy on
    /// generated platforms, with the diagnostics overlay. Purpose: eyeball the generated assets on a real
    /// Android device (no pink materials, correct proportions, landscape framing). Not gameplay.
    /// The scene is generated, so it lives in the generated folder and is rebuilt on every call.
    /// </summary>
    public static class ShowcaseSceneBuilder
    {
        public const string ScenePath = GeneratorOptions.DefaultGeneratedRoot + "/Scenes/Phase0_Showcase.unity";

        [MenuItem("Wonder Jump/Build/Rebuild Phase 0 Showcase Scene", false, 300)]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(new GeneratorOptions());
            Debug.Log("[WonderJump] Showcase scene written: " + ScenePath);
        }

        public static string Build(GeneratorOptions options)
        {
            WonderJumpAssetGenerator.EnsureFolder(AssetPaths.Parent(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera at -Z looking +Z (characters face -Z).
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 1f);
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            cameraGo.transform.position = new Vector3(0f, 2.6f, -16f);
            cameraGo.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

            var lightGo = new GameObject("Sun");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.75f);

            new GameObject("Bootstrap").AddComponent<BootstrapDiagnostics>();

            // Ground: a row of Forest_Ground platforms (top surface at y = 0), 4 m each.
            for (int i = 0; i < 7; i++)
                Place(options, "Platforms/GreenForest/Forest_Ground.prefab", new Vector3(-12f + i * 4f, 0f, 0f));

            // Characters, front row.
            for (int i = 0; i < AssetCatalog.Characters.Length; i++)
                Place(options, AssetCatalog.RelativePrefabPath(AssetCatalog.Characters[i]), new Vector3(-6.25f + i * 2.5f, 0f, 0f));

            // Collectibles floating above the characters.
            float x = -6.25f;
            foreach (SimpleAssetDef def in AssetCatalog.SimpleAssets)
            {
                if (def.Category != AssetCategory.Item) continue;
                GameObject item = Place(options, AssetCatalog.RelativePrefabPath(def), new Vector3(x, 3.4f, 0f));
                if (item != null) item.AddComponent<ShowcaseMotion>();
                x += 2.5f;
            }

            // One row of enemies behind (z = +3), grouped by world order.
            float ex = -10.5f;
            foreach (WorldId world in WorldInfo.PlayableWorlds)
            {
                foreach (SimpleAssetDef def in AssetCatalog.SimpleAssets)
                {
                    if (def.Category != AssetCategory.Enemy || def.World != world) continue;
                    Place(options, AssetCatalog.RelativePrefabPath(def), new Vector3(ex, 0f, 3f));
                    ex += 3.5f;
                }
            }
            // Backing ground for the enemy row.
            for (int i = 0; i < 7; i++)
                Place(options, "Platforms/GreenForest/Forest_Ground.prefab", new Vector3(-12f + i * 4f, 0f, 3f));

            // Portal on the right as a landmark.
            Place(options, "Props/Shared/PortalFrame.prefab", new Vector3(10.5f, 0f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            return ScenePath;
        }

        /// <summary>Instantiates the content variant if present (normal workflow), else the generated prefab.</summary>
        private static GameObject Place(GeneratorOptions options, string relative, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(options.ContentPrefabsRoot + "/" + relative)
                         ?? AssetDatabase.LoadAssetAtPath<GameObject>(options.PrefabsRoot + "/" + relative);
            if (prefab == null)
            {
                Debug.LogWarning("[WonderJump] Showcase: missing prefab " + relative);
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = position;
            return go;
        }
    }
}
