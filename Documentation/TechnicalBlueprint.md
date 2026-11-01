# Wonder Jump – Adventure Worlds — Technical Blueprint

| Item | Value |
|---|---|
| Version | 0.3 (Phase 0 Foundation Repair; D-05 decided) |
| Date | 2026-11-01 |
| Status | Architecture contracts written; decisions D-02/D-03/D-04 adopted per Phase 0 brief; D-05 decided by owner 2026-11-01 |
| Related | `FoundationRepairReport.md`, `AssetManifest.md`, `TASKS.md`, `QA_TestPlan.md`, `AssetPackAudit.md` |

## 0. Requirements baseline
| # | Requirement | Notes |
|---|---|---|
| R1 | Android only | No iOS/Web builds |
| R2 | Unity 6 LTS + URP | Exact editor version recorded in `ProjectSettings/ProjectVersion.txt` once the project is created |
| R3 | 2.5D cute 3D chibi platformer | 3D models; gameplay on X/Y, Z locked |
| R4 | Landscape | Left + Right, auto-rotate between them |
| R5 | 6 selectable characters | Fixed IDs: Leon, Elin, Miyu, Koko, Riku, Flora |
| R6 | 5 worlds × 5 levels | MVP: 1 world, 3 levels |
| R7 | Purple-and-gold Crystal Virtual Joystick | On-screen control feeding the Input System |
| R8 | Single player + 2-player online co-op | Single player works **without any internet connection**; co-op in MVP |
| R9 | Mobile-optimised | Budgets in §6 |

**Assumptions:**

| ID | Assumption |
|---|---|
| A1 | Solo developer: Claude writes code and docs; the owner runs Unity, builds and devices |
| A2 | Co-op is online only (2 phones) |
| A3 | Minimum device: mid-range Android from about 2021 (4 GB RAM, Vulkan) |
| A4 | Unity Personal + Unity Gaming Services free tier |
| A5 | Characters are mostly cosmetic; gameplay differences are decided in the GDD |

## 1. Decisions
| ID | Decision | Status | Rationale / trade-off |
|---|---|---|---|
| D-01 | Folder ownership: Generated (read-only) / Content (authored, variants) / Code | **Adopted, implemented** | Regeneration can never destroy authored work. Cost: one extra variant per asset |
| D-02 | Player = **custom kinematic motor** (capsule casts + kinematic Rigidbody, Z frozen), not dynamic physics or CharacterController | **Adopted** | Tight platformer feel and deterministic enough for owner-authoritative netcode. Cost: more code in Phase 1. Unity's CharacterController was rejected: no Rigidbody (so no trigger callbacks unless one is added), and fixed up-axis assumptions complicate moving platforms |
| D-03 | Co-op = **host/client over Unity Relay** (NGO + Multiplayer Services Sessions). Player movement owner-authoritative; shared events host-authoritative | **Adopted** | No server cost; responsive local control. Cost: session ends if the host leaves (handled by a clear UX) |
| D-04 | **One gameplay code path** for single player and co-op, gated by `GameAuthority` | **Adopted** | Prevents the "retrofit netcode" rewrite. See §4.3 for how offline avoids any network dependency |
| D-05 | Co-op camera: **each phone follows its own player** (+ partner indicator + catch-up rule, §4.5) | **Decided by owner (2026-11-01)** | Each player always sees their own character at full size. Cost: players can drift apart, so the catch-up rule, an off-screen partner indicator, and host-side enemy activation around *both* players are required |
| D-06 | Each level is its own scene, loaded additively into `Gameplay`; co-op uses NGO's scene manager | Adopted | Clean memory per level; both peers load the same scene |
| D-07 | Data = ScriptableObjects; IDs = enums with fixed numeric values (`CharacterId`, `WorldId`), levels = `LevelId` "W{w}_L{l}" | **Adopted, implemented** | Safe for save files and network messages |
| D-08 | Local JSON save, `schemaVersion`, atomic write; per-player in co-op | Adopted (schema written) | Cloud save post-MVP |

