# TASKS — Wonder Jump – Adventure Worlds

Status: `TODO` · `DOING` · `REVIEW` (delivered by Claude, waiting for the owner to compile/test) · `DONE` (verified by the owner in Unity or on a device) · `BLOCKED`
Priority: **P0** blocks the MVP · **P1** needed for the MVP · **P2** later
Owner: **C** = Claude · **U** = owner

> A task involving Unity compilation, Play Mode, an Android build or multiplayer is only `DONE` after the owner actually ran it.

## Phase 0 — Foundation Repair & Technical Validation (current: REVIEW)
### Delivered by Claude
| ID | Pri | Task | Own | Status | Evidence |
|---|---|---|---|---|---|
| 0.01 | P0 | Audit asset pack | C | DONE | `AssetPackAudit.md` |
| 0.10 | P0 | Fix invalid `PrimitiveType` Cone/Torus/Octahedron → procedural meshes | C | REVIEW | F01; stub-compiled; mesh tests 4/4 (stub) |
| 0.11 | P0 | URP material creation: fallback chain, no pink, no duplicates, preserve/repair in place | C | REVIEW | F04, F08 |
| 0.12 | P0 | Colliders: character VisualRoot/collider split, item triggers + collect-once, enemy body + hitbox | C | REVIEW | F02, F03, F11–F13; builder smoke 596 checks PASS (stub) |
| 0.13 | P0 | Safe regeneration: manifest, skip hand edits, Prefab Variants in Content, category/world folders | C | REVIEW | F05, F06, F17 |
| 0.14 | P0 | World asset organisation (5 worlds + Shared) | C | REVIEW | `AssetManifest.md` |
| 0.15 | P0 | Gameplay architecture contracts (input, movement, state, animation, camera, enemy AI, collectibles, checkpoint, level, save, network) | C | REVIEW | `TechnicalBlueprint.md` §3 |
| 0.16 | P0 | Multiplayer foundation design + `GameAuthority` (offline default) | C | REVIEW | Blueprint §4 |
| 0.17 | P0 | Asmdefs, layers tool, Android settings tool, Bootstrap scene tool | C | REVIEW | F09, F10 |
| 0.18 | P0 | Validator menu (14 check groups) | C | REVIEW | `WonderJumpAssetValidator.cs` |
| 0.19 | P0 | Tests: 22 EditMode + 3 PlayMode | C | REVIEW | 17 pure tests pass on stub; 8 need Unity |
| 0.20 | P0 | Docs: CLAUDE, TASKS, TechnicalBlueprint, AssetManifest, QA_TestPlan, FoundationRepairReport, README | C | DONE | `Documentation/` |
| 0.21 | P1 | Out-of-Unity compile check (`Tools/CompileCheck`) | C | DONE | `run.sh` passes |
| 0.22 | P0 | Pin Unity 6000.3.27f1 + minimal `Packages/manifest.json` | C | REVIEW | D-09 |
| 0.23 | P0 | Build pipeline: Ensure URP, showcase scene, `WonderJumpBuild` (menu + CI entry) | C | REVIEW | stub-compiled |
| 0.24 | P0 | GitHub Actions workflow: Unity tests + Android APK artifact | C | REVIEW | D-10 |
| 0.25 | P1 | Coin stands upright (faces side camera) | C | REVIEW | BuildVersion 3 |

### Owner gate (Phase 0 is DONE only when all of these pass)
| ID | Pri | Task | Own | Status | Record |
|---|---|---|---|---|---|
| 0.29 | P0 | Add GitHub secrets UNITY_LICENSE / UNITY_EMAIL / UNITY_PASSWORD (README) → Claude triggers and monitors CI | U | TODO | |
| 0.30 | P0 | Install Unity 6000.3.27f1 + Android Build Support (for local work) | U | TODO | |
| 0.31 | P0 | Clone repo, open with Unity Hub (README), Ensure URP Pipeline | U | TODO | |
| 0.32 | P0 | QA-P0-01 compile: 0 errors (paste any errors to Claude) | U | TODO | |
| 0.33 | P0 | QA-P0-02…17 manual editor checks | U | TODO | |
| 0.34 | P0 | Test Runner: EditMode 22 + PlayMode 3 green | U | TODO | |
| 0.35 | P0 | Bootstrap APK on physical device, landscape, airplane mode (QA-P0-D01…03) | U | TODO | Device: `______` FPS: `___` |
| 0.36 | P0 | Commit `ProjectSettings/`, `Packages/`, `.meta` files, generated assets + manifest | U | TODO | |
| 0.37 | P1 | Answer Q1 (package name), Q2 (co-op camera) | U | DOING | Q2 answered 2026-11-01: per-phone follow (D-05). Q1 open |
| 0.38 | P1 | Owner approves Phase 0 → Phase 1 may start | U | TODO | |

