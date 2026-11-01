# TASKS — Wonder Jump – Adventure Worlds

Status: `TODO` · `DOING` · `REVIEW` (delivered, waiting for owner check) · `DONE` · `BLOCKED` · `DROPPED`
Priority: **P0** blocks MVP · **P1** needed for MVP · **P2** later — Owner: **C** Claude · **U** owner

> A task involving the APK on a phone or multiplayer is only `DONE` after it actually ran there.

## History: Unity Phase 0 (archived on branch `archive/unity-phase0`)
Generator repair, contracts, tests and the GameCI pipeline were delivered. Unity CI licence activation was **BLOCKED** (game-ci#336), which led to the D-11 pivot. Unity-only tasks are **DROPPED**; design work was ported (see Phase 0W).

## Phase 0W — Web Foundation (current: REVIEW)
| ID | Pri | Task | Own | Status | Evidence |
|---|---|---|---|---|---|
| W.01 | P0 | Archive Unity code (branch `archive/unity-phase0`) | C | DONE | Branch pushed (tags are blocked by the proxy) |
| W.02 | P0 | TS monorepo (`packages/shared`, `apps/client`, `apps/mobile`) | C | DONE | `npm run check` green |
| W.03 | P0 | Port catalog, IDs, LevelId, authority, collect-once, movement maths, save schema to `shared` + tests | C | DONE | 16 tests pass |
| W.04 | P0 | Three.js chibi + asset builders (shared geometry, toon materials, pivots) + tests | C | DONE | 5 tests pass |
| W.05 | P0 | Showcase scene (6 chars, items, enemies, tap-to-hop, character card, FPS HUD, rotate overlay) | C | REVIEW | Headless Chromium screenshots reviewed |
| W.06 | P0 | Fix v1 defect: faces hidden inside the hair sphere | C | DONE | Screenshot |
| W.07 | P0 | Capacitor Android: landscape, fullscreen, CI version codes | C | REVIEW | Needs the CI APK |
| W.08 | P0 | CI: `ci.yml` + `android-apk.yml` (artifact + Latest APK release) | C | DONE | APK 0.1.1 published (5.0 MB) |
| W.09 | P0 | Docs: README, CLAUDE, Blueprint v1.0, ArtDirection, TASKS, QA, AssetManifest | C | DONE | |
| W.10 | P0 | **Install Latest APK on a phone; check landscape, fullscreen, faces, FPS, tap card** | U | TODO | Device + FPS: `____` |
| W.11 | P1 | Remove `NEON_API_KEY` secret if unused, or keep for Phase 2+ | U | TODO | |
| W.12 | P1 | Owner approves Phase 0W → Phase 1 | U | TODO | |

## Phase 1 — Movement & Controls
| ID | Pri | Task | Own |
|---|---|---|---|
| 1.01 | P0 | Level data schema (boxes/tiles, spawns, items, goal) + W1 test level JSON | C |
| 1.02 | P0 | `Motor.step` in shared: run/walk, accel, jump, **double jump**, coyote, buffer, variable height, one-way platforms, moving platforms | C |
| 1.03 | P0 | Collision vs level boxes (swept AABB/capsule) + tests | C |
| 1.04 | P0 | Crystal Virtual Joystick (purple/gold, floating/fixed, dead-zone, size) + Jump + Attack buttons → `InputFrame` | C |
| 1.05 | P0 | Follow camera (side view, look-ahead, level bounds) | C |
| 1.06 | P0 | Player state machine → placeholder animation (squash/stretch, lean) | C |
| 1.07 | P1 | Keyboard input for desktop testing | C |
| 1.08 | P0 | Device test: feel + 60 FPS | U |

## Phase 2 — Co-op Server (authoritative)
2.01 `apps/server` (Fastify + Socket.IO), rooms with join code (max 2) · 2.02 30 Hz authoritative tick running the shared motor · 2.03 Client prediction + reconciliation, partner interpolation · 2.04 Lobby UI + Render wake ping · 2.05 Disconnect handling · 2.06 Per-phone camera + partner indicator + catch-up (D-05) · 2.07 Deploy to Render (free), CI · 2.08 Test 2 phones on different networks (U)

## Phase 3 — Characters
3.01 Character select (offline + room-synced) · 3.02 Signature attacks per character (Q5) · 3.03 Portraits from concept art (cleaned)

## Phase 4 — Level framework
4.01 Hearts/damage/i-frames/lives · 4.02 Co-op revive (Q4) · 4.03 Collectibles via server `tryCollect` · 4.04 Hazards, checkpoints, goal portal, level end (Q7) · 4.05 HUD (hearts, coins, pause) · 4.06 W1_L1

## Phase 5 — Enemies (Green Forest)
5.01 Enemy AI in shared (server-run) · 5.02 MushroomWalker, ForestBee, +1 · 5.03 Stomp + Attack hits

## Phase 6 — Progression
6.01 Save adapter (Capacitor Preferences) · 6.02 Level select, stars · 6.03 W1_L2, W1_L3

## Phase 7 — Art pass
7.01 One-character GLB pipeline spike (ArtDirection §pipeline) · 7.02 Shared 11-clip animation set · 7.03 Remaining 5 characters · 7.04 Forest environment, logo redraw, joystick art · 7.05 VFX + audio

## Phase 8 — Optimise & RC
8.01 Device profiling vs budgets · 8.02 Release keystore + AAB · 8.03 QA pass on 2+ devices

## Open questions
Q1 package name · Q3 AI 3D budget · Q4 co-op death rules · Q5 character abilities · Q6 Stone_Platform world · Q7 co-op level end
