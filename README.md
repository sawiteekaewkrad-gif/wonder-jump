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

## First-time setup (owner, in Unity)
This repo holds the project's code and docs. The Unity project files (`ProjectSettings/`, `Packages/`) come from Unity's URP template.

1. **Unity Hub:** install the newest **Unity 6 LTS** with **Android Build Support** (OpenJDK, Android SDK & NDK tools).
2. **Create the project:** Hub → New project → **Universal 3D** template → create it in an empty folder, e.g. `WonderJump`. Close Unity.
3. **Attach this repo** (in that folder):
   ```bash
   git init
   git lfs install
   git remote add origin https://github.com/sawiteekaewkrad-gif/wonder-jump.git
   git fetch origin
   git checkout -b main --track origin/main   # repo files do not overlap the template's
   ```
4. **Open the project** in Unity and wait for compilation. **Check the Console for 0 errors.**
5. **Package Manager → Unity Registry:** install Input System, Netcode for GameObjects, Multiplayer Services, Cinemachine, Multiplayer Play Mode. (Test Framework ships with the template.) Accept the Input System restart prompt.
6. Commit `ProjectSettings/`, `Packages/` and the new `.meta` files.

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

**Regeneration is safe.** Each asset is tracked in `_GeneratorManifest.json`:
- Unchanged assets are skipped (no churn).
- Assets whose definition changed are rebuilt in place, keeping the same GUID.
- **Hand-edited generated assets are skipped and reported.**

Customise objects through the variants in `WonderJumpContent`, never the generated prefabs.

## Tests
- Unity: **Window → General → Test Runner** → run EditMode and PlayMode.
- Outside Unity (C# sanity only, not a substitute): `bash Tools/CompileCheck/run.sh`.
