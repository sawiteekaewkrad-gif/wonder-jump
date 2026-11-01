# Asset Manifest — generated placeholder assets (Generator v2)

Source of truth: `Assets/WonderJump/Editor/Generation/AssetCatalog.cs`. This document describes it; if they disagree, the code wins and this file must be fixed.

**All assets listed here are graybox PLACEHOLDERS** built from primitives. They are not final art: no sculpted models, rigs, animations or textures.

## Totals
| Type | Count | Location |
|---|---|---|
| Prefabs | 38 (6 characters, 7 enemies, 5 bosses, 6 items, 7 platforms, 1 hazard, 6 props) | `Assets/WonderJumpGenerated/Prefabs/` |
| Materials (URP/Lit) | 34 (22 shared + 2 per character) | `Assets/WonderJumpGenerated/Materials/` |
| Procedural meshes | 3 (`WJ_Cone`, `WJ_Torus`, `WJ_Octahedron`) | `Assets/WonderJumpGenerated/Meshes/` |
| Prefab Variants (editable) | 38, created only if missing | `Assets/WonderJumpContent/Prefabs/` (same sub-paths) |
| Manifest | 1 | `Assets/WonderJumpGenerated/_GeneratorManifest.json` |

v1 → v2 differences: the 6 `<Id>_Sleeve` materials were removed (identical to `<Id>_Outfit`), and the 3 meshes are new (they replace the non-existent `PrimitiveType.Cone/Torus/Octahedron`).

## Folder structure (category, then world)
```
Assets/WonderJumpGenerated/
  _GeneratorManifest.json
  Materials/            M_*.mat, <Character>_Outfit.mat, <Character>_Hair.mat
  Meshes/               WJ_Cone.asset, WJ_Torus.asset, WJ_Octahedron.asset
  Scenes/               Phase0_Showcase.unity (rebuilt by the build pipeline)
  Prefabs/
    Characters/         Leon Elin Miyu Koko Riku Flora
    Items/              Coin Star Heart CrystalGem Key PowerUp
    Enemies/<World>/
    Bosses/<World>/
    Platforms/<World|Shared>/
    Hazards/<World>/
    Props/<World|Shared>/
```

## Assets by world
| World | Platforms | Hazards | Enemies | Boss | Props |
|---|---|---|---|---|---|
| **1. Green Forest** (`GreenForest`) | Forest_Ground | – | MushroomWalker, ForestBee | Boss_ForestGuardian | FantasyTree, TreeCanopy, MushroomProp |
| **2. Candy Kingdom** (`CandyKingdom`) | Candy_Platform | – | CandyJumper | Boss_CandyKing | – |
| **3. Ocean Adventure** (`OceanAdventure`) | Ocean_Platform | – | OceanPuffer | Boss_SeaMonster | – |
| **4. Crystal Cave** (`CrystalCave`) | Crystal_Platform | **Lava_Hazard** | CrystalCrawler, **LavaSlime** | Boss_CrystalGolem | CrystalCluster |
| **5. Sky Castle** (`SkyCastle`) | Sky_Platform, **Stone_Platform** | – | SkyDrone | Boss_SkyOverlord | – |
| **Shared** | Moving_Platform | – | – | – | WoodenCrate, PortalFrame |
| **Shared** (characters/items) | 6 characters, 6 collectibles | | | | |

**Stone_Platform → Sky Castle (assumption, easy to change).** Castle masonry fits the theme, and it balances the worlds: Crystal Cave already has its own platform plus the lava content. To move it, change one line in `AssetCatalog.cs` and regenerate.

**Content gap:** Candy, Ocean and Sky have one enemy type each. About 3 per world are needed for 5 interesting levels (GDD task).

## Prefab anatomy (all prefabs)
```
<Id>                 Root: gameplay components + gameplay collider(s); unscaled; gameplay layer
├── VisualRoot       meshes only, NO colliders; offset so the pivot rule holds
│   └── ...parts
└── Hitbox           (enemies & bosses only) trigger collider for player contact
```

