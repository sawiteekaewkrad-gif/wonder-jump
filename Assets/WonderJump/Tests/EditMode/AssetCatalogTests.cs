using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WonderJump.Characters;
using WonderJump.EditorTools.Generation;
using WonderJump.Worlds;

namespace WonderJump.Tests.EditMode
{
    public class AssetCatalogTests
    {
        private static SimpleAssetDef Get(string id)
        {
            return AssetCatalog.SimpleAssets.Single(d => d.Id == id);
        }

        [Test]
        public void SixCharacterIds_ArePreservedExactly()
        {
            CollectionAssert.AreEqual(
                new[] { "Leon", "Elin", "Miyu", "Koko", "Riku", "Flora" },
                AssetCatalog.Characters.Select(c => c.Id.ToString()).ToArray());
            CollectionAssert.AreEquivalent(CharacterIds.All, AssetCatalog.Characters.Select(c => c.Id).ToArray());
        }

        [Test]
        public void AssetIds_AreUnique_AndCountsMatchV1()
        {
            var ids = AssetCatalog.SimpleAssets.Select(d => d.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "duplicate asset id");
            Assert.AreEqual(7, AssetCatalog.SimpleAssets.Count(d => d.Category == AssetCategory.Enemy));
            Assert.AreEqual(5, AssetCatalog.SimpleAssets.Count(d => d.Category == AssetCategory.Boss));
            Assert.AreEqual(6, AssetCatalog.SimpleAssets.Count(d => d.Category == AssetCategory.Item));
            Assert.AreEqual(6, AssetCatalog.SimpleAssets.Count(d => d.Category == AssetCategory.Prop));
            Assert.AreEqual(8, AssetCatalog.SimpleAssets.Count(d => d.Category == AssetCategory.Platform || d.Category == AssetCategory.Hazard));
            Assert.AreEqual(38, AssetCatalog.ExpectedPrefabCount);
        }

        [Test]
        public void WorldAssignments_FollowPhase0Brief()
        {
            Assert.AreEqual(WorldId.CrystalCave, Get("LavaSlime").World);
            Assert.AreEqual(WorldId.CrystalCave, Get("Lava_Hazard").World);
            Assert.AreEqual(WorldId.CrystalCave, Get("CrystalCrawler").World);
            Assert.AreEqual(WorldId.OceanAdventure, Get("OceanPuffer").World);
            Assert.AreEqual(WorldId.SkyCastle, Get("SkyDrone").World);
            Assert.AreEqual(WorldId.SkyCastle, Get("Stone_Platform").World);
            Assert.AreEqual(WorldId.GreenForest, Get("MushroomWalker").World);
            Assert.AreEqual(WorldId.GreenForest, Get("ForestBee").World);
        }

        [Test]
        public void EveryWorld_HasOneBoss_APlatform_AndAnEnemy()
        {
            foreach (WorldId world in WorldInfo.PlayableWorlds)
            {
                List<SimpleAssetDef> inWorld = AssetCatalog.SimpleAssets.Where(d => d.World == world).ToList();
                Assert.AreEqual(1, inWorld.Count(d => d.Category == AssetCategory.Boss), world + " boss");
                Assert.GreaterOrEqual(inWorld.Count(d => d.Category == AssetCategory.Platform), 1, world + " platform");
                Assert.GreaterOrEqual(inWorld.Count(d => d.Category == AssetCategory.Enemy), 1, world + " enemy");
            }
        }

        [Test]
        public void Items_AreTriggers_AndCharactersEnemiesStandOnFeet()
        {
            foreach (SimpleAssetDef d in AssetCatalog.SimpleAssets)
            {
                if (d.Category == AssetCategory.Item) Assert.AreEqual(ColliderRole.Trigger, d.Collider, d.Id);
                if (d.Category == AssetCategory.Enemy || d.Category == AssetCategory.Boss) Assert.AreEqual(PivotMode.Feet, d.Pivot, d.Id);
                if (d.Category == AssetCategory.Platform) Assert.AreEqual(PivotMode.Top, d.Pivot, d.Id);
            }
        }

        [Test]
        public void Coin_StandsUpright_ToFaceSideCamera()
        {
            Assert.AreEqual(90f, Get("Coin").VisualRotation.x, 1e-4f);
        }

        [Test]
        public void Materials_AreUnique_NoSleeveDuplicates()
        {
            List<string> names = AssetCatalog.AllMaterialNames();
            Assert.AreEqual(34, names.Count);
            Assert.AreEqual(names.Count, names.Distinct().Count());
            Assert.IsFalse(names.Any(n => n.EndsWith("_Sleeve")));
            foreach (SimpleAssetDef d in AssetCatalog.SimpleAssets) CollectionAssert.Contains(names, d.Material, d.Id);
        }

        [Test]
        public void PrefabPaths_AreUniqueAndOrganisedByCategoryThenWorld()
        {
            var paths = AssetCatalog.SimpleAssets.Select(AssetCatalog.RelativePrefabPath)
                .Concat(AssetCatalog.Characters.Select(AssetCatalog.RelativePrefabPath)).ToList();
            Assert.AreEqual(paths.Count, paths.Distinct().Count());
            Assert.AreEqual("Enemies/CrystalCave/LavaSlime.prefab", AssetCatalog.RelativePrefabPath(Get("LavaSlime")));
            Assert.AreEqual("Items/Coin.prefab", AssetCatalog.RelativePrefabPath(Get("Coin")));
            Assert.AreEqual("Characters/Leon.prefab", AssetCatalog.RelativePrefabPath(AssetCatalog.Characters[0]));
        }
    }
}
