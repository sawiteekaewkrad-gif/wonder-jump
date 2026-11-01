# Wonder Jump – Adventure Worlds

Cute 2.5D 3D-chibi platformer for **Android** (landscape): 6 characters, 5 worlds, 25 levels, single player and 2-player online co-op.

**Stack (since 2026-11-01):** TypeScript monorepo · **Three.js** (3D, cel-shaded) · Vite · **Capacitor** (Android APK) · GitHub Actions.
Co-op (Phase 2): Node + Socket.IO **authoritative server** on Render, with optional Postgres on Neon. This is the same pattern as Happy Farm Online.
The earlier Unity 6 work is preserved on branch [`archive/unity-phase0`](../../tree/archive/unity-phase0).

## 📱 Install the latest APK
On an Android phone, open **[Releases → "Latest APK"](../../releases/tag/apk-latest)** and tap `WonderJump-latest.apk`. Allow "install unknown apps" when asked.
Every push to `main` rebuilds it automatically. No licence or local tools are needed.

## Repository layout
```
packages/shared/   engine-free rules + data (IDs, asset catalog, collect-once, movement maths, save schema)
                   → the same code will run on the authoritative co-op server
apps/client/       Three.js game (Vite). Phase 0W: asset showcase scene
apps/mobile/       Capacitor Android shell (landscape, fullscreen); android/ is committed
Documentation/     TechnicalBlueprint, TASKS, QA_TestPlan, ArtDirection, AssetManifest, reports
Documentation/Art/ concept model sheets (art direction reference)
```

## Develop
```bash
npm install          # Node 22+
npm run dev          # http://localhost:5173 (use your phone on the same Wi-Fi: http://<pc-ip>:5173)
npm run check        # typecheck + tests + build (same as CI)
npm run mobile:sync  # build web + copy into apps/mobile/android
```
To build an APK locally, you need Android Studio / JDK 21: `cd apps/mobile/android && ./gradlew assembleDebug`.

## CI
| Workflow | Trigger | Does |
|---|---|---|
| `ci.yml` | push / PR | `npm ci` → typecheck → tests → build |
| `android-apk.yml` | push to main / manual | tests → web build → Capacitor sync → Gradle debug APK → artifact + **Latest APK** release |

The APK is a debug build signed with the debug key, with placeholder package `com.wonderjump.adventureworlds.dev` until Q1 (package name) is answered. It is for testing only, not for Google Play.