## 2. Code layout (implemented in Phase 0)
| Assembly | Folder | Depends on | Contents |
|---|---|---|---|
| `WonderJump.Runtime` | `Assets/WonderJump/Runtime` | UnityEngine only (**no package dependencies yet**) | Contracts + small, real components |
| `WonderJump.Editor` | `Assets/WonderJump/Editor` | Runtime | Generator, validator, project setup |
| `WonderJump.Tests.EditMode` | `Assets/WonderJump/Tests/EditMode` | Runtime, Editor, NUnit | 22 tests (17 pure logic + 5 generator integration) |
| `WonderJump.Tests.PlayMode` | `Assets/WonderJump/Tests/PlayMode` | Runtime, NUnit | 3 physics tests |

Phase 1/2 will add `WonderJump.Net` (references NGO/UGS) and `WonderJump.Controls.InputSystem` (references the Input System). Runtime stays free of package references, so core gameplay always compiles.

## 3. Gameplay architecture (Step 7)
```
           ┌────────────── device ──────────────┐
 Touch ──► │ IPlayerInputSource  (Crystal Joystick│
           │   / keyboard / test script)        │
           └──────────────┬─────────────────────┘
                          │ PlayerInputFrame (plain struct)
                          ▼
                 IPlayerMotor.Simulate(input, dt)    ◄── PlayerMovementSettings (SO)
                   uses IGroundProbe (Ground layer)
                          │ MovementState (plain struct)
             ┌────────────┼──────────────────────┐
             ▼            ▼                      ▼
   Rigidbody.MovePosition  IPlayerStateMachine ──► IPlayerAnimationDriver
                          │ PlayerStateId (replicated)
                          ▼
                 Net adapter (Phase 2): owner sends transform + state;
                 remote peers render only (no input, no motor)
```

| Module | File(s) | Phase 0 deliverable | Implemented in |
|---|---|---|---|
| Player Input | `Runtime/Controls/PlayerInputContracts.cs` | `PlayerInputFrame`, `IPlayerInputSource` | P1: TouchInputSource + Crystal Joystick |
| Player Movement | `Runtime/Player/PlayerContracts.cs`, `PlayerMovementSettings.cs` | `IPlayerMotor`, `IGroundProbe`, `MovementState`, `GroundHit`; settings SO with derived gravity (g = 2h/t²) and jump velocity (v = 2h/t) | P1: KinematicMotor |
| Player State | `PlayerContracts.cs` | `PlayerStateId` (9 states), `IPlayerStateMachine` | P1 |
| Player Animation | `PlayerContracts.cs` | `IPlayerAnimationDriver` | P1 placeholder squash/stretch; P7 Humanoid |
| Camera | `Runtime/Camera/CameraContracts.cs` | `ICameraTargetGroup` (Cinemachine wrapper later) | P1/P2 |
| Enemy AI | `Runtime/Enemies/EnemyContracts.cs` | **`EnemyIdentity` (real, on prefabs)**, `IEnemyBrain`, `EnemyContext` | P5 |
| Collectibles | `Runtime/Gameplay/Collectible.cs` | **Real**: trigger pickup, collect-once guard, authority gate, respawn reset, `ICollector` | P4 adds scoring + network request |
| Checkpoint | `Runtime/Gameplay/CheckpointContracts.cs` | `ICheckpointService` (per-slot respawn) | P4 |
| Level Management | `Runtime/Level/LevelContracts.cs` | **`LevelId` (real, validated)**, `LevelResult`, `ILevelService` | P4/P6 |
| Save | `Runtime/Save/SaveContracts.cs` | `SaveData` schema v1, `ISaveStore`, `ISaveService` | P6 |
| Network | `Runtime/Net/GameAuthority.cs` | **`GameAuthority` (real)**, `SessionRole`, `INetworkSessionService` | P2 |
| Layers | `Runtime/Core/WonderJumpLayers.cs` | Player, Enemy, Ground, Collectible, Hazard, Interactable | — |
| Bootstrap | `Runtime/Core/BootstrapDiagnostics.cs` | 60 FPS target + version/FPS overlay | — |

## 4. Multiplayer foundation (Step 8)
### 4.1 Topology
2 players. One phone is **Host** (server + client); the other is **Client**. Transport: Unity Transport over **Relay**. Discovery: Multiplayer Services **Session** with a join code. Sign-in: UGS anonymous.

