# Wonder Jump – Adventure Worlds — Technical Blueprint

| Item | Value |
|---|---|
| Version | 1.0 (web stack, after the D-11 pivot) |
| Date | 2026-11-01 |
| Status | Phase 0W delivered (showcase + APK pipeline). Earlier Unity blueprint v0.3 is on branch `archive/unity-phase0` |
| Related | `TASKS.md`, `QA_TestPlan.md`, `ArtDirection.md`, `AssetManifest.md`, `FoundationRepairReport.md` (Unity history) |

## 0. Requirements baseline
| # | Requirement | Web-stack note |
|---|---|---|
| R1 | Android only | APK via Capacitor; the browser build is a dev convenience |
| R2 | ~~Unity 6 LTS + URP~~ → **Three.js + TypeScript** | Changed by owner 2026-11-01 (D-11) |
| R3 | 2.5D cute 3D chibi platformer | Real-time 3D, side camera, toon shading |
| R4 | Landscape | `sensorLandscape` in AndroidManifest + rotate overlay in the browser |
| R5 | 6 selectable characters | Fixed IDs Leon…Flora |
| R6 | 5 worlds × 5 levels | MVP: 1 world, 3 levels |
| R7 | Purple-gold Crystal Virtual Joystick | DOM/Canvas overlay → `InputFrame` (Phase 1) |
| R8 | Single player + 2-player online co-op | Offline needs no network; co-op is **server-authoritative** (D-03r) |
| R9 | Mobile performance | Budgets in §6 |

