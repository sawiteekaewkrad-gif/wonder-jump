using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using WonderJump.Characters;
using WonderJump.Core;
using WonderJump.Gameplay;
using WonderJump.Worlds;

namespace WonderJump.EditorTools.Generation
{
    public enum AssetCategory { Character, Enemy, Boss, Item, Platform, Hazard, Prop }

    public enum ShapeKind { Sphere, Capsule, Cylinder, Cube, Cone, Torus, Octahedron }

    /// <summary>
    /// Where the prefab's origin sits relative to its visual.
    /// Feet: bottom at y=0 (characters, enemies, props). Top: top surface at y=0 (platforms, hazards).
    /// Center: visual centre at origin (floating items).
    /// </summary>
    public enum PivotMode { Feet, Top, Center }

    /// <summary>None = decorative. Solid = blocks movement. Trigger = overlap only.</summary>
    public enum ColliderRole { None, Solid, Trigger }

    public enum AccessoryKind { Goggles, BunnyEars, WitchHat, FoxEars, Headphones, FlowerCrown }

    public sealed class CharacterDef
    {
        public CharacterId Id;
        public string DisplayName;
        public Color Outfit;
        public Color Hair;
        public AccessoryKind Accessory;

        public string OutfitMaterial { get { return Id + "_Outfit"; } }
        public string HairMaterial { get { return Id + "_Hair"; } }

        public string Fingerprint()
        {
            return "char|" + Id + "|" + DisplayName + "|" + AssetCatalog.F(Outfit) + "|" + AssetCatalog.F(Hair) + "|" + Accessory;
        }
    }

    public sealed class SimpleAssetDef
    {
        public string Id;
        public AssetCategory Category;
        public WorldId World;
        public ShapeKind Shape;
        /// <summary>Visual size (scale of the unit primitive).</summary>
        public Vector3 Size;
        public string Material;
        public ColliderRole Collider;
        public string Layer;
        public PivotMode Pivot;
        /// <summary>Only for Category == Item.</summary>
        public CollectibleType ItemType;
        public int ItemValue;
        /// <summary>Adds a kinematic Rigidbody (moving platforms, enemies, bosses).</summary>
        public bool Kinematic;
        /// <summary>Local Euler rotation of the visual mesh (e.g. coins stand upright to face the side camera).</summary>
        public Vector3 VisualRotation;

        public string Fingerprint()
        {
            return "simple|" + Id + "|" + Category + "|" + World + "|" + Shape + "|" + AssetCatalog.F(Size) + "|" +
                   Material + "|" + Collider + "|" + Layer + "|" + Pivot + "|" + ItemType + "|" + ItemValue + "|" + Kinematic + "|" +
                   AssetCatalog.F(VisualRotation);
        }
    }

    public sealed class MaterialDef
    {
        public string Name;
        public Color Color;
        public float Metallic;
        public float Smoothness;

        public MaterialDef(string name, Color color, float metallic, float smoothness)
        {
            Name = name;
            Color = color;
            Metallic = metallic;
            Smoothness = smoothness;
        }
    }

    /// <summary>
    /// Single source of truth for every generated asset: what it is, which world it belongs to,
    /// how it looks and how it collides. The generator, the validator and the tests all read this.
    /// </summary>
    public static class AssetCatalog
    {
        public const string CharactersFolder = "Characters";
        public const string ItemsFolder = "Items";

        // ------------------------------------------------------------------ materials
        // 22 shared materials (same colours as Asset Pack v1) + 2 per character = 34 total.
        // v1 also created "<Id>_Sleeve" with the same colour as "<Id>_Outfit"; sleeves now reuse Outfit.
        public static readonly MaterialDef[] BaseMaterials =
        {
            new MaterialDef("M_Cream",   new Color(1f, .88f, .68f), 0f, .3f),
            new MaterialDef("M_Skin",    new Color(1f, .72f, .58f), 0f, .3f),
            new MaterialDef("M_Dark",    new Color(.16f, .12f, .20f), 0f, .3f),
            new MaterialDef("M_Eye",     new Color(.04f, .07f, .13f), 0f, .8f),
            new MaterialDef("M_White",   Color.white, 0f, .3f),
            new MaterialDef("M_Gold",    new Color(1f, .72f, .16f), .55f, .6f),
            new MaterialDef("M_Crystal", new Color(.15f, .78f, 1f), .35f, .8f),
            new MaterialDef("M_Purple",  new Color(.48f, .22f, .82f), 0f, .3f),
            new MaterialDef("M_Pink",    new Color(1f, .40f, .65f), 0f, .3f),
            new MaterialDef("M_Blue",    new Color(.18f, .48f, .92f), 0f, .3f),
            new MaterialDef("M_Green",   new Color(.24f, .72f, .36f), 0f, .3f),
            new MaterialDef("M_Orange",  new Color(1f, .43f, .12f), 0f, .3f),
            new MaterialDef("M_Grass",   new Color(.25f, .72f, .28f), 0f, .2f),
            new MaterialDef("M_Earth",   new Color(.48f, .27f, .16f), 0f, .1f),
            new MaterialDef("M_Stone",   new Color(.42f, .45f, .55f), 0f, .2f),
            new MaterialDef("M_Ice",     new Color(.45f, .82f, 1f), 0f, .7f),
            new MaterialDef("M_Lava",    new Color(1f, .24f, .04f), 0f, .5f),
            new MaterialDef("M_Candy",   new Color(1f, .55f, .78f), 0f, .5f),
            new MaterialDef("M_Ocean",   new Color(.05f, .48f, .78f), 0f, .6f),
            new MaterialDef("M_Leaf",    new Color(.28f, .65f, .28f), 0f, .2f),
            new MaterialDef("M_Wood",    new Color(.43f, .24f, .12f), 0f, .1f),
            new MaterialDef("M_Red",     new Color(.85f, .12f, .16f), 0f, .3f)
        };