### 4.2 Authority table
| Data / event | Authority | Mechanism (Phase 2+) |
|---|---|---|
| Own player movement | **Owner** | Owner-authoritative NetworkTransform, interpolated on remote |
| Player animation state | Owner | `NetworkVariable<PlayerStateId>` (owner write) |
| Chosen character | Host validates | `NetworkVariable<CharacterId>` (server write) |
| HP, lives, KO/revive | **Host** | Server-write NetworkVariables |
| Collectible taken | **Host** | Client sends pickup request → host runs `TryCollect` (collect-once) → despawn + ClientRpc VFX |
| Enemy AI, damage, stomp | **Host** | Brains tick only where `HasSharedStateAuthority`; NetworkTransform from host |
| Checkpoint activation | **Host** | Host RPC broadcast; same respawn for both players |
| Level load / completion | **Host** | NGO NetworkSceneManager; host calls `ILevelService.Complete` |
| Save data | Each device | Local only, never networked |

In code today, `Collectible.OnTriggerEnter` already refuses to resolve pickups when `GameAuthority.Current.HasSharedStateAuthority` is false. This is covered by the PlayMode test `Client_DoesNotResolvePickupsLocally`.

### 4.3 Single player without internet
- `GameAuthority` defaults to **Offline** (`HasSharedStateAuthority = true`), so every system works with **no NetworkManager, no UGS sign-in, no Relay**. This is the state the game boots in.
- **Phase 2 decision point (spike):** whether offline also runs NGO as a loopback host (one prefab set, maximal code sharing) or as pure local objects (simplest, nothing networked).

| Option | Pros | Cons |
|---|---|---|
| Loopback host | Identical spawn path in single player and co-op | Starts a transport even offline |
| Pure local | Zero network code paths in single player | Needs a spawn abstraction so co-op and offline share prefabs |

Neither option ever contacts the internet. Gameplay code is written against the authority interface, so both remain possible.

### 4.4 Session flow (Phase 2)
```mermaid
sequenceDiagram
  participant H as Host phone
  participant UGS as Unity Services
  participant C as Client phone
  H->>UGS: Anonymous sign-in
  H->>UGS: Create session (max 2) + Relay allocation
  UGS-->>H: Join code
  H->>H: StartHost; GameAuthority = Host
  C->>UGS: Anonymous sign-in, join session(code)
  C->>H: Connect via Relay; GameAuthority = Client
  H-->>C: Approve, spawn player with CharacterId
  H->>C: Load W1_L1 (NetworkSceneManager)
  Note over H,C: Host leaves → client shown "Host left", returns to menu, GameAuthority = Offline
```

