#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace WonderJump.EditorTools
{
    public static class WonderJumpAssetGenerator
    {
        private const string Root = "Assets/WonderJumpGenerated";
        private static string MatDir => Root + "/Materials";
        private static string CharDir => Root + "/Prefabs/Characters";
        private static string EnemyDir => Root + "/Prefabs/Enemies";
        private static string BossDir => Root + "/Prefabs/Bosses";
        private static string ItemDir => Root + "/Prefabs/Items";
        private static string WorldDir => Root + "/Prefabs/Worlds";
        private static string PropDir => Root + "/Prefabs/Props";

        [MenuItem("Wonder Jump/Generate Complete 3D Asset Pack")]
        public static void GenerateAll()
        {
            EnsureFolders();
            GenerateMaterials();
            GenerateCharacters();
            GenerateEnemies();
            GenerateBosses();
            GenerateItems();
            GenerateWorldAssets();
            GenerateProps();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Wonder Jump",
                "Asset generation complete! See Assets/WonderJumpGenerated/Prefabs.", "OK");
        }

        private static void EnsureFolders()
        {
            string[] folders = {
                "Assets/WonderJumpGenerated", MatDir,
                Root + "/Prefabs", Root + "/Prefabs/Characters",
                Root + "/Prefabs/Enemies", Root + "/Prefabs/Bosses",
                Root + "/Prefabs/Items", Root + "/Prefabs/Worlds",
                Root + "/Prefabs/Props"
            };
            foreach (var f in folders)
                if (!AssetDatabase.IsValidFolder(f))
                {
                    string parent = Path.GetDirectoryName(f).Replace("\\", "/");
                    string name = Path.GetFileName(f);
                    if (!AssetDatabase.IsValidFolder(parent))
                        Directory.CreateDirectory(parent);
                    AssetDatabase.CreateFolder(parent, name);
                }
        }

        private static Material MakeMat(string name, Color color, float metallic = 0f)
        {
            string path = MatDir + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader);
            m.name = name;
            m.color = color;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material Get(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + name + ".mat");

        private static void GenerateMaterials()
        {
            MakeMat("M_Cream", new Color(1f, .88f, .68f));
            MakeMat("M_Skin", new Color(1f, .72f, .58f));
            MakeMat("M_Dark", new Color(.16f, .12f, .20f));
            MakeMat("M_Eye", new Color(.04f, .07f, .13f));
            MakeMat("M_White", Color.white);
            MakeMat("M_Gold", new Color(1f, .72f, .16f), .55f);
            MakeMat("M_Crystal", new Color(.15f, .78f, 1f), .35f);
            MakeMat("M_Purple", new Color(.48f, .22f, .82f));
            MakeMat("M_Pink", new Color(1f, .40f, .65f));
            MakeMat("M_Blue", new Color(.18f, .48f, .92f));
            MakeMat("M_Green", new Color(.24f, .72f, .36f));
            MakeMat("M_Orange", new Color(1f, .43f, .12f));
            MakeMat("M_Grass", new Color(.25f, .72f, .28f));
            MakeMat("M_Earth", new Color(.48f, .27f, .16f));
            MakeMat("M_Stone", new Color(.42f, .45f, .55f));
            MakeMat("M_Ice", new Color(.45f, .82f, 1f));
            MakeMat("M_Lava", new Color(1f, .24f, .04f));
            MakeMat("M_Candy", new Color(1f, .55f, .78f));
            MakeMat("M_Ocean", new Color(.05f, .48f, .78f));
            MakeMat("M_Leaf", new Color(.28f, .65f, .28f));
            MakeMat("M_Wood", new Color(.43f, .24f, .12f));
            MakeMat("M_Red", new Color(.85f, .12f, .16f));
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent,
            Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r && mat) r.sharedMaterial = mat;
            return go;
        }

        private static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void MakeCharacter(string id, string display, Color outfit,
            Color hair, string accessory)
        {
            var rootGo = new GameObject(id);
            rootGo.tag = "Untagged";
            var body = new GameObject("VisualRoot");
            body.transform.SetParent(rootGo.transform, false);

            Primitive(PrimitiveType.Capsule, "Body", body.transform,
                new Vector3(0, .78f, 0), new Vector3(.58f, .72f, .42f), MakeMatRuntime(id+"_Outfit", outfit));
            Primitive(PrimitiveType.Sphere, "Head", body.transform,
                new Vector3(0, 1.55f, 0), new Vector3(.92f, .88f, .78f), MakeMatRuntime(id+"_Hair", hair));
            Primitive(PrimitiveType.Sphere, "Face", body.transform,
                new Vector3(0, 1.48f, -.20f), new Vector3(.72f, .62f, .18f), Get("M_Skin"));
            Primitive(PrimitiveType.Sphere, "Eye_L", body.transform,
                new Vector3(-.20f, 1.57f, -.30f), new Vector3(.105f, .15f, .07f), Get("M_Eye"));
            Primitive(PrimitiveType.Sphere, "Eye_R", body.transform,
                new Vector3(.20f, 1.57f, -.30f), new Vector3(.105f, .15f, .07f), Get("M_Eye"));
            Primitive(PrimitiveType.Sphere, "EyeHighlight_L", body.transform,
                new Vector3(-.225f, 1.62f, -.36f), new Vector3(.035f, .045f, .025f), Get("M_White"));
            Primitive(PrimitiveType.Sphere, "EyeHighlight_R", body.transform,
                new Vector3(.175f, 1.62f, -.36f), new Vector3(.035f, .045f, .025f), Get("M_White"));
            Primitive(PrimitiveType.Sphere, "Arm_L", body.transform,
                new Vector3(-.39f, .91f, 0), new Vector3(.22f, .48f, .23f), MakeMatRuntime(id+"_Sleeve", outfit));
            Primitive(PrimitiveType.Sphere, "Arm_R", body.transform,
                new Vector3(.39f, .91f, 0), new Vector3(.22f, .48f, .23f), MakeMatRuntime(id+"_Sleeve", outfit));
            Primitive(PrimitiveType.Capsule, "Leg_L", body.transform,
                new Vector3(-.17f, .27f, 0), new Vector3(.22f, .45f, .24f), Get("M_Dark"));
            Primitive(PrimitiveType.Capsule, "Leg_R", body.transform,
                new Vector3(.17f, .27f, 0), new Vector3(.22f, .45f, .24f), Get("M_Dark"));

            // Signature accessory is a recognizable placeholder prop, not a final rigged mesh.
            if (accessory == "Goggles")
            {
                Primitive(PrimitiveType.Cylinder, "GogglesBand", body.transform,
                    new Vector3(0, 1.89f, 0), new Vector3(.48f, .07f, .48f), Get("M_Gold"));
                Primitive(PrimitiveType.Sphere, "Goggle_L", body.transform,
                    new Vector3(-.20f, 1.79f, -.32f), new Vector3(.18f, .14f, .08f), Get("M_Crystal"));
                Primitive(PrimitiveType.Sphere, "Goggle_R", body.transform,
                    new Vector3(.20f, 1.79f, -.32f), new Vector3(.18f, .14f, .08f), Get("M_Crystal"));
            }
            else if (accessory == "WitchHat")
            {
                Primitive(PrimitiveType.Cylinder, "HatBrim", body.transform,
                    new Vector3(0, 2.02f, 0), new Vector3(.68f, .08f, .58f), Get("M_Purple"));
                Primitive(PrimitiveType.Cone, "HatCone", body.transform,
                    new Vector3(0, 2.30f, 0), new Vector3(.42f, .60f, .42f), Get("M_Purple"));
                Primitive(PrimitiveType.Sphere, "HatStar", body.transform,
                    new Vector3(.25f, 2.12f, -.27f), new Vector3(.12f, .12f, .08f), Get("M_Gold"));
            }
            else if (accessory == "BunnyEars")
            {
                Primitive(PrimitiveType.Capsule, "Ear_L", body.transform,
                    new Vector3(-.25f, 2.08f, 0), new Vector3(.18f, .48f, .15f), Get("M_Pink"));
                Primitive(PrimitiveType.Capsule, "Ear_R", body.transform,
                    new Vector3(.25f, 2.08f, 0), new Vector3(.18f, .48f, .15f), Get("M_Pink"));
            }
            else if (accessory == "FoxEars")
            {
                Primitive(PrimitiveType.Cone, "Ear_L", body.transform,
                    new Vector3(-.30f, 1.98f, 0), new Vector3(.28f, .40f, .22f), Get("M_Orange"));
                Primitive(PrimitiveType.Cone, "Ear_R", body.transform,
                    new Vector3(.30f, 1.98f, 0), new Vector3(.28f, .40f, .22f), Get("M_Orange"));
            }
            else if (accessory == "Headphones")
            {
                Primitive(PrimitiveType.Torus, "Headphones", body.transform,
                    new Vector3(0, 1.84f, 0), new Vector3(.62f, .62f, .18f), Get("M_Blue"));
                Primitive(PrimitiveType.Cylinder, "EarCup_L", body.transform,
                    new Vector3(-.47f, 1.58f, 0), new Vector3(.16f, .12f, .16f), Get("M_Crystal"));
                Primitive(PrimitiveType.Cylinder, "EarCup_R", body.transform,
                    new Vector3(.47f, 1.58f, 0), new Vector3(.16f, .12f, .16f), Get("M_Crystal"));
            }
            else if (accessory == "FlowerCrown")
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    Primitive(PrimitiveType.Sphere, "Flower_" + i, body.transform,
                        new Vector3(Mathf.Cos(a)*.37f, 1.94f, Mathf.Sin(a)*.30f),
                        Vector3.one*.16f, Get("M_Pink"));
                }
            }

            var capsule = rootGo.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, .95f, 0);
            capsule.height = 1.9f;
            capsule.radius = .38f;
            rootGo.AddComponent<Rigidbody>().constraints =
                RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            var identity = rootGo.AddComponent<WonderJump.Runtime.CharacterIdentity>();
            identity.characterId = id;
            identity.displayName = display;
            SavePrefab(rootGo, CharDir + "/" + id + ".prefab");
        }

        private static Material MakeMatRuntime(string name, Color c)
        {
            return MakeMat(name, c);
        }

        private static void GenerateCharacters()
        {
            MakeCharacter("Leon", "Leon - Adventurer", new Color(.16f,.38f,.78f), new Color(.36f,.20f,.10f), "Goggles");
            MakeCharacter("Elin", "Elin - Bunny Explorer", new Color(1f,.55f,.72f), new Color(1f,.70f,.82f), "BunnyEars");
            MakeCharacter("Miyu", "Miyu - Little Witch", new Color(.48f,.22f,.78f), new Color(.55f,.28f,.80f), "WitchHat");
            MakeCharacter("Koko", "Koko - Fox Scout", new Color(1f,.58f,.18f), new Color(1f,.68f,.30f), "FoxEars");
            MakeCharacter("Riku", "Riku - Sky Runner", new Color(.12f,.48f,.86f), new Color(.12f,.45f,.95f), "Headphones");
            MakeCharacter("Flora", "Flora - Flower Fairy", new Color(.28f,.72f,.38f), new Color(.45f,.82f,.45f), "FlowerCrown");
        }

        private static void MakeSimplePrefab(string name, string folder, PrimitiveType type,
            Vector3 scale, Material mat, bool collider = true)
        {
            var go = Primitive(type, name, null, Vector3.zero, scale, mat);
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c) Object.DestroyImmediate(c);
            }
            SavePrefab(go, folder + "/" + name + ".prefab");
        }

        private static void GenerateEnemies()
        {
            MakeSimplePrefab("MushroomWalker", EnemyDir, PrimitiveType.Sphere, new Vector3(.8f,.65f,.7f), Get("M_Red"));
            MakeSimplePrefab("CandyJumper", EnemyDir, PrimitiveType.Sphere, new Vector3(.75f,.75f,.75f), Get("M_Pink"));
            MakeSimplePrefab("OceanPuffer", EnemyDir, PrimitiveType.Sphere, new Vector3(.8f,.8f,.8f), Get("M_Ocean"));
            MakeSimplePrefab("CrystalCrawler", EnemyDir, PrimitiveType.Cube, new Vector3(.8f,.55f,.8f), Get("M_Purple"));
            MakeSimplePrefab("SkyDrone", EnemyDir, PrimitiveType.Sphere, new Vector3(.65f,.65f,.65f), Get("M_Gold"));
            MakeSimplePrefab("ForestBee", EnemyDir, PrimitiveType.Sphere, new Vector3(.55f,.45f,.55f), Get("M_Gold"));
            MakeSimplePrefab("LavaSlime", EnemyDir, PrimitiveType.Sphere, new Vector3(.8f,.55f,.8f), Get("M_Lava"));
        }

        private static void GenerateBosses()
        {
            MakeSimplePrefab("Boss_ForestGuardian", BossDir, PrimitiveType.Capsule, new Vector3(2.2f,2.4f,1.8f), Get("M_Green"));
            MakeSimplePrefab("Boss_CandyKing", BossDir, PrimitiveType.Capsule, new Vector3(2.2f,2.5f,1.8f), Get("M_Pink"));
            MakeSimplePrefab("Boss_SeaMonster", BossDir, PrimitiveType.Sphere, new Vector3(2.5f,2.2f,2f), Get("M_Ocean"));
            MakeSimplePrefab("Boss_CrystalGolem", BossDir, PrimitiveType.Cube, new Vector3(2.3f,2.5f,1.8f), Get("M_Crystal"));
            MakeSimplePrefab("Boss_SkyOverlord", BossDir, PrimitiveType.Capsule, new Vector3(2.4f,2.8f,1.8f), Get("M_Purple"));
        }

        private static void GenerateItems()
        {
            MakeSimplePrefab("Coin", ItemDir, PrimitiveType.Cylinder, new Vector3(.45f,.08f,.45f), Get("M_Gold"), false);
            MakeSimplePrefab("Star", ItemDir, PrimitiveType.Sphere, new Vector3(.45f,.45f,.20f), Get("M_Gold"), false);
            MakeSimplePrefab("Heart", ItemDir, PrimitiveType.Sphere, new Vector3(.45f,.45f,.25f), Get("M_Pink"), false);
            MakeSimplePrefab("CrystalGem", ItemDir, PrimitiveType.Octahedron, new Vector3(.45f,.65f,.35f), Get("M_Crystal"), false);
            MakeSimplePrefab("Key", ItemDir, PrimitiveType.Cylinder, new Vector3(.18f,.55f,.18f), Get("M_Gold"), false);
            MakeSimplePrefab("PowerUp", ItemDir, PrimitiveType.Sphere, new Vector3(.5f,.5f,.5f), Get("M_Purple"), false);
        }

        private static void GenerateWorldAssets()
        {
            MakeSimplePrefab("Forest_Ground", WorldDir, PrimitiveType.Cube, new Vector3(4f,.5f,1f), Get("M_Grass"));
            MakeSimplePrefab("Candy_Platform", WorldDir, PrimitiveType.Cube, new Vector3(4f,.5f,1f), Get("M_Candy"));
            MakeSimplePrefab("Ocean_Platform", WorldDir, PrimitiveType.Cube, new Vector3(4f,.5f,1f), Get("M_Ocean"));
            MakeSimplePrefab("Crystal_Platform", WorldDir, PrimitiveType.Cube, new Vector3(4f,.5f,1f), Get("M_Ice"));
            MakeSimplePrefab("Sky_Platform", WorldDir, PrimitiveType.Cube, new Vector3(4f,.5f,1f), Get("M_White"));
            MakeSimplePrefab("Stone_Platform", WorldDir, PrimitiveType.Cube, new Vector3(4f,.5f,1f), Get("M_Stone"));
            MakeSimplePrefab("Lava_Hazard", WorldDir, PrimitiveType.Cube, new Vector3(3f,.25f,1f), Get("M_Lava"));
            MakeSimplePrefab("Moving_Platform", WorldDir, PrimitiveType.Cube, new Vector3(2f,.35f,1f), Get("M_Gold"));
        }

        private static void GenerateProps()
        {
            MakeSimplePrefab("FantasyTree", PropDir, PrimitiveType.Cylinder, new Vector3(.35f,1.2f,.35f), Get("M_Wood"));
            MakeSimplePrefab("TreeCanopy", PropDir, PrimitiveType.Sphere, new Vector3(1.6f,1.5f,1.4f), Get("M_Leaf"));
            MakeSimplePrefab("MushroomProp", PropDir, PrimitiveType.Capsule, new Vector3(.6f,.8f,.6f), Get("M_Pink"));
            MakeSimplePrefab("WoodenCrate", PropDir, PrimitiveType.Cube, Vector3.one, Get("M_Wood"));
            MakeSimplePrefab("CrystalCluster", PropDir, PrimitiveType.Octahedron, new Vector3(.7f,1.4f,.7f), Get("M_Crystal"));
            MakeSimplePrefab("PortalFrame", PropDir, PrimitiveType.Torus, new Vector3(1.5f,1.8f,.3f), Get("M_Gold"));
        }
    }
}
#endif
