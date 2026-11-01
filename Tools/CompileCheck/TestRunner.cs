// Reflection runner for the [Test] methods that do not need the Unity Editor.
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public static class StubTestRunner
{
    // Fixtures that only touch pure logic. GeneratorIntegrationTests (AssetDatabase) and the PlayMode
    // physics tests can only run inside Unity and are compiled but NOT executed here.
    private static readonly string[] Fixtures =
    {
        "WonderJump.Tests.EditMode.ProceduralMeshesTests",
        "WonderJump.Tests.EditMode.AssetCatalogTests",
        "WonderJump.Tests.EditMode.RuntimeContractTests"
    };

    public static int Main()
    {
        // Pretend the layers exist, as they would after "Ensure Layers".
        string[] layers = { "Player", "Enemy", "Ground", "Collectible", "Hazard", "Interactable" };
        for (int i = 0; i < layers.Length; i++) UnityEngine.LayerMask.Layers[layers[i]] = 8 + i;

        int passed = 0, failed = 0, ignored = 0;
        Assembly asm = typeof(StubTestRunner).Assembly;
        foreach (string fixtureName in Fixtures)
        {
            Type fixture = asm.GetType(fixtureName, true);
            foreach (MethodInfo test in fixture.GetMethods().Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0))
            {
                object instance = Activator.CreateInstance(fixture);
                try
                {
                    Invoke(fixture, instance, typeof(SetUpAttribute));
                    test.Invoke(instance, null);
                    Invoke(fixture, instance, typeof(TearDownAttribute));
                    passed++;
                    Console.WriteLine("PASS    " + fixture.Name + "." + test.Name);
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException is IgnoreException) { ignored++; Console.WriteLine("IGNORED " + fixture.Name + "." + test.Name); }
                    else { failed++; Console.WriteLine("FAIL    " + fixture.Name + "." + test.Name + "\n        " + ex.InnerException.Message); }
                }
            }
        }

        int builderErrors = BuilderSmoke();

        Console.WriteLine("\n" + passed + " passed, " + failed + " failed, " + ignored + " ignored (stub environment, NOT Unity)");
        return failed == 0 && builderErrors == 0 ? 0 : 1;
    }

    /// <summary>
    /// Builds all 38 prefabs with the real PrefabBuilder (unsaved, stub GameObjects) and runs the
    /// validator's per-prefab collider/layer/renderer checks on them. Exercises the collider maths.
    /// </summary>
    private static int BuilderSmoke()
    {
        Console.WriteLine("\n== Builder smoke test (all catalog assets, validator per-prefab checks)");
        var report = new WonderJump.EditorTools.Generation.GenerationReport();
        var materials = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>();
        foreach (string n in WonderJump.EditorTools.Generation.AssetCatalog.AllMaterialNames())
            materials[n] = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")) { name = n };
        var meshes = new System.Collections.Generic.Dictionary<WonderJump.EditorTools.Generation.ShapeKind, UnityEngine.Mesh>();
        foreach (var shape in new[] { WonderJump.EditorTools.Generation.ShapeKind.Cone, WonderJump.EditorTools.Generation.ShapeKind.Torus, WonderJump.EditorTools.Generation.ShapeKind.Octahedron })
        {
            var mesh = new UnityEngine.Mesh();
            WonderJump.EditorTools.Generation.ProceduralMeshes.WriteTo(mesh, WonderJump.EditorTools.Generation.ProceduralMeshes.Build(shape), shape.ToString());
            meshes[shape] = mesh;
        }
        var ctx = new WonderJump.EditorTools.Generation.BuildContext { Materials = materials, Meshes = meshes, Report = report };

        Type validator = typeof(WonderJump.EditorTools.Validation.WonderJumpAssetValidator);
        MethodInfo vChar = validator.GetMethod("ValidateCharacter", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo vSimple = validator.GetMethod("ValidateSimple", BindingFlags.NonPublic | BindingFlags.Static);
        var vr = new WonderJump.EditorTools.Validation.ValidationReport();
        var seen = new System.Collections.Generic.HashSet<WonderJump.Characters.CharacterId>();
        int built = 0;

        foreach (var def in WonderJump.EditorTools.Generation.AssetCatalog.Characters)
        {
            UnityEngine.GameObject go = WonderJump.EditorTools.Generation.PrefabBuilder.BuildCharacter(def, ctx);
            vChar.Invoke(null, new object[] { def, go, def.Id + ".prefab", vr, seen });
            var cap = go.GetComponent<UnityEngine.CapsuleCollider>();
            Console.WriteLine(string.Format("  {0,-6} capsule h={1:F3} r={2:F3} bottom={3:F3}  visual lift={4:F3}",
                def.Id, cap.height, cap.radius, cap.center.y - cap.height / 2f, go.transform.Find("VisualRoot").localPosition.y));
            built++;
        }
        foreach (var def in WonderJump.EditorTools.Generation.AssetCatalog.SimpleAssets)
        {
            UnityEngine.GameObject go = WonderJump.EditorTools.Generation.PrefabBuilder.BuildSimple(def, ctx);
            vSimple.Invoke(null, new object[] { def, go, def.Id + ".prefab", vr });
            built++;
        }

        Console.WriteLine("  built " + built + " prefabs, builder errors " + report.Errors.Count + ", validator: " + vr.ToString().Trim());
        foreach (string e in report.Errors) Console.WriteLine("  BUILD ERROR " + e);
        return report.Errors.Count + vr.Errors.Count;
    }

    private static void Invoke(Type fixture, object instance, Type attribute)
    {
        foreach (MethodInfo m in fixture.GetMethods().Where(m => m.GetCustomAttributes(attribute, false).Length > 0)) m.Invoke(instance, null);
    }
}