        // ------------------------------------------------------------------ characters
        // IDs, display names, colours and accessories are unchanged from Asset Pack v1.
        public static readonly CharacterDef[] Characters =
        {
            new CharacterDef { Id = CharacterId.Leon,  DisplayName = "Leon - Adventurer",     Outfit = new Color(.16f, .38f, .78f), Hair = new Color(.36f, .20f, .10f), Accessory = AccessoryKind.Goggles },
            new CharacterDef { Id = CharacterId.Elin,  DisplayName = "Elin - Bunny Explorer", Outfit = new Color(1f, .55f, .72f),   Hair = new Color(1f, .70f, .82f),   Accessory = AccessoryKind.BunnyEars },
            new CharacterDef { Id = CharacterId.Miyu,  DisplayName = "Miyu - Little Witch",   Outfit = new Color(.48f, .22f, .78f), Hair = new Color(.55f, .28f, .80f), Accessory = AccessoryKind.WitchHat },
            new CharacterDef { Id = CharacterId.Koko,  DisplayName = "Koko - Fox Scout",      Outfit = new Color(1f, .58f, .18f),   Hair = new Color(1f, .68f, .30f),   Accessory = AccessoryKind.FoxEars },
            new CharacterDef { Id = CharacterId.Riku,  DisplayName = "Riku - Sky Runner",     Outfit = new Color(.12f, .48f, .86f), Hair = new Color(.12f, .45f, .95f), Accessory = AccessoryKind.Headphones },
            new CharacterDef { Id = CharacterId.Flora, DisplayName = "Flora - Flower Fairy",  Outfit = new Color(.28f, .72f, .38f), Hair = new Color(.45f, .82f, .45f), Accessory = AccessoryKind.FlowerCrown }
        };

        // ------------------------------------------------------------------ everything else
        public static readonly SimpleAssetDef[] SimpleAssets = BuildSimpleAssets();