## 1. Decisions
| ID | Decision | Status | Rationale / trade-off |
|---|---|---|---|
| **D-11** | **Replace Unity with a TypeScript web stack (Three.js + Capacitor), modelled on Happy Farm Online** | **Adopted 2026-11-01 (owner)** | Unity CI builds were blocked: GameCI Personal licence activation fails with `TimeStamp validation failed` (game-ci#336), and Personal licences expire monthly. The web stack needs no licence, builds an APK in ~10 min, and Claude can typecheck, test and render it directly. Cost: lower 3D headroom than native, the co-op server is ours to run, no visual level editor, and Unity Phase 0 tooling is discarded (kept on the archive branch) |
| D-12 | Renderer = **Three.js** (over Babylon.js and Phaser 2D) | Adopted (owner had no preference) | Skeletal animation means 11 clips are made once and shared by 6 characters (sprites would need 66 rendered sets). The primitive placeholders ported directly. Small, mature, works well with Vite/TS. Cost: we write our own platformer physics (planned anyway, D-02) |
| D-02 | Custom kinematic platformer motor (AABB/capsule vs tile/box colliders), in `packages/shared` | Adopted | Deterministic and reusable on the server. No physics engine needed for a side-scroller |
| **D-03r** | Co-op = **server-authoritative** rooms: Node + Socket.IO on Render (free tier, Singapore) | **Adopted 2026-11-01 (owner)** | Cheat-resistant, with one source of truth. TypeScript means the server runs the same `packages/shared` rules. Cost: server CPU per room; Render free tier sleeps (~50 s cold start). Mitigations: wake-up ping on the lobby screen; paid instance before public launch |
| D-04 | One rules code path for offline / server / client roles (`SimRole`) | Adopted, implemented | Offline = this device is the authority; online = server is the authority, clients predict their own movement |
| D-05 | Each phone's camera follows its own player + partner indicator + catch-up teleport | Adopted (owner) | Unchanged from Unity design; server decides catch-up |
| D-06 | Levels are data (JSON tile/box layouts) loaded by the client and the server | Adopted | Same file drives rendering and authoritative collision |
| D-07 | IDs: `CharacterId` 1–6, `WorldId` 1–5, `LevelId` `W{w}_L{l}` | Adopted, implemented | Stable for saves and network |
| D-08 | Save = local JSON (Capacitor Preferences / localStorage), `schemaVersion`, sanitising loader | Adopted (schema + loader implemented) | Cloud save (Neon) is post-MVP and optional |
| **D-14** | **Interim character visuals = owner's concept art as 2.5D paper cut-outs** (textured plane + procedural animation); primitive chibi kept as `?chibi=primitive` | Adopted 2026-11-01 (owner: "put the characters in like this") | Looks like the concept art today, 1 draw per character, ~45 KB each. Cost: one pose for all states, no side view; replaced by GLB in Phase 7 behind the same `visual` contract |
| D-13 | Persistence for accounts/leaderboards: **Neon Postgres via the server only** (never from the app) | Proposed, post-MVP | Owner already has a Neon project `wonder_jump`. DB credentials must never ship in the APK |
| Q7 | Co-op level end: first player at the goal vs both | OPEN | Phase 4 |

## 2. Repository layout
| Package | Role | Depends on |
|---|---|---|
| `packages/shared` | Rules + data: ids, catalog, authority, collectibles, movement maths, save schema. No DOM/Three | — |
| `apps/client` | Three.js renderer, input, UI, scenes | shared, three |
| `apps/mobile` | Capacitor Android shell (landscape, fullscreen, committed `android/`) | client build |
| `apps/server` *(Phase 2)* | Fastify + Socket.IO rooms, authoritative tick | shared |

## 3. Runtime architecture
```
InputFrame (joystick/keys/test) ─► Motor.step(state, input, dt, level)   [shared, pure]
                                        │
                     offline: local authority ─┬─► Render (Three.js) + HUD
                                               │
           online: client predicts own player ─┘   server: authoritative tick 30 Hz
                     ▲          │ inputs                 │ snapshots (players, enemies, items)
                     └──────────┴──── Socket.IO ─────────┘
```
| Module | Location | Phase 0W | Next |
|---|---|---|---|
| IDs / catalog | `shared/ids.ts`, `shared/catalog.ts` | ✅ ported + tests | Level data schema (P1) |
| Authority | `shared/authority.ts` | ✅ `SimRole`, `hasSharedAuthority` | Server rooms (P2) |
| Collectibles | `shared/collectibles.ts` | ✅ collect-once, authority-gated, tested | Network request path (P2/P4) |
| Movement | `shared/player.ts`, `shared/motor.ts` | ✅ settings + maths; **P1: deterministic fixed-step motor (60 Hz) with AABB collision, one-way, moving-platform carry, coyote/buffer, double jump, attack** | Server reuse (P2) |
| Save | `shared/save.ts` | ✅ schema + sanitising loader | Storage adapter (P6) |
| Rendering | `client/render/*` | ✅ shared geometries, toon materials, chibi + asset builders; **D-14: `spriteChibi.ts` + `SpriteAnimator` (concept-art cut-outs)** | GLB loader + AnimationMixer (P7) |
| Scenes | `client/scenes/showcase.ts`, `client/scenes/level.ts` | ✅ showcase = menu; **P1: level scene, interpolated rendering, keep-in-frame camera, pickups (offline authority), course clear** | Co-op scene (P2) |
| Input | `client/input/controls.ts` | **P1: Crystal Joystick (floating, dead-zone), Jump/Attack buttons, keyboard → `InputFrame`** | Settings UI (P6) |
| Network | — | — | `apps/server` + client netcode (P2) |

## 4. Co-op (Phase 2 design)
- **Room:** host creates → 6-character join code → max 2 players. Server keeps the level state.
- **Tick:** server at 30 Hz. Clients send inputs (seq-numbered); server simulates with `packages/shared`, broadcasts snapshots; clients predict own movement and reconcile, and interpolate the partner.
- **Authority:** server decides collectibles (`tryCollect(..., 'server')`), enemy AI and damage, checkpoints, catch-up teleport, level end.
- **Offline:** the same simulation runs locally with role `offline`. No socket is opened.
- **Hosting:** Render free web service (Singapore) for testing, plus an optional Neon DB for accounts later. The lobby pings `/health` early to hide the cold start.

## 5. Android packaging
| Item | Value |
|---|---|
| Shell | Capacitor 8, `webDir = apps/client/dist`, `androidScheme https` |
| Orientation | `android:screenOrientation="sensorLandscape"` |
| Fullscreen | `MainActivity` hides system bars (swipe shows them briefly) |
| SDK | min 24, target/compile 36 (Capacitor defaults) |
| Version | `versionCode = run number`, `versionName = 0.1.<run>` (CI env) |
| Package | `com.wonderjump.adventureworlds.dev` (placeholder until Q1) |
| Signing | Debug key (test builds). Release keystore before any store upload |

## 6. Performance budgets (mobile WebView)
| Metric | Target |
|---|---|
| Frame rate | 60 FPS mid-range (A3), never below 30 low-end |
| Draw calls | ≤ 150 (showcase: 62 with concept-art cut-outs, 195 with primitive chibis; final GLB = 1 draw per character) |
| Pixel ratio | ≤ 2 |
| Shadows | 1 directional, 1024² map; only characters/enemies cast |
| Character GLB | ≤ 8k tris, ≤ 30 bones, 1 toon material, ≤ 1.5 MB |
| JS bundle | three ≈ 133 KB gzip + game; total APK ≤ 40 MB for MVP |
| Network | ≤ 10 KB/s per client |

## 7. Risks
| Risk | L | I | Mitigation |
|---|---|---|---|
| WebView 3D performance on low-end phones | Med | High | Budgets, merge placeholder meshes, quality toggle, device tests every phase |
| Server-authoritative co-op complexity (prediction/reconciliation) | Med | High | Shared deterministic rules; start with simple snapshot interpolation; Phase 2 spike |
| Render free tier cold start / limits | High | Med | Wake ping; paid instance for launch |
| AI 3D pipeline quality (chibi rigging) | Med | High | One-character spike first (Phase 7) |
| Scope (25 levels solo) | High | High | Data-driven levels, MVP gate |
| Concept-art inconsistencies | Certain | Low | `design-reference-6chars.jpg` canonical; redo logo |

## 8. Open questions
| # | Question | Blocks |
|---|---|---|
| Q1 | Android package name | Release signing / Play |
| Q3 | Budget for AI 3D tools | Phase 7 |
| Q4 | Co-op death rules | Phase 4 |
| Q5 | Characters differ in gameplay (attack types from signature items)? | Phase 3 |
| Q6 | Confirm Stone_Platform → Sky Castle | Low |
| Q7 | Co-op level end rule | Phase 4 |