### 4.5 Co-op camera and separation (D-05)
| Rule | Design (default values are proposals, tunable in a config asset) | Authority |
|---|---|---|
| Camera | Each phone's Cinemachine camera follows **only the local player**, clamped to level bounds. The remote player is rendered normally whenever in view | Local |
| Partner indicator | When the partner is off-screen: an arrow on the screen edge with their character icon and distance (m) | Local |
| Catch-up (leash) | If the players are > **25 m** apart horizontally for > **3 s**, the trailing player is teleported to a safe grounded point near the leader, with **1.5 s** of invulnerability + VFX | **Host** decides and picks the safe point; the owner client applies `IPlayerMotor.Teleport` |
| Never teleport into danger | Safe point = last grounded position of the leader that is not a hazard, not over a gap, and not inside an enemy | Host |
| Enemy activation | Host simulates enemies within an activation radius of **either** player (not just the host's view); otherwise the client meets frozen enemies | Host |
| Checkpoints | Activated by either player; respawn for both moves forward (already in `ICheckpointService`) | Host |
| Level end | Open question Q7 | Host |

Contract impact: `ICameraTargetGroup` stays (single target per phone). Phase 2 adds `ICoopLeashService` and `IPartnerIndicator`; enemy brains get a "near any player" activation check.

## 5. Scene flow
`Bootstrap → MainMenu → (Single Player → CharacterSelect) | (Host/Join → Lobby → CharacterSelect) → Gameplay + Level (additive) → WorldMap`.
Phase 0 delivers `Bootstrap` only (menu: Setup/Create Bootstrap Scene).

## 6. Performance budgets (mobile)
| Metric | Target |
|---|---|
| Frame rate | 60 FPS on the A3 device; never below 30 on low-end |
| SetPass calls | ≤ 80 (SRP Batcher on; generator disables GPU instancing on materials accordingly) |
| Triangles on screen | ≤ 150k |
| Final character | ≤ 10k tris, 1 skinned mesh, 1 material, ≤ 30 bones |
| Textures | ASTC 6×6 (UI 4×4), ≤ 1024² per character |
| Lighting | 1 realtime directional light, baked GI/probes; tiny parts (eyes, highlights, flowers) don't cast shadows (already set by the generator) |
| Memory | ≤ 1 GB peak |
| Build | ≤ 150 MB AAB for MVP |
| Network | ≤ 10 KB/s per client at 30 Hz |

Placeholder characters are 13–16 renderers each (~7–9k tris from default primitives). Acceptable for graybox with 2 players; final art collapses to one skinned mesh.

## 7. Android configuration (`Wonder Jump/Setup/Apply Android Player Settings`)
| Setting | Value |
|---|---|
| Orientation | AutoRotation, Landscape Left + Right only |
| Scripting backend / arch | IL2CPP / ARM64 |
| Graphics APIs | Vulkan, OpenGLES3 |
| Color space | Linear |
| Min API / Target API | 26 / Auto (highest installed; confirm against Google Play's requirement at release) |
| Package name | **OPEN (Q1)**. The tool warns while it is still `DefaultCompany` |
| Keystore | Owner-created, never committed (.gitignore blocks `*.keystore`/`*.jks`) |

## 8. Phase plan
| Phase | Goal | Gate |
|---|---|---|
| **0. Foundation Repair** | Generator fixed, architecture contracts, tests, docs | Owner: Unity compile 0 errors, generator + validator PASS, Test Runner green, empty APK runs landscape on device |
| 1. Movement & controls | KinematicMotor, Crystal Joystick, camera, graybox level | 60 FPS on device; tuning checklist |
| 2. Co-op skeleton | Session service, Relay, 2 players moving; offline decision (§4.3) | 2 phones on different networks |
| 3. Characters | 6 definitions, selection, synced spawn | Remote sees the right character |
| 4. Level framework | Health, collectibles (network request), hazards, checkpoints, goal, HUD, W1_L1 | SP + co-op finish W1_L1 |
| 5. Enemies | World 1 brains, stomp, host-auth | Consistent on both phones |
| 6. Progression | Save, level select, W1_L2–3 | Persists across restarts |
| 7. Art pass | Toon shader, 1-character pipeline spike → rest, Forest art, joystick art, VFX, audio | Within §6 budgets |
| 8. Optimise & RC | Profiling, AAB, internal test track | QA plan passes on 2+ devices |

## 9. Risks
| Risk | L | I | Mitigation |
|---|---|---|---|
| Unity re-serialises generated files after save → false "modified" skips | Med | Low | Hash taken after `SaveAssets`; integration test `SecondRun_IsRepeatable` detects it; Force Regenerate as escape hatch |
| Overwriting a base prefab may reset variant overrides on child objects | Med | Med | Rebuild only when the definition changes; QA-P0-13 verifies; keep variant edits on root/added children |
| Phase 0 code never compiled in real Unity yet | Certain until owner runs it | Med | Stub compile + pure tests pass; owner compile is the Phase 0 gate |
| Netcode retrofit | Low (D-04) | High | Authority gate already in place |
| Chibi auto-rig failure (Mixamo) | Med | High | 1-character spike; Rigify fallback |
| Scope (25 levels solo) | High | High | MVP gate; data-driven levels |
| Thin enemy variety | High | Med | GDD: ~3 enemy types per world |

## 10. Open questions
| # | Question | Blocks |
|---|---|---|
| Q1 | Android package name | Device build signing / Play |
| Q2 | ~~Co-op camera~~ **Answered: per-phone follow (D-05)** | — |
| Q7 | Level end in co-op: complete when the first player reaches the goal, or when both do (with a countdown/teleport for the trailing player)? | Phase 4 |
| Q3 | Budget for AI 3D tools | Phase 7 |
| Q4 | Co-op death rules (shared lives / bubble revive / independent) | Phase 4 |
| Q5 | Do characters differ in gameplay? | Phase 3 |
| Q6 | Confirm Stone_Platform → Sky Castle | Low; one-line change |
