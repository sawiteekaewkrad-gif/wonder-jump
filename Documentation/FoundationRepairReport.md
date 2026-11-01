# Phase 0 — Foundation Repair & Technical Validation Report

| Item | Value |
|---|---|
| Date | 2026-11-01 |
| Input | `WonderJump_3D_AssetPack_Android_v1.zip` (archived in `Reference/AssetPack_v1/`) |
| Output | `Assets/WonderJump/**` (generator v2, runtime contracts, tests), docs in `Documentation/`, `CLAUDE.md`, `README.md` |
| Environment | Cloud Linux container. **No Unity Editor, no Android device.** C# checked with Mono `mcs` against hand-written API stubs |
| Status | **Waiting for owner review.** Phase 1 does not start until approved |

## 1. Verification summary (honest levels)
| Area | Inspected | Corrected | Stub-compiled (NOT Unity) | Unity-compiled | Unity Play Mode tested | Android device tested |
|---|---|---|---|---|---|---|
| Editor generator (meshes, materials, prefabs, colliders, regeneration) | ✅ | ✅ | ✅ | ❌ not run | ❌ not run | n/a |
| Validator + project setup tools | ✅ | ✅ (new) | ✅ | ❌ | ❌ | n/a |
| Runtime contracts + Collectible/EnemyIdentity/LevelId/GameAuthority | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| EditMode tests (22) | ✅ | ✅ (new) | ✅ compiled; **17 pure-logic tests executed: 17 pass** | ❌ | ❌ (5 integration tests need Unity) | n/a |
| PlayMode tests (3) | ✅ | ✅ (new) | ✅ compiled only | ❌ | ❌ | n/a |
| Android build | – | settings tool written | ✅ | ❌ | ❌ | ❌ **never built** |
| Multiplayer | design only | contracts only | ✅ | ❌ | ❌ | ❌ **not implemented** |

**I do not claim that the project compiles in Unity, passes Play Mode, or runs on Android.** Those are the owner's Phase 0 gate checks (§6).

