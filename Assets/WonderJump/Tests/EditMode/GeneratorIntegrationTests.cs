using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WonderJump.EditorTools;
using WonderJump.EditorTools.Generation;
using WonderJump.EditorTools.Validation;

namespace WonderJump.Tests.EditMode
{
    /// <summary>
    /// Runs the real generator into throw-away folders and checks: validation passes, regeneration is
    /// repeatable, hand edits survive, broken materials are repaired in place, variants are created.
    /// Requires the URP package (ignored otherwise). Note: EnsureLayers adds the Wonder Jump layers to
    /// the project's TagManager, which is the intended project setup anyway.
    /// </summary>
    public class GeneratorIntegrationTests
    {
        private const string TestGenerated = "Assets/__WJTest_Generated";
        private const string TestContent = "Assets/__WJTest_Content";

        private GeneratorOptions options;

        [SetUp]
        public void SetUp()
        {
            if (MaterialLibrary.ResolveShader() == null) Assert.Ignore("URP not installed in this project.");
            DeleteTestFolders();
            options = new GeneratorOptions { GeneratedRoot = TestGenerated, ContentRoot = TestContent, Interactive = false };
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestFolders();
        }

        private static void DeleteTestFolders()
        {
            AssetDatabase.DeleteAsset(TestGenerated);
            AssetDatabase.DeleteAsset(TestContent);
            AssetDatabase.Refresh();
        }

        private GenerationReport GenerateOk()
        {
            GenerationReport report = WonderJumpAssetGenerator.Generate(options);
            Assert.IsTrue(report.Succeeded, report.Summary());
            return report;
        }

        [Test]
        public void FirstRun_CreatesEverything_AndValidationPasses()
        {
            GenerationReport report = GenerateOk();
            Assert.AreEqual(AssetCatalog.ExpectedPrefabCount + 3, report.Created.Count, "38 prefabs + 3 meshes");
            Assert.AreEqual(34, report.MaterialsCreated.Count);
            Assert.AreEqual(AssetCatalog.ExpectedPrefabCount, report.VariantsCreated.Count);

            ValidationReport validation = WonderJumpAssetValidator.Validate(options);
            Assert.IsTrue(validation.Passed, validation.ToString());
        }

        [Test]
        public void SecondRun_IsRepeatable_NoDuplicatesNoChurn()
        {
            GenerateOk();
            string leonGuid = AssetDatabase.AssetPathToGUID(TestGenerated + "/Prefabs/Characters/Leon.prefab");
            string goldGuid = AssetDatabase.AssetPathToGUID(TestGenerated + "/Materials/M_Gold.mat");
            int filesBefore = Directory.GetFiles(AssetPaths.ToFullPath(TestGenerated), "*", SearchOption.AllDirectories).Length;

            GenerationReport second = GenerateOk();
            Assert.AreEqual(0, second.Created.Count, second.Summary());
            Assert.AreEqual(0, second.Regenerated.Count, second.Summary());
            Assert.AreEqual(0, second.SkippedModified.Count, "Unity re-serialised files after generation? " + second.Summary());
            Assert.AreEqual(AssetCatalog.ExpectedPrefabCount + 3, second.UpToDate.Count);
            Assert.AreEqual(0, second.MaterialsCreated.Count);
            Assert.AreEqual(34, second.MaterialsPreserved.Count);
            Assert.AreEqual(0, second.VariantsCreated.Count);

            int filesAfter = Directory.GetFiles(AssetPaths.ToFullPath(TestGenerated), "*", SearchOption.AllDirectories).Length;
            Assert.AreEqual(filesBefore, filesAfter, "no duplicate assets");
            Assert.AreEqual(leonGuid, AssetDatabase.AssetPathToGUID(TestGenerated + "/Prefabs/Characters/Leon.prefab"));
            Assert.AreEqual(goldGuid, AssetDatabase.AssetPathToGUID(TestGenerated + "/Materials/M_Gold.mat"));
        }

        [Test]
        public void HandModifiedPrefab_IsSkipped_UntilForced()
        {
            GenerateOk();
            string path = TestGenerated + "/Prefabs/Items/Coin.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            new GameObject("ManualEdit").transform.SetParent(contents.transform, false);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);

            GenerationReport second = GenerateOk();
            CollectionAssert.Contains(second.SkippedModified, path);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path).transform.Find("ManualEdit"), "manual edit preserved");

            string guid = AssetDatabase.AssetPathToGUID(path);
            options.Force = true;
            GenerationReport forced = GenerateOk();
            CollectionAssert.Contains(forced.Regenerated, path);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(path).transform.Find("ManualEdit"), "force rebuilds");
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path), "GUID kept");
        }

        [Test]
        public void BrokenMaterial_IsRepairedInPlace()
        {
            GenerateOk();
            string path = TestGenerated + "/Materials/M_Gold.mat";
            string guid = AssetDatabase.AssetPathToGUID(path);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            mat.shader = Shader.Find("Hidden/InternalErrorShader");
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            GenerationReport second = GenerateOk();
            Assert.IsTrue(second.MaterialsRepaired.Any(m => m.StartsWith(path)), second.Summary());
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.IsTrue(MaterialLibrary.HasUsableShader(mat));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
        }

        [Test]
        public void ContentVariants_AreVariants_AndNeverOverwritten()
        {
            GenerateOk();
            string variantPath = TestContent + "/Prefabs/Characters/Leon.prefab";
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            Assert.IsNotNull(variant);
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(variant));

            string hashBefore = GeneratorManifest.HashFile(variantPath);
            options.Force = true;
            GenerateOk();
            Assert.AreEqual(hashBefore, GeneratorManifest.HashFile(variantPath), "content variant untouched by force regeneration");
        }
    }
}