## Phase 1 — Movement & Controls (starts after 0.38)
| ID | Pri | Task | Own |
|---|---|---|---|
| 1.01 | P0 | Input Actions asset (Move, Jump, Action, Pause) + `WonderJump.Controls.InputSystem` asmdef | C |
| 1.02 | P0 | Crystal Virtual Joystick: purple/gold ring + knob, floating/fixed, dead-zone, size from `SettingsData` | C |
| 1.03 | P0 | Jump + action buttons | C |
| 1.04 | P0 | `KinematicMotor : IPlayerMotor` (run, accel/decel, variable jump, coyote, buffer, slopes ≤ 45°, moving platforms) | C |
| 1.05 | P0 | `SphereCastGroundProbe : IGroundProbe` on the Ground layer | C |
| 1.06 | P0 | `PlayerStateMachine` + placeholder animation driver (squash/stretch) | C |
| 1.07 | P0 | Follow camera (Cinemachine), level bounds | C |
| 1.08 | P0 | Graybox test level from Content variants | C/U |
| 1.09 | P1 | Layer collision matrix (Player↔Ground/Enemy/Hazard/Collectible/Interactable) | C |
| 1.10 | P0 | EditMode motor tests with a fake probe (jump apex = designed height, coyote, buffer) | C |
| 1.11 | P0 | Device test: feel + 60 FPS | U |

## Phase 2 — Co-op Skeleton
| ID | Pri | Task | Own |
|---|---|---|---|
| 2.01 | P0 | Offline-mode spike: loopback host vs pure local (Blueprint §4.3) → decision | C/U |
| 2.02 | P0 | `WonderJump.Net` asmdef; `NgoSessionService : INetworkSessionService` (anonymous auth, Sessions + Relay, max 2) | C |
| 2.03 | P0 | Player NetworkObject, owner-authoritative transform, replicated `PlayerStateId` | C |
| 2.04 | P0 | Lobby UI: host, join code, errors | C |
| 2.05 | P0 | Disconnect / host-left handling → back to menu, `GameAuthority` reset | C |
| 2.06 | P0 | Per-phone follow camera (D-05): local player only, level bounds | C |
| 2.08 | P0 | Off-screen partner indicator (edge arrow + icon + distance) | C |
| 2.09 | P0 | Catch-up rule: host-decided teleport to a safe point (25 m / 3 s / 1.5 s i-frames, tunable) | C |
| 2.10 | P1 | Enemy activation around either player (host), so the client never meets frozen enemies | C |
| 2.07 | P0 | Test: Multiplayer Play Mode + 2 phones on different networks | U |

## Phase 3 — Characters
3.01 CharacterDefinition SO × 6 · 3.02 selection UI (SP + co-op synced) · 3.03 spawn chosen variant, sync `CharacterId` · 3.04 GDD Q5 abilities decision

## Phase 4 — Level Framework
4.01 Health/damage/i-frames/lives · 4.02 co-op revive (Q4) · 4.03 collectible network request + scoring · 4.04 hazards, kill plane, checkpoints (`ICheckpointService`), goal portal · 4.05 HUD · 4.06 `LevelService` + W1_L1 graybox

## Phase 5 — Enemies (Green Forest)
5.01 Enemy brains (`IEnemyBrain`), host-only tick · 5.02 MushroomWalker, ForestBee · 5.03 third Forest enemy (GDD) · 5.04 stomp/contact via `Hitbox`

## Phase 6 — Progression & MVP content
6.01 `JsonSaveService` (atomic, schemaVersion, corrupt fallback) · 6.02 level select/unlocks/stars · 6.03 W1_L2, W1_L3

## Phase 7 — Art pass (MVP)
7.01 URP toon shader + outline · 7.02 one-character pipeline spike (AI model → Blender → rig → Humanoid → 9 clips) · 7.03 remaining characters · 7.04 Forest environment + joystick art · 7.05 VFX + audio

## Phase 8 — Optimise & release candidate
8.01 Device profiling vs budgets · 8.02 ASTC/atlases/batching · 8.03 AAB + internal test track · 8.04 full QA pass on 2+ devices

## Backlog (P2)
Worlds 2–5 (22 levels) · 5 bosses · ~3 enemy types per world · cloud save · Addressables · accessibility · achievements · GDD.md (to create before Phase 3)
