# QA Test Plan — Wonder Jump – Adventure Worlds

Status: `NOT RUN` · `PASS` · `FAIL` · `BLOCKED`. Record date, build (`0.1.<run>`) and device model.
Only the owner can mark device or multiplayer tests PASS.

## 1. Verification levels
| Level | Meaning | Who |
|---|---|---|
| L1 | Inspected | Claude |
| L2 | Corrected | Claude |
| L3 | Typechecked + unit-tested (`npm run check`) | Claude / CI |
| L4 | Rendered in headless Chromium, screenshot reviewed | Claude |
| L5 | APK built in CI | CI |
| L6 | Runs on a physical Android device | Owner |

## 2. Device matrix
| Slot | Model | Android | RAM | Notes |
|---|---|---|---|---|
| D1 main | | | | |
| D2 co-op partner | | | | |
| D3 low-end | | | | |

## 3. Phase 0W — Web Foundation
| ID | Check | Level | Expected | Status |
|---|---|---|---|---|
| QA-W-01 | `packages/shared` tests (16) | L3 | All pass | **PASS** 2026-11-01 (local) |
| QA-W-02 | `apps/client` render tests (5): all chibis built, feet at y=0, shared geometry, pivots, upright coin | L3 | All pass | **PASS** 2026-11-01 (local) |
| QA-W-03 | Typecheck + production build | L3 | No errors | **PASS** 2026-11-01 (three ≈ 133 KB gzip) |
| QA-W-04 | Showcase at 915×412 @2x (phone landscape) | L4 | 6 characters with visible faces, items, enemies, portal, ground fills the lower screen, no console errors | **PASS** 2026-11-01 (headless, SwiftShader) |
| QA-W-05 | Tap Miyu | L4 | Miyu hops; card "Miyu · Little Witch · Crystal Staff" + tagline | **PASS** 2026-11-01 |
| QA-W-06 | Portrait viewport | L4 | Rotate overlay (TH + EN) | **PASS** 2026-11-01 |
| QA-W-07 | `ci.yml` on GitHub | L3 | Green | **PASS** 2026-11-01 (run 41289889671, 23 s) |
| QA-W-08 | `android-apk.yml`: artifact + Latest APK release | L5 | `WonderJump-latest.apk` in release `apk-latest` | **PASS** 2026-11-01: build 0.1.1, 5.0 MB, 2 m 36 s (run 41289889541) |
| QA-W-D01 | Install Latest APK on D1 | L6 | Installs, opens in **landscape fullscreen** | **PASS** 2026-11-01 (owner screenshot, viewport 832×384) |
| QA-W-D02 | Visual check on D1 | L6 | Faces visible, colours match ArtDirection, no black screen | **PASS** 2026-11-01 |
| QA-W-D03 | Performance on D1 | L6 | HUD FPS ≥ 55 (note the draw count) | **PASS** 2026-11-01: 120 FPS, 195 draws |
| QA-W-D04 | Tap each of the 6 characters | L6 | Hop + correct card each time | NOT RUN |
| QA-W-D05 | Airplane mode | L6 | App starts normally (no network needed) | NOT RUN |

## 4. Phase 1 — Movement & Controls
| ID | Check | Level | Expected | Status |
|---|---|---|---|---|
| QA-1-01 | Motor tests (21): apex = designed height ±5 %, variable height, single double jump, coyote, buffer, bonk, one-way, carry, kill plane, states, determinism | L3 | All pass | **PASS** 2026-11-01 |
| QA-1-02 | Level tests: playground valid; 4 m gap crossable with one jump; 5 m wall needs a double jump | L3 | All pass | **PASS** 2026-11-01 |
| QA-1-03 | Joystick maths (4) + animator drift regression | L3 | All pass | **PASS** 2026-11-01 |
| QA-1-04 | Headless: menu → tap Riku → ▶ Play | L4 | Level starts with Riku | **PASS** |
| QA-1-05 | Headless: pointer drag on the joystick | L4 | Player runs right (state Run) | **PASS** |
| QA-1-06 | Headless: Space, Space → DoubleJump visible in frame | L4 | Character fully on screen | **PASS** (after camera + animator fixes) |
| QA-1-07 | Headless: stand on the moving platform | L4 | Carried with the platform, grounded on m0 | **PASS** |
| QA-1-08 | Headless: J → Attack, reach portal → clear panel | L4 | Attack state; "ผ่านด่านทดสอบ!" panel with time/items/falls | **PASS** |
| QA-1-D01 | Device: joystick feels responsive; walk with a small tilt, run with a full tilt | L6 | Owner judgement | NOT RUN |
| QA-1-D02 | Device: jump over the gap, double jump onto the wall, ride the moving platform, reach the portal | L6 | Possible without frustration | NOT RUN |
| QA-1-D03 | Device: two thumbs at once (joystick + jump) | L6 | Both register | NOT RUN |
| QA-1-D04 | Device: FPS in the level | L6 | ≥ 55 | NOT RUN |

## 4a. Art insert — concept-art characters (D-14)
| ID | Check | Level | Expected | Status |
|---|---|---|---|---|
| QA-A-01 | Art files: 6 WebP, 512 px tall, aspect matches `CHARACTER_ART`, each < 150 KB; logo < 150 KB | L3 | Pass | **PASS** 2026-11-01 |
| QA-A-02 | Sprite chibi: feet at y = 0, catalog height, 1 mesh, silhouette depth material, hitbox unchanged | L3 | Pass | **PASS** 2026-11-01 |
| QA-A-03 | Mirror rule + `SpriteAnimator` returns to rest after 3000 frames of mixed states | L3 | Pass | **PASS** 2026-11-01 |
| QA-A-04 | Headless menu 915×412: logo, 6 concept-art characters, tap Koko → card with portrait | L4 | Visible, no console errors | **PASS** 2026-11-01 (62 draws vs 195) |
| QA-A-05 | Headless level: Koko run right / turn left (mirror), Miyu attack (spin + arc), double jump (cartwheel) | L4 | Correct facing; poses visible | **PASS** 2026-11-01 (first run showed the art drawn facing right → `artFacing` fixed to +1) |
| QA-A-06 | `?chibi=primitive` fallback | L4 | Primitive chibis, 195 draws | **PASS** 2026-11-01 |
| QA-A-07 | `android-apk.yml` on the art commit | L5 | APK published | **PASS** 2026-11-01: build 0.1.3 (run 3) |
| QA-A-D01 | Device: characters look like the concept art, readable size, turning/flip OK, FPS | L6 | Owner judgement | NOT RUN |

## 5. Later phases (outline)
| Phase | Themes |
|---|---|
| 1 | Joystick dead-zone/size, jump buffer/coyote, double jump height, one-way/moving platforms, 60 FPS |
| 2 | Join code, Wi-Fi + 4G pair, cold start, host/client leave, collect-once across players (server), latency 150 ms simulated |
| 4 | Checkpoints shared, hazards, revive, level end (Q7) |
| 6 | Save survives restart, corrupt save fallback |
| 8 | Memory, thermal 20-min run, battery, low-end device |

## 6. Unity Phase 0 history
Unity QA rows (generator, GameCI) are on branch `archive/unity-phase0`. Final state: GameCI licence activation **FAIL/BLOCKED** (`TimeStamp validation failed`). Unity code was never compiled in a real editor.
