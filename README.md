# Wonder Jump – Adventure Worlds

Cute 2.5D 3D-chibi platformer for **Android** (landscape): 6 characters, 5 worlds, 25 levels, single player and 2-player online co-op.
Unity 6 LTS · URP · C# · Netcode for GameObjects · Unity Multiplayer Services · Unity Relay · Input System.

**Status:** Phase 0 – Foundation Repair, waiting for owner review. See `Documentation/FoundationRepairReport.md`.

## Repository layout
```
Assets/WonderJump/            code (Runtime, Editor, Tests) – one asmdef each
Assets/WonderJumpGenerated/   generator output (created in Unity, read-only)
Assets/WonderJumpContent/     authored content: prefab variants, scenes, data
Documentation/                TASKS, TechnicalBlueprint, AssetManifest, QA_TestPlan, reports
Reference/AssetPack_v1/       original asset pack (archived, not compiled)
Tools/CompileCheck/           out-of-Unity compile check (Mono + API stubs)
```

## Unity version (pinned)
**Unity 6000.3.27f1 (Unity 6.3 LTS)** with Android Build Support, as pinned in `ProjectSettings/ProjectVersion.txt`.
- This version includes the fix for the Android security issue CVE-2025-59489 (fixed from 6000.0.58f2 onward). Don't build with older editors.
- A matching GameCI CI image exists (`unityci/editor:ubuntu-6000.3.27f1-android-3`).
- Upgrading Unity = change `ProjectVersion.txt` in its own commit and check that a CI image exists for the new version.

## First-time setup (owner, local Unity)
1. Unity Hub → Installs → install **6000.3.27f1** with **Android Build Support** (OpenJDK, Android SDK & NDK).
2. `git clone https://github.com/sawiteekaewkrad-gif/wonder-jump.git` (then `git lfs install`).
3. Unity Hub → **Add project from disk** → choose the cloned folder → open with 6000.3.27f1. Unity creates the missing project settings on first open.
4. Check the Console: **0 errors**.
5. **Wonder Jump → Setup → Ensure URP Pipeline** (creates and assigns a mobile URP asset if none is active).
6. Later phases add packages (Input System, Netcode for GameObjects, Multiplayer Services, Cinemachine, Multiplayer Play Mode); Phase 0 needs only what is in `Packages/manifest.json`.
7. Commit `ProjectSettings/`, `Packages/packages-lock.json` and the new `.meta` files.

## Building the APK on GitHub (no local Unity needed)
Workflow: `.github/workflows/android.yml` (GameCI). It runs on every push to `main` that touches the project, or manually (Actions → *Unity tests + Android APK* → Run workflow). Each run:
1. Checks the license secrets.
2. Compiles in real Unity and runs EditMode + PlayMode tests.
3. Builds the APK: URP → generate → validate → showcase scene → APK.

Download the APK from the run page → **Artifacts → WonderJump-Android-APK**, then install it on the phone (allow "install unknown apps").

**One-time setup: Unity Personal license secrets** (you must do this yourself; never paste passwords in chat):
1. Install Unity Hub, sign in, then go to **Preferences → Licenses → Add → Get a free personal license**. Do the "Add" step even if a license is already shown.
2. Find the license file `Unity_lic.ulf` (hidden folders):
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
3. GitHub repo → **Settings → Secrets and variables → Actions → New repository secret**, three times:
   - `UNITY_LICENSE` = the full contents of `Unity_lic.ulf`
   - `UNITY_EMAIL` = your Unity account email
   - `UNITY_PASSWORD` = your Unity account password
4. Actions → **Unity tests + Android APK** → **Run workflow**. The first run takes about 30–60 min (downloads the Unity image); later runs are faster thanks to the Library cache.

The APK uses Unity's debug signing and the placeholder package `com.wonderjump.adventureworlds.dev` until Q1 is answered (or set the env var `WJ_ANDROID_PACKAGE`). It is for device testing, not for Google Play.

## Generating the placeholder assets
Menu **Wonder Jump**:

| Menu item | What it does |
|---|---|
| `Setup/Ensure Layers` | Adds layers Player, Enemy, Ground, Collectible, Hazard, Interactable (also done automatically by Generate) |
| `Generate Complete 3D Asset Pack` | Creates/updates 34 materials, 3 procedural meshes and 38 prefabs in `Assets/WonderJumpGenerated`, plus missing Prefab Variants in `Assets/WonderJumpContent/Prefabs` |
| `Validate Generated Assets` | Static checks: shaders, colliders, triggers, layers, folder layout, manifest. Must report **PASS** |
| `Create Missing Content Variants` | Only adds variants that don't exist yet |
| `Advanced/Force Regenerate All Generated Assets` | Rebuilds everything in the generated folder, including hand-edited files (asks first). Never touches Content |
| `Setup/Apply Android Player Settings` | Landscape, IL2CPP, ARM64, Vulkan+GLES3, Linear, min API 26 |
| `Setup/Create Bootstrap Scene` | Diagnostics scene (FPS/version overlay), set as first build scene |
| `Setup/Ensure URP Pipeline` | Creates and assigns a mobile URP asset if none is active (never replaces an existing one) |
| `Build/Rebuild Phase 0 Showcase Scene` | All characters, items and enemies on platforms, for checking assets on a device |
| `Build/Build Android APK` | Full pipeline locally → `Builds/Android/WonderJump.apk` (the same code CI runs) |

**Regeneration is safe.** Each asset is tracked in `_GeneratorManifest.json`:
- Unchanged assets are skipped (no churn).
- Assets whose definition changed are rebuilt in place, keeping the same GUID.
- **Hand-edited generated assets are skipped and reported.**

Customise objects through the variants in `WonderJumpContent`, never the generated prefabs.

## Tests
- Unity: **Window → General → Test Runner** → run EditMode and PlayMode.
- Outside Unity (C# sanity only, not a substitute): `bash Tools/CompileCheck/run.sh`.
