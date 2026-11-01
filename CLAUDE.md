# Wonder Jump – Adventure Worlds: working rules

Read first: `README.md`, `Documentation/TASKS.md`, `Documentation/TechnicalBlueprint.md`, `Documentation/ArtDirection.md`.

## Target (fixed)
- Android only, landscape (Left + Right), fullscreen. 2.5D cute 3D chibi platformer; gameplay on the X/Y plane, Z is depth only.
- **Stack:** TypeScript monorepo, Three.js, Vite, Capacitor (Android), GitHub Actions APK. Node 22+. This was decided 2026-11-01 (D-11), replacing Unity, which is archived on branch `archive/unity-phase0`.
- Single player must work with **no internet**. 2-player online co-op is **server-authoritative** (Node + Socket.IO, Phase 2).
- 6 characters with fixed IDs **Leon, Elin, Miyu, Koko, Riku, Flora** (`CharacterId` 1–6, never renumber).
- 5 worlds × 5 levels (`WorldId` 1–5); levels are named `W{world}_L{level}`.

## Code rules
- Game rules and data live in `packages/shared` (no DOM, no Three.js). Client and server import the same code.
- `apps/client` renders and handles input only. Movement takes an `InputFrame`, never raw touch events.
- Shared outcomes (collectibles, enemies/damage, checkpoints, level end) are decided only where `hasSharedAuthority(role)` is true (`offline` or `server`).
- Asset definitions live ONLY in `packages/shared/src/catalog.ts`.
- Mobile first: shared geometries/materials (`render/primitives.ts`), pixel ratio ≤ 2, few lights, avoid per-frame allocations.
- Strict TypeScript; every rule change comes with a vitest test.

## Verification honesty (mandatory)
State the level each claim reached; never claim a higher one:
1. **Inspected** · 2. **Corrected** · 3. **Typechecked + unit-tested** (`npm run check`)
4. **Rendered in headless Chromium** (screenshot reviewed) · 5. **APK built in CI** · 6. **Android device tested** (model recorded)

Never claim an APK runs on a phone, or that multiplayer works, unless that was actually done. Record results in `Documentation/QA_TestPlan.md`.

## Phase gate
Small phases. Don't start a new phase until the owner approves the previous one (`Documentation/TASKS.md`).
