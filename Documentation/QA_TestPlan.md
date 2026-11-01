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
| QA-W-D01 | Install Latest APK on D1 | L6 | Installs, opens in **landscape fullscreen**; rotating 180° flips; portrait stays landscape | NOT RUN |
| QA-W-D02 | Visual check on D1 | L6 | Faces visible, colours match ArtDirection, no black screen | NOT RUN |
| QA-W-D03 | Performance on D1 | L6 | HUD FPS ≥ 55 (note the draw count) | NOT RUN |
| QA-W-D04 | Tap each of the 6 characters | L6 | Hop + correct card each time | NOT RUN |
| QA-W-D05 | Airplane mode | L6 | App starts normally (no network needed) | NOT RUN |

## 4. Later phases (outline)
| Phase | Themes |
|---|---|
| 1 | Joystick dead-zone/size, jump buffer/coyote, double jump height, one-way/moving platforms, 60 FPS |
| 2 | Join code, Wi-Fi + 4G pair, cold start, host/client leave, collect-once across players (server), latency 150 ms simulated |
| 4 | Checkpoints shared, hazards, revive, level end (Q7) |
| 6 | Save survives restart, corrupt save fallback |
| 8 | Memory, thermal 20-min run, battery, low-end device |

## 5. Unity Phase 0 history
Unity QA rows (generator, GameCI) are on branch `archive/unity-phase0`. Final state: GameCI licence activation **FAIL/BLOCKED** (`TimeStamp validation failed`). Unity code was never compiled in a real editor.