## 2. Errors fixed
| # | v1 defect | Severity | Fix (file) |
|---|---|---|---|
| F01 | `PrimitiveType.Cone`, `.Torus`, `.Octahedron` don't exist (7 call sites), so the whole editor assembly failed to compile and Play Mode was blocked | **Blocker** | Procedural mesh builders saved as assets `WJ_Cone`, `WJ_Torus`, `WJ_Octahedron`, unit-box sized so v1 scales still fit, outward winding tested (`ProceduralMeshes.cs`) |
| F02 | Every primitive part kept its auto collider: characters had 14–17 colliders forming one compound body | High | All visual parts are collider-free under `VisualRoot`; one gameplay collider on the root (`PrefabBuilder.cs`) |
| F03 | Collectibles had no collider, so they couldn't be picked up | High | Trigger `SphereCollider` (1.15 × visual) + `Collectible` component with collect-once guard and host-authority gate (`Collectible.cs`) |
| F04 | Materials fell back to `Standard` (pink in URP) and were never repaired on re-run | Medium | URP-only chain (Lit → Simple Lit → Unlit); abort before writing anything if URP is missing; warn if no URP asset is assigned; repair broken shaders in place (same GUID); keep valid materials untouched (`MaterialLibrary.cs`) |
| F05 | Every run overwrote every prefab; manual edits were lost | Medium | `_GeneratorManifest.json` stores a file hash + definition fingerprint per asset: unchanged → skip, definition changed → rebuild in place, **hand-edited → skip + report**, Force Regenerate behind a confirmation (`GeneratorManifest.cs`, `WonderJumpAssetGenerator.cs`) |
| F06 | Generated assets and authored assets mixed in one folder | Medium | Generated = `Assets/WonderJumpGenerated` (read-only); authored = `Assets/WonderJumpContent`; generator only **adds** missing Prefab Variants there, never modifies them |
| F07 | Dynamic Rigidbody on characters (floaty, poor for netcode) | Medium | Kinematic Rigidbody (interpolated, speculative CCD, rotation + Z frozen), ready for the custom kinematic motor (D-02) |
| F08 | `<Id>_Sleeve` materials duplicated `<Id>_Outfit` (6 redundant) | Low | Sleeves reuse Outfit: 34 materials instead of 40 |
| F09 | No layers/tags | Low | 6 layers created idempotently (slots 8–13 preferred) and assigned per category (`WonderJumpProjectSetup.cs`, `WonderJumpLayers.cs`) |
| F10 | No assembly definitions | Low | `WonderJump.Runtime`, `.Editor`, `.Tests.EditMode`, `.Tests.PlayMode` |
| F11 | *(found in repair)* Character feet sank 0.18 below the origin (leg capsules extend past y=0) | Medium | `VisualRoot` lifted so the body's lowest point is y=0 (measured lift 0.180) |
| F12 | *(found)* v1 capsule (h 1.90, r 0.38) was a fixed guess, not derived from the body | Medium | Capsule from core body bounds (accessories excluded): h 2.062, r 0.322, **bottom exactly 0.000** for stable ground probing |
| F13 | *(found)* Enemies/bosses: visual and gameplay collision in one object, no Rigidbody (so no trigger events between kinematic bodies) | Medium | Root solid body (0.90 × visual) + child `Hitbox` trigger (1.00 × visual) + kinematic Rigidbody + `EnemyIdentity(id, world, isBoss)` |
| F14 | *(found)* `Get("M_x")` silently returned null for a missing material | Low | `BuildContext.Mat()` reports every missing material/mesh as an error |
| F15 | *(found)* Folder creation mixed `Directory.CreateDirectory` with `AssetDatabase` (folders unknown to the AssetDatabase) | Low | Recursive `AssetDatabase.CreateFolder` only |
| F16 | *(found)* Blocking dialog on every run (breaks tests/batch mode) | Low | Generation API returns a `GenerationReport`; dialogs only from menu items and never in batch mode |
| F17 | *(found)* No world organisation | Brief | `AssetCatalog` assigns every asset to one of 5 worlds or Shared; folders are category/world (see `AssetManifest.md`) |
| F18 | *(found)* Pivots inconsistent | Low | Feet (characters, enemies, props), top surface (platforms, hazards), centre (items) |

## 3. World assignment (Step 6)
| World | Assets |
|---|---|
| Green Forest | Forest_Ground, MushroomWalker, ForestBee, Boss_ForestGuardian, FantasyTree, TreeCanopy, MushroomProp |
| Candy Kingdom | Candy_Platform, CandyJumper, Boss_CandyKing |
| Ocean Adventure | Ocean_Platform, **OceanPuffer**, Boss_SeaMonster |
| Crystal Cave | Crystal_Platform, **CrystalCrawler**, **LavaSlime**, **Lava_Hazard**, Boss_CrystalGolem, CrystalCluster |
| Sky Castle | Sky_Platform, **Stone_Platform** (assumption: castle masonry; one-line change), **SkyDrone**, Boss_SkyOverlord |
| Shared | 6 characters, 6 items, Moving_Platform, WoodenCrate, PortalFrame |

No environments were built (as instructed).

## 4. Test results (this environment)
Command: `bash Tools/CompileCheck/run.sh` (Mono 6.8 `mcs`, API stubs, **not Unity**)

| Step | Result |
|---|---|
| Runtime assembly alone (no editor define) | Compiled, 0 errors |
| Runtime + Editor | Compiled, 0 errors |
| Runtime + PlayMode tests | Compiled, 0 errors |
| Runtime + Editor + EditMode tests | Compiled, 0 errors |
| Pure-logic EditMode tests executed | **17 passed, 0 failed** (ProceduralMeshes 4, AssetCatalog 7, RuntimeContracts 6) |
| Builder smoke: all 38 prefabs built with the real `PrefabBuilder`, then the validator's per-prefab checks | **PASS: 596 checks, 0 errors, 0 warnings** |
| GeneratorIntegrationTests (5), CollectiblePickupTests (3) | **Not run**: need the Unity AssetDatabase / physics |