| Category | Pivot (origin) | Layer | Root components | Colliders |
|---|---|---|---|---|
| Character | Feet (y=0) | Player | CharacterIdentity, Rigidbody (kinematic, interpolated, rot + Z frozen) | 1 solid CapsuleCollider: height = 0.95 × body height, radius = 0.70 × body half-width, bottom exactly y=0. Accessories excluded |
| Enemy / Boss | Feet | Enemy | EnemyIdentity (id, world, isBoss), Rigidbody (kinematic) | Root: solid body = 0.90 × visual (box for angular shapes, capsule for round). `Hitbox`: trigger = 1.00 × visual |
| Item | Centre | Collectible | Collectible (type, value; collect-once guard) | 1 trigger SphereCollider, radius = 1.15 × max(visual half-width, half-height) |
| Platform | Top surface (y=0) | Ground | Moving_Platform also has a kinematic Rigidbody | 1 solid BoxCollider = visual |
| Hazard | Top surface | Hazard | – | 1 trigger BoxCollider = visual |
| Prop | Feet | Default / Ground / Interactable | – | Decor: none. WoodenCrate: solid box (Ground). PortalFrame: trigger box (Interactable) |

Measured on the builder (stub run): character capsule height 2.062, radius 0.322, bottom 0.000. The visual is lifted 0.180, which fixes v1's feet sinking 0.18 below the ground.

## Characters (IDs fixed)
| ID | Display name | Outfit | Accessory |
|---|---|---|---|
| Leon | Leon - Adventurer | blue | Goggles (gold band, crystal lenses) |
| Elin | Elin - Bunny Explorer | pink | Bunny ears |
| Miyu | Miyu - Little Witch | purple | Witch hat (**WJ_Cone**) with gold star |
| Koko | Koko - Fox Scout | orange | Fox ears (**WJ_Cone**) |
| Riku | Riku - Sky Runner | blue | Headphones (**WJ_Torus**) |
| Flora | Flora - Flower Fairy | green | Flower crown |

## Collectibles (values are placeholders until the GDD economy)
| Item | Type | Value | Shape |
|---|---|---|---|
| Coin | Coin | 1 | flat cylinder, **stood upright (X 90°) to face the side camera** (v1 lay flat, edge-on) |
| Star | Star | 1 | flat sphere |
| Heart | Heart | 1 | flat sphere |
| CrystalGem | CrystalGem | 10 | **WJ_Octahedron** |
| Key | Key | 1 | thin cylinder |
| PowerUp | PowerUp | 1 | sphere |

## Procedural meshes
All three fit a unit box centred on the origin (same convention as Unity's Cube/Sphere), so v1 scale values keep their intended size. Winding is outward (Unity clockwise convention); this is verified by tests.

| Mesh | Geometry | Vertices / triangles |
|---|---|---|
| WJ_Cone | radius 0.5, height 1, 24 segments, smooth sides + flat base | 75 / 48 |
| WJ_Torus | in the XY plane (faces the camera), R 0.375, r 0.125, 32×12 | 429 / 768 |
| WJ_Octahedron | vertices at ±0.5, flat-shaded | 24 / 8 |

## Regeneration rules
| Situation | Generator action |
|---|---|
| Asset missing | Create |
| Exists, unchanged since generation, definition unchanged | Skip ("up to date") |
| Exists, unchanged, definition changed (catalog edit or `BuildVersion` bump) | Rebuild in place (same GUID) |
| Exists, **edited by hand** (file hash ≠ manifest) | **Skip + report** |
| Exists, not in manifest | Skip + report ("untracked") |
| Force Regenerate (menu, confirmation) | Rebuild all generated assets; Content untouched |
| Material exists with a URP shader | Preserve (colour tweaks survive) |
| Material exists with a missing/non-URP/error shader | Repair in place (same GUID) |
| No URP shader available | Abort before writing anything |
| Content variant exists | Never modified |

## Not included (still missing)
Final chibi FBX models, humanoid rigs, animation clips (Idle, Run, Jump, Fall, Land, Hurt, KO, Revive, Victory), enemy/boss models and animations, toon shader, textures/atlases, backgrounds/skyboxes, Crystal Joystick art, UI, VFX, audio, level scenes, gameplay implementation, network implementation, APK.
