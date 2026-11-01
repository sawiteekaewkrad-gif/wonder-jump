using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WonderJump.Core;
using WonderJump.Enemies;
using WonderJump.Gameplay;
using WonderJump.Runtime;

namespace WonderJump.EditorTools.Generation
{
    public sealed class BuildContext
    {
        public Dictionary<string, Material> Materials;
        public Dictionary<ShapeKind, Mesh> Meshes;
        public GenerationReport Report;

        public Material Mat(string name)
        {
            Material m;
            if (Materials == null || !Materials.TryGetValue(name, out m) || m == null)
            {
                Report.Errors.Add("Missing material '" + name + "'");
                return null;
            }
            return m;
        }
    }

    /// <summary>
    /// Builds (unsaved) GameObjects from catalog definitions.
    ///
    /// Every prefab follows one layout:
    ///   Root        gameplay components + gameplay collider(s), unscaled, on the gameplay layer
    ///   └ VisualRoot  meshes only, NO colliders (visual can be replaced by final art freely)
    ///   └ Hitbox      (enemies/bosses) trigger used for player contact
    /// </summary>
    public static class PrefabBuilder
    {
        /// <summary>Bump whenever build logic changes, so existing generated prefabs get rebuilt.</summary>
        public const int BuildVersion = 3;

        public const string VisualRootName = "VisualRoot";
        public const string HitboxName = "Hitbox";

        /// <summary>Parts that define the character's gameplay silhouette (accessories excluded).</summary>
        public static readonly string[] CoreBodyParts = { "Body", "Head", "Leg_L", "Leg_R" };

        /// <summary>Capsule height vs core body height (slightly lower than the head top: forgiving ceilings).</summary>
        public const float CharacterHeightFactor = 0.95f;
        /// <summary>Capsule radius vs widest core half-width (narrower than the visual: forgiving edges).</summary>
        public const float CharacterRadiusFactor = 0.70f;
        /// <summary>Enemy/boss solid body vs visual.</summary>
        public const float SolidBodyFactor = 0.90f;
        /// <summary>Enemy/boss contact trigger vs visual.</summary>
        public const float HitboxFactor = 1.00f;
        /// <summary>Item pickup radius vs visual (generous, mobile-friendly).</summary>
        public const float ItemTriggerFactor = 1.15f;

        // ------------------------------------------------------------------ characters

        public static GameObject BuildCharacter(CharacterDef def, BuildContext ctx)
        {
            var root = new GameObject(def.Id.ToString());
            var visual = new GameObject(VisualRootName);
            visual.transform.SetParent(root.transform, false);
            Transform v = visual.transform;

            Material outfit = ctx.Mat(def.OutfitMaterial);
            Material hair = ctx.Mat(def.HairMaterial);

            // Body layout identical to Asset Pack v1 (positions/scales), sleeves reuse the outfit material.
            Part(ShapeKind.Capsule, "Body", v, new Vector3(0, .78f, 0), new Vector3(.58f, .72f, .42f), outfit, ctx, true);
            Part(ShapeKind.Sphere, "Head", v, new Vector3(0, 1.55f, 0), new Vector3(.92f, .88f, .78f), hair, ctx, true);
            Part(ShapeKind.Sphere, "Face", v, new Vector3(0, 1.48f, -.20f), new Vector3(.72f, .62f, .18f), ctx.Mat("M_Skin"), ctx, true);
            Part(ShapeKind.Sphere, "Eye_L", v, new Vector3(-.20f, 1.57f, -.30f), new Vector3(.105f, .15f, .07f), ctx.Mat("M_Eye"), ctx, false);
            Part(ShapeKind.Sphere, "Eye_R", v, new Vector3(.20f, 1.57f, -.30f), new Vector3(.105f, .15f, .07f), ctx.Mat("M_Eye"), ctx, false);
            Part(ShapeKind.Sphere, "EyeHighlight_L", v, new Vector3(-.225f, 1.62f, -.36f), new Vector3(.035f, .045f, .025f), ctx.Mat("M_White"), ctx, false);
            Part(ShapeKind.Sphere, "EyeHighlight_R", v, new Vector3(.175f, 1.62f, -.36f), new Vector3(.035f, .045f, .025f), ctx.Mat("M_White"), ctx, false);
            Part(ShapeKind.Sphere, "Arm_L", v, new Vector3(-.39f, .91f, 0), new Vector3(.22f, .48f, .23f), outfit, ctx, true);
            Part(ShapeKind.Sphere, "Arm_R", v, new Vector3(.39f, .91f, 0), new Vector3(.22f, .48f, .23f), outfit, ctx, true);
            Part(ShapeKind.Capsule, "Leg_L", v, new Vector3(-.17f, .27f, 0), new Vector3(.22f, .45f, .24f), ctx.Mat("M_Dark"), ctx, true);
            Part(ShapeKind.Capsule, "Leg_R", v, new Vector3(.17f, .27f, 0), new Vector3(.22f, .45f, .24f), ctx.Mat("M_Dark"), ctx, true);

            BuildAccessory(def.Accessory, v, ctx);

            // Feet on y = 0. In v1 the leg capsules reached 0.18 below the origin (feet sank into the ground).
            Bounds core = GeometryUtil.MeshBounds(root.transform, IsCoreBodyPart);
            float lift = -core.min.y;
            v.localPosition = new Vector3(0f, lift, 0f);
            core.center += new Vector3(0f, lift, 0f);

            // One gameplay collider, on the root, sized from the core body (accessories excluded).
            float height = core.size.y * CharacterHeightFactor;
            float radius = Mathf.Min(Mathf.Max(core.extents.x, core.extents.z) * CharacterRadiusFactor, height * 0.5f);
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.height = height;
            capsule.radius = radius;
            capsule.center = new Vector3(core.center.x, height * 0.5f, 0f); // bottom exactly at y = 0

            // Kinematic body: movement is done by the custom motor (decision D-02); the Rigidbody is
            // still required so trigger events (items, hazards, hitboxes) fire.
            var body = root.AddComponent<Rigidbody>();
            ConfigureKinematic(body);

            var identity = root.AddComponent<CharacterIdentity>();
            identity.characterId = def.Id.ToString();
            identity.displayName = def.DisplayName;

            SetLayerRecursively(root, LayerOrError(WonderJumpLayers.Player, ctx));
            return root;
        }