Measured character collider (all 6): height 2.062, radius 0.322, bottom 0.000, visual lift 0.180.

What the stub pass does **not** prove: that Unity 6's API matches the signatures I used, how assets serialise, real physics trigger behaviour, rendering. One stub bug was found and fixed during the run (`Component.name` didn't mirror `GameObject.name` as Unity does). It was a stub issue, not a game-code issue.

## 5. Remaining issues and risks
| # | Issue | Impact | Plan |
|---|---|---|---|
| R1 | **Not compiled in Unity.** Calls most worth watching in the first compile: `PlayerSettings.SetScriptingBackend(NamedBuildTarget…)`, `PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26`, `SetGraphicsAPIs`, `GraphicsSettings.currentRenderPipeline`, `AssetDatabase.SaveAssetIfDirty`, `PrefabUtility.SaveAsPrefabAsset(…, out bool)`, and the test asmdefs' `nunit.framework.dll` reference | Possible compile errors | Owner pastes Console errors; fixed same day |
| R2 | Unity may re-serialise a prefab after saving, making its hash differ → false "modified" skip | Low | Hash taken after `SaveAssets`; `SecondRun_IsRepeatable` detects it; Force Regenerate is the escape hatch |
| R3 | When a base prefab is rebuilt, overrides on *child* objects of its variants may be lost | Medium | Rebuild only on definition change; QA-P0-13; prefer root-level variant changes |
| R4 | Placeholder characters use 13–16 renderers each | OK for graybox | Final art = 1 skinned mesh |
| R5 | Torus headphones / some accessories partly intersect the head | Cosmetic placeholder | Final art |
| R6 | Layer collision matrix not configured yet | None until the motor exists | Phase 1 task 1.09 |
| R7 | Android package name still default | Blocks Play upload, not debug builds | Q1 |
| R8 | Integration tests add the 6 layers to the project TagManager | Intended | Documented |
| R9 | Thin enemy variety (Candy/Ocean/Sky: 1 type) | Design | GDD |

## 6. Remaining blockers (owner)
1. Create the Unity 6 LTS **Universal 3D** project and attach this repo (README "First-time setup").
2. Install packages: Input System, Netcode for GameObjects, Multiplayer Services, Cinemachine, Multiplayer Play Mode.
3. Run QA-P0-01 → QA-P0-17 and the Test Runner (QA-P0-A01…A05); record results in `QA_TestPlan.md`.
4. Build the Bootstrap scene to a physical Android phone (QA-P0-D01…D03).
5. Answer Q1 (package name) and Q2 (co-op camera) before Phase 2.

## 7. Asset generation instructions (short)
1. Open the project (URP active). **Wonder Jump → Generate Complete 3D Asset Pack.**
2. **Wonder Jump → Validate Generated Assets** must say **PASS**.
3. Use prefabs from `Assets/WonderJumpContent/Prefabs/...` (variants) in scenes. Never edit `Assets/WonderJumpGenerated`.
4. To change an asset's definition, edit `AssetCatalog.cs` and run Generate (it rebuilds in place). To discard hand edits in generated files, use **Advanced → Force Regenerate**.
5. Commit `Assets/WonderJumpGenerated/**` including `_GeneratorManifest.json`, so every machine shares the same GUIDs and hashes.

## 8. Next recommended phase (after approval): Phase 1, Movement & Controls
Scope: Input actions; **Crystal Virtual Joystick** (purple/gold, floating/fixed, dead-zone, size setting); jump/action buttons; `KinematicMotor` implementing `IPlayerMotor` (run, accel/decel, variable jump, coyote, buffer, slopes, moving platforms) with `IGroundProbe`; layer collision matrix; follow camera; graybox test level from generated platforms; EditMode tests for the motor using a fake probe; device test at 60 FPS.
Not in Phase 1: netcode, enemies, levels, art.
