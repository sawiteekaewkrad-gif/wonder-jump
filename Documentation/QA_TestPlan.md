# QA Test Plan — Wonder Jump – Adventure Worlds

Status values: `NOT RUN` · `PASS` · `FAIL` · `BLOCKED`. Record who ran each test, the date, the Unity version, and the device model where relevant.
**Only the owner (in Unity / on a device) can set PASS for Unity, Play Mode, Android or multiplayer tests.**

## 1. Verification levels
| Level | Meaning | Who |
|---|---|---|
| L1 Inspected | Code read and reviewed | Claude |
| L2 Corrected | Code changed | Claude |
| L3 Stub-compiled | `bash Tools/CompileCheck/run.sh` passes (Mono + API stubs, NOT Unity) | Claude / CI |
| L4 Unity-compiled | Unity 6 Console shows 0 compile errors | Owner |
| L5 Unity-tested | EditMode/PlayMode tests green in the Test Runner; manual editor checks | Owner |
| L6 Device-tested | Runs on a physical Android device | Owner |

## 2. Device matrix (fill in)
| Slot | Model | Android | SoC | RAM | Notes |
|---|---|---|---|---|---|
| D1 (main) | | | | | |
| D2 (co-op partner, Phase 2) | | | | | |
| D3 (low-end) | | | | | |

## 3. Phase 0 — Foundation Repair
### 3.1 Automated (Unity Test Runner)
| ID | Test | Level | Expected | Status |
|---|---|---|---|---|
| QA-P0-A01 | EditMode `ProceduralMeshesTests` (4) | L5 | All pass | NOT RUN (L3 stub run: 4/4 pass) |
| QA-P0-A02 | EditMode `AssetCatalogTests` (8) | L5 | All pass | NOT RUN (L3 stub run: 8/8 pass) |
| QA-P0-A03 | EditMode `RuntimeContractTests` (6) | L5 | All pass | NOT RUN (L3 stub run: 6/6 pass) |
| QA-P0-A04 | EditMode `GeneratorIntegrationTests` (5): first run + validator, repeatable second run, hand-edit skip/force, material repair, variants untouched | L5 | All pass. Ignored if URP is missing | NOT RUN (cannot run outside Unity) |
| QA-P0-A05 | PlayMode `CollectiblePickupTests` (3): collect once, two players same frame, client doesn't resolve | L5 | All pass | NOT RUN (cannot run outside Unity) |