        private static bool IsCoreBodyPart(Transform t)
        {
            return Array.IndexOf(CoreBodyParts, t.name) >= 0;
        }

        private static void BuildAccessory(AccessoryKind accessory, Transform v, BuildContext ctx)
        {
            switch (accessory)
            {
                case AccessoryKind.Goggles:
                    Part(ShapeKind.Cylinder, "GogglesBand", v, new Vector3(0, 1.89f, 0), new Vector3(.48f, .07f, .48f), ctx.Mat("M_Gold"), ctx, true);
                    Part(ShapeKind.Sphere, "Goggle_L", v, new Vector3(-.20f, 1.79f, -.32f), new Vector3(.18f, .14f, .08f), ctx.Mat("M_Crystal"), ctx, false);
                    Part(ShapeKind.Sphere, "Goggle_R", v, new Vector3(.20f, 1.79f, -.32f), new Vector3(.18f, .14f, .08f), ctx.Mat("M_Crystal"), ctx, false);
                    break;
                case AccessoryKind.WitchHat:
                    Part(ShapeKind.Cylinder, "HatBrim", v, new Vector3(0, 2.02f, 0), new Vector3(.68f, .08f, .58f), ctx.Mat("M_Purple"), ctx, true);
                    Part(ShapeKind.Cone, "HatCone", v, new Vector3(0, 2.30f, 0), new Vector3(.42f, .60f, .42f), ctx.Mat("M_Purple"), ctx, true);
                    Part(ShapeKind.Sphere, "HatStar", v, new Vector3(.25f, 2.12f, -.27f), new Vector3(.12f, .12f, .08f), ctx.Mat("M_Gold"), ctx, false);
                    break;
                case AccessoryKind.BunnyEars:
                    Part(ShapeKind.Capsule, "Ear_L", v, new Vector3(-.25f, 2.08f, 0), new Vector3(.18f, .48f, .15f), ctx.Mat("M_Pink"), ctx, true);
                    Part(ShapeKind.Capsule, "Ear_R", v, new Vector3(.25f, 2.08f, 0), new Vector3(.18f, .48f, .15f), ctx.Mat("M_Pink"), ctx, true);
                    break;
                case AccessoryKind.FoxEars:
                    Part(ShapeKind.Cone, "Ear_L", v, new Vector3(-.30f, 1.98f, 0), new Vector3(.28f, .40f, .22f), ctx.Mat("M_Orange"), ctx, true);
                    Part(ShapeKind.Cone, "Ear_R", v, new Vector3(.30f, 1.98f, 0), new Vector3(.28f, .40f, .22f), ctx.Mat("M_Orange"), ctx, true);
                    break;
                case AccessoryKind.Headphones:
                    Part(ShapeKind.Torus, "Headphones", v, new Vector3(0, 1.84f, 0), new Vector3(.62f, .62f, .18f), ctx.Mat("M_Blue"), ctx, true);
                    Part(ShapeKind.Cylinder, "EarCup_L", v, new Vector3(-.47f, 1.58f, 0), new Vector3(.16f, .12f, .16f), ctx.Mat("M_Crystal"), ctx, true);
                    Part(ShapeKind.Cylinder, "EarCup_R", v, new Vector3(.47f, 1.58f, 0), new Vector3(.16f, .12f, .16f), ctx.Mat("M_Crystal"), ctx, true);
                    break;
                case AccessoryKind.FlowerCrown:
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 2f / 5f;
                        Part(ShapeKind.Sphere, "Flower_" + i, v, new Vector3(Mathf.Cos(a) * .37f, 1.94f, Mathf.Sin(a) * .30f),
                            Vector3.one * .16f, ctx.Mat("M_Pink"), ctx, false);
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ everything else

        public static GameObject BuildSimple(SimpleAssetDef def, BuildContext ctx)
        {
            var root = new GameObject(def.Id);
            var visual = new GameObject(VisualRootName);
            visual.transform.SetParent(root.transform, false);
            bool shadows = def.Category != AssetCategory.Item;
            GameObject mesh = Part(def.Shape, "Mesh", visual.transform, Vector3.zero, def.Size, ctx.Mat(def.Material), ctx, shadows);
            mesh.transform.localRotation = Quaternion.Euler(def.VisualRotation);

            Bounds b = GeometryUtil.MeshBounds(root.transform, null);
            float offset;
            switch (def.Pivot)
            {
                case PivotMode.Feet: offset = -b.min.y; break;
                case PivotMode.Top: offset = -b.max.y; break;
                default: offset = -b.center.y; break;
            }
            visual.transform.localPosition = new Vector3(0f, offset, 0f);
            b.center += new Vector3(0f, offset, 0f);

            int layer = def.Layer != null ? LayerOrError(def.Layer, ctx) : 0;

            switch (def.Category)
            {
                case AssetCategory.Item:
                {
                    var sphere = root.AddComponent<SphereCollider>();
                    sphere.isTrigger = true;
                    sphere.center = b.center;
                    sphere.radius = Mathf.Max(b.extents.x, b.extents.y) * ItemTriggerFactor;
                    root.AddComponent<Collectible>().Configure(def.ItemType, def.ItemValue);
                    break;
                }
                case AssetCategory.Enemy:
                case AssetCategory.Boss:
                {
                    FitCollider(root, def.Shape, b, SolidBodyFactor, false);
                    var hitbox = new GameObject(HitboxName);
                    hitbox.transform.SetParent(root.transform, false);
                    FitCollider(hitbox, def.Shape, b, HitboxFactor, true);
                    root.AddComponent<EnemyIdentity>().Configure(def.Id, def.World, def.Category == AssetCategory.Boss);
                    break;
                }
                default:
                {
                    if (def.Collider != ColliderRole.None)
                    {
                        var box = root.AddComponent<BoxCollider>();
                        box.center = b.center;
                        box.size = b.size;
                        box.isTrigger = def.Collider == ColliderRole.Trigger;
                    }
                    break;
                }
            }

            if (def.Kinematic) ConfigureKinematic(root.AddComponent<Rigidbody>());

            SetLayerRecursively(root, layer);
            return root;
        }

        /// <summary>Box for angular shapes, capsule (along the longer of X/Y) for rounded shapes.</summary>
        public static Collider FitCollider(GameObject host, ShapeKind shape, Bounds b, float factor, bool trigger)
        {
            Collider result;
            if (shape == ShapeKind.Cube || shape == ShapeKind.Octahedron || shape == ShapeKind.Cone || shape == ShapeKind.Torus)
            {
                var box = host.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size * factor;
                result = box;
            }
            else
            {
                var capsule = host.AddComponent<CapsuleCollider>();
                capsule.center = b.center;
                if (b.size.x > b.size.y)
                {
                    capsule.direction = 0;
                    capsule.radius = Mathf.Min(b.extents.y, b.extents.z) * factor;
                    capsule.height = b.size.x * factor;
                }
                else
                {
                    capsule.direction = 1;
                    capsule.radius = Mathf.Min(b.extents.x, b.extents.z) * factor;
                    capsule.height = b.size.y * factor;
                }
                result = capsule;
            }
            result.isTrigger = trigger;
            return result;
        }

        // ------------------------------------------------------------------ helpers

        public static GameObject Part(ShapeKind shape, string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, BuildContext ctx, bool castShadows)
        {
            GameObject go;
            if (ProceduralMeshes.IsProcedural(shape))
            {
                go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                Mesh mesh;
                if (ctx.Meshes == null || !ctx.Meshes.TryGetValue(shape, out mesh) || mesh == null)
                {
                    ctx.Report.Errors.Add("Missing procedural mesh for " + shape + " (part '" + name + "')");
                    mesh = null;
                }
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
            }
            else
            {
                go = GameObject.CreatePrimitive(ToPrimitive(shape));
                go.name = name;
                // CreatePrimitive adds a collider; visuals must never collide (v1 defect D2).
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;

            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            return go;
        }

        public static PrimitiveType ToPrimitive(ShapeKind shape)
        {
            switch (shape)
            {
                case ShapeKind.Sphere: return PrimitiveType.Sphere;
                case ShapeKind.Capsule: return PrimitiveType.Capsule;
                case ShapeKind.Cylinder: return PrimitiveType.Cylinder;
                case ShapeKind.Cube: return PrimitiveType.Cube;
                default: throw new ArgumentOutOfRangeException("shape", shape, "Not a built-in primitive");
            }
        }

        public static void ConfigureKinematic(Rigidbody body)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }

        private static int LayerOrError(string layerName, BuildContext ctx)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                ctx.Report.Errors.Add("Layer '" + layerName + "' does not exist. Run Wonder Jump/Setup/Ensure Layers.");
                return 0;
            }
            return layer;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }
    }
}