        private static SimpleAssetDef[] BuildSimpleAssets()
        {
            var list = new List<SimpleAssetDef>();

            // --- 1. Green Forest
            Platform(list, "Forest_Ground", WorldId.GreenForest, "M_Grass");
            Enemy(list, "MushroomWalker", WorldId.GreenForest, ShapeKind.Sphere, new Vector3(.8f, .65f, .7f), "M_Red");
            Enemy(list, "ForestBee", WorldId.GreenForest, ShapeKind.Sphere, new Vector3(.55f, .45f, .55f), "M_Gold");
            Boss(list, "Boss_ForestGuardian", WorldId.GreenForest, ShapeKind.Capsule, new Vector3(2.2f, 2.4f, 1.8f), "M_Green");
            Prop(list, "FantasyTree", WorldId.GreenForest, ShapeKind.Cylinder, new Vector3(.35f, 1.2f, .35f), "M_Wood", ColliderRole.None, null);
            Prop(list, "TreeCanopy", WorldId.GreenForest, ShapeKind.Sphere, new Vector3(1.6f, 1.5f, 1.4f), "M_Leaf", ColliderRole.None, null);
            Prop(list, "MushroomProp", WorldId.GreenForest, ShapeKind.Capsule, new Vector3(.6f, .8f, .6f), "M_Pink", ColliderRole.None, null);

            // --- 2. Candy Kingdom
            Platform(list, "Candy_Platform", WorldId.CandyKingdom, "M_Candy");
            Enemy(list, "CandyJumper", WorldId.CandyKingdom, ShapeKind.Sphere, new Vector3(.75f, .75f, .75f), "M_Pink");
            Boss(list, "Boss_CandyKing", WorldId.CandyKingdom, ShapeKind.Capsule, new Vector3(2.2f, 2.5f, 1.8f), "M_Pink");

            // --- 3. Ocean Adventure
            Platform(list, "Ocean_Platform", WorldId.OceanAdventure, "M_Ocean");
            Enemy(list, "OceanPuffer", WorldId.OceanAdventure, ShapeKind.Sphere, new Vector3(.8f, .8f, .8f), "M_Ocean");
            Boss(list, "Boss_SeaMonster", WorldId.OceanAdventure, ShapeKind.Sphere, new Vector3(2.5f, 2.2f, 2f), "M_Ocean");

            // --- 4. Crystal Cave (also hosts the lava content)
            Platform(list, "Crystal_Platform", WorldId.CrystalCave, "M_Ice");
            Enemy(list, "CrystalCrawler", WorldId.CrystalCave, ShapeKind.Cube, new Vector3(.8f, .55f, .8f), "M_Purple");
            Enemy(list, "LavaSlime", WorldId.CrystalCave, ShapeKind.Sphere, new Vector3(.8f, .55f, .8f), "M_Lava");
            list.Add(new SimpleAssetDef
            {
                Id = "Lava_Hazard", Category = AssetCategory.Hazard, World = WorldId.CrystalCave,
                Shape = ShapeKind.Cube, Size = new Vector3(3f, .25f, 1f), Material = "M_Lava",
                Collider = ColliderRole.Trigger, Layer = WonderJumpLayers.Hazard, Pivot = PivotMode.Top
            });
            Boss(list, "Boss_CrystalGolem", WorldId.CrystalCave, ShapeKind.Cube, new Vector3(2.3f, 2.5f, 1.8f), "M_Crystal");
            Prop(list, "CrystalCluster", WorldId.CrystalCave, ShapeKind.Octahedron, new Vector3(.7f, 1.4f, .7f), "M_Crystal", ColliderRole.None, null);

            // --- 5. Sky Castle (Stone_Platform = castle masonry; see AssetManifest.md for the rationale)
            Platform(list, "Sky_Platform", WorldId.SkyCastle, "M_White");
            Platform(list, "Stone_Platform", WorldId.SkyCastle, "M_Stone");
            Enemy(list, "SkyDrone", WorldId.SkyCastle, ShapeKind.Sphere, new Vector3(.65f, .65f, .65f), "M_Gold");
            Boss(list, "Boss_SkyOverlord", WorldId.SkyCastle, ShapeKind.Capsule, new Vector3(2.4f, 2.8f, 1.8f), "M_Purple");

            // --- Shared
            list.Add(new SimpleAssetDef
            {
                Id = "Moving_Platform", Category = AssetCategory.Platform, World = WorldId.Shared,
                Shape = ShapeKind.Cube, Size = new Vector3(2f, .35f, 1f), Material = "M_Gold",
                Collider = ColliderRole.Solid, Layer = WonderJumpLayers.Ground, Pivot = PivotMode.Top, Kinematic = true
            });
            Prop(list, "WoodenCrate", WorldId.Shared, ShapeKind.Cube, Vector3.one, "M_Wood", ColliderRole.Solid, WonderJumpLayers.Ground);
            Prop(list, "PortalFrame", WorldId.Shared, ShapeKind.Torus, new Vector3(1.5f, 1.8f, .3f), "M_Gold", ColliderRole.Trigger, WonderJumpLayers.Interactable);

            // --- Collectibles (shared). Values are placeholders until the GDD sets the economy.
            // v1 coin lay flat (edge-on to the side camera); stand it upright so its face points at the camera.
            Item(list, "Coin", ShapeKind.Cylinder, new Vector3(.45f, .08f, .45f), "M_Gold", CollectibleType.Coin, 1).VisualRotation = new Vector3(90f, 0f, 0f);
            Item(list, "Star", ShapeKind.Sphere, new Vector3(.45f, .45f, .20f), "M_Gold", CollectibleType.Star, 1);
            Item(list, "Heart", ShapeKind.Sphere, new Vector3(.45f, .45f, .25f), "M_Pink", CollectibleType.Heart, 1);
            Item(list, "CrystalGem", ShapeKind.Octahedron, new Vector3(.45f, .65f, .35f), "M_Crystal", CollectibleType.CrystalGem, 10);
            Item(list, "Key", ShapeKind.Cylinder, new Vector3(.18f, .55f, .18f), "M_Gold", CollectibleType.Key, 1);
            Item(list, "PowerUp", ShapeKind.Sphere, new Vector3(.5f, .5f, .5f), "M_Purple", CollectibleType.PowerUp, 1);

            return list.ToArray();
        }