### 3.2 Manual editor checks
| ID | Steps | Expected | Status |
|---|---|---|---|
| QA-P0-01 Compile | Open project, wait for import | Console: **0 errors**. Warnings listed in the report | NOT RUN |
| QA-P0-02 Menu | Open menu **Wonder Jump** | Items: Generate Complete 3D Asset Pack, Create Missing Content Variants, Validate Generated Assets, Setup/(Ensure Layers, Apply Android Player Settings, Create Bootstrap Scene), Advanced/Force Regenerate | NOT RUN |
| QA-P0-03 Layers | Setup/Ensure Layers → Project Settings > Tags and Layers | Player, Enemy, Ground, Collectible, Hazard, Interactable present. Existing layers unchanged | NOT RUN |
| QA-P0-04 Generate | Run Generate on a fresh project | Dialog: created 41 (38 prefabs + 3 meshes), 34 materials, 38 variants, 0 errors | NOT RUN |
| QA-P0-05 No pink | Open Characters/Leon, Items/CrystalGem, Props/Shared/PortalFrame, Characters/Miyu, Koko, Riku | All render coloured (no magenta). Witch hat, fox ears = cones; headphones, portal = torus; gem = octahedron | NOT RUN |
| QA-P0-06 Validate | Validate Generated Assets | **PASS**, 0 errors | NOT RUN |
| QA-P0-07 Character collider | Select Leon prefab, Scene view with Gizmos | One green capsule from the floor (y=0) to just below the head top; no colliders under VisualRoot; feet on y=0 | NOT RUN |
| QA-P0-08 Item trigger | Select Items/Coin | One SphereCollider, Is Trigger ticked, slightly larger than the coin; Collectible component (Coin, 1) | NOT RUN |
| QA-P0-09 Enemy hitboxes | Select Enemies/GreenForest/MushroomWalker | Root solid capsule inside the visual; child Hitbox trigger matching the visual; EnemyIdentity world = GreenForest | NOT RUN |
| QA-P0-10 Repeatable | Run Generate again | Created 0, regenerated 0, **up to date 41**, materials preserved 34, variants 0. No new files | NOT RUN |
| QA-P0-11 Hand-edit protection | Open generated Items/Coin, add an empty child, save. Run Generate | Console lists Coin under "Skipped: modified by hand"; the child is still there | NOT RUN |
| QA-P0-12 Material repair | Set M_Gold's shader to anything non-URP. Run Generate | "Materials repaired: 1"; M_Gold back on URP/Lit; references intact | NOT RUN |
| QA-P0-13 Variant safety | In Content/Prefabs/Characters/Leon (variant), change the root name or add a component; Force Regenerate | Variant change survives; variant still linked to its base | NOT RUN |
| QA-P0-14 No URP guard | (Optional, throwaway project without URP) Run Generate | Aborts with "No URP shader found"; **nothing written** | NOT RUN |
| QA-P0-15 World folders | Inspect `Prefabs/Enemies/CrystalCave` | Contains CrystalCrawler and LavaSlime; `Hazards/CrystalCave/Lava_Hazard`; `Platforms/SkyCastle/Stone_Platform` | NOT RUN |
| QA-P0-16 Play Mode readiness | Setup/Create Bootstrap Scene → Play | Enters Play Mode with no errors; overlay shows FPS and version | NOT RUN |
| QA-P0-17 Android settings | Setup/Apply Android Player Settings; inspect Player Settings | Landscape L+R only, IL2CPP, ARM64, Vulkan+GLES3, Linear, min API 26; warning about the package name until Q1 is answered | NOT RUN |

### 3.3 CI (GitHub Actions, real Unity)
| ID | Check | Expected | Status |
|---|---|---|---|
| QA-P0-C01 | Job `check-secrets` | Passes once the 3 secrets are set | NOT RUN |
| QA-P0-C02 | Job `tests`: compile + EditMode 23 + PlayMode 3 | All green; results in check "Unity test results" | NOT RUN |
| QA-P0-C03 | Job `android`: log steps 1/7…7/7, `Build Succeeded` | Artifact `WonderJump-Android-APK` contains `WonderJump.apk` | NOT RUN |

### 3.4 Device
| ID | Steps | Expected | Status |
|---|---|---|---|
| QA-P0-D01 | Install the CI APK (or Wonder Jump → Build → Build Android APK) on D1 | Installs and launches the showcase; **landscape only**; overlay ≈ 60 FPS; 6 characters, 6 items, 7 enemies visible, **no pink**, coin faces the camera | NOT RUN |
| QA-P0-D02 | Rotate the phone 180° | Switches between Landscape Left/Right | NOT RUN |
| QA-P0-D03 | Airplane mode on, launch | Launches normally (no network needed) | NOT RUN |

## 4. Later phases (outline, detailed when each phase starts)
| Phase | Key test themes |
|---|---|
| 1 | Joystick dead-zone/size, jump buffer & coyote timing, slopes, moving platforms, 60 FPS with graybox level |
| 2 | Host/join by code, different networks (Wi-Fi + 4G), host leaves, client leaves, reconnect, airplane-mode single player; per-phone camera follows only the local player; partner arrow appears off-screen; catch-up teleport after 25 m / 3 s never lands in a hazard or gap; enemies near the client are active |
| 3 | Selection sync, duplicate pick rules, remote sees the correct character |
| 4 | Collect-once across 2 players, checkpoints shared, hazards, KO/revive, level complete on both |
| 5 | Enemy state identical on both phones, stomp vs side hit |
| 6 | Save survives restart, corrupt save fallback, migration |
| 8 | Memory ≤ 1 GB, thermal 20-min run, battery, AAB install via internal test track |

## 5. Regression rule
Before every phase gate: run all EditMode + PlayMode tests, run `Tools/CompileCheck/run.sh`, run the generator twice (QA-P0-10), and do an Android smoke install (QA-P0-D01).