        private static void Platform(List<SimpleAssetDef> list, string id, WorldId world, string material)
        {
            list.Add(new SimpleAssetDef
            {
                Id = id, Category = AssetCategory.Platform, World = world, Shape = ShapeKind.Cube,
                Size = new Vector3(4f, .5f, 1f), Material = material, Collider = ColliderRole.Solid,
                Layer = WonderJumpLayers.Ground, Pivot = PivotMode.Top
            });
        }

        private static void Enemy(List<SimpleAssetDef> list, string id, WorldId world, ShapeKind shape, Vector3 size, string material)
        {
            list.Add(new SimpleAssetDef
            {
                Id = id, Category = AssetCategory.Enemy, World = world, Shape = shape, Size = size,
                Material = material, Collider = ColliderRole.Solid, Layer = WonderJumpLayers.Enemy,
                Pivot = PivotMode.Feet, Kinematic = true
            });
        }

        private static void Boss(List<SimpleAssetDef> list, string id, WorldId world, ShapeKind shape, Vector3 size, string material)
        {
            list.Add(new SimpleAssetDef
            {
                Id = id, Category = AssetCategory.Boss, World = world, Shape = shape, Size = size,
                Material = material, Collider = ColliderRole.Solid, Layer = WonderJumpLayers.Enemy,
                Pivot = PivotMode.Feet, Kinematic = true
            });
        }

        private static void Prop(List<SimpleAssetDef> list, string id, WorldId world, ShapeKind shape, Vector3 size,
            string material, ColliderRole collider, string layer)
        {
            list.Add(new SimpleAssetDef
            {
                Id = id, Category = AssetCategory.Prop, World = world, Shape = shape, Size = size,
                Material = material, Collider = collider, Layer = layer, Pivot = PivotMode.Feet
            });
        }

        private static SimpleAssetDef Item(List<SimpleAssetDef> list, string id, ShapeKind shape, Vector3 size, string material,
            CollectibleType type, int value)
        {
            var def = new SimpleAssetDef
            {
                Id = id, Category = AssetCategory.Item, World = WorldId.Shared, Shape = shape, Size = size,
                Material = material, Collider = ColliderRole.Trigger, Layer = WonderJumpLayers.Collectible,
                Pivot = PivotMode.Center, ItemType = type, ItemValue = value
            };
            list.Add(def);
            return def;
        }

        // ------------------------------------------------------------------ paths

        public static string CategoryFolder(AssetCategory category)
        {
            switch (category)
            {
                case AssetCategory.Character: return CharactersFolder;
                case AssetCategory.Enemy: return "Enemies";
                case AssetCategory.Boss: return "Bosses";
                case AssetCategory.Item: return ItemsFolder;
                case AssetCategory.Platform: return "Platforms";
                case AssetCategory.Hazard: return "Hazards";
                default: return "Props";
            }
        }

        /// <summary>Path relative to "&lt;root&gt;/Prefabs/". Characters and items are not split by world.</summary>
        public static string RelativePrefabPath(SimpleAssetDef def)
        {
            if (def.Category == AssetCategory.Item) return ItemsFolder + "/" + def.Id + ".prefab";
            return CategoryFolder(def.Category) + "/" + WorldInfo.FolderName(def.World) + "/" + def.Id + ".prefab";
        }

        public static string RelativePrefabPath(CharacterDef def)
        {
            return CharactersFolder + "/" + def.Id + ".prefab";
        }

        /// <summary>All expected material names (34).</summary>
        public static List<string> AllMaterialNames()
        {
            var names = new List<string>();
            for (int i = 0; i < BaseMaterials.Length; i++) names.Add(BaseMaterials[i].Name);
            for (int i = 0; i < Characters.Length; i++)
            {
                names.Add(Characters[i].OutfitMaterial);
                names.Add(Characters[i].HairMaterial);
            }
            return names;
        }

        public static int ExpectedPrefabCount
        {
            get { return Characters.Length + SimpleAssets.Length; }
        }

        // ------------------------------------------------------------------ fingerprint helpers

        internal static string F(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R}", v.x, v.y, v.z);
        }

        internal static string F(Color c)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R},{3:R}", c.r, c.g, c.b, c.a);
        }

        /// <summary>Human-readable table for docs / debugging.</summary>
        public static string Describe()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < SimpleAssets.Length; i++)
            {
                SimpleAssetDef d = SimpleAssets[i];
                sb.Append(d.World).Append('\t').Append(d.Category).Append('\t').Append(d.Id).Append('\n');
            }
            return sb.ToString();
        }
    }
}
