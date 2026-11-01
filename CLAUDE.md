# Wonder Jump – Adventure Worlds: working rules

Read first: `README.md`, `Documentation/TASKS.md`, `Documentation/TechnicalBlueprint.md`, `Documentation/AssetManifest.md`.

## Target (fixed)
- Android only, landscape (Left + Right). **Unity 6000.3.27f1 (6.3 LTS)**, pinned in `ProjectSettings/ProjectVersion.txt`, URP, C#. Never build with an editor older than the CVE-2025-59489 fix.
- CI: `.github/workflows/android.yml` (GameCI) runs Unity tests + builds the APK. A CI run with real Unity is verification level 4/5 for the tested code.
- 2.5D cute 3D chibi platformer. Gameplay runs on the X/Y plane; Z is locked.
- Single player (must work with no internet) + 2-player online co-op (NGO + Unity Multiplayer Services + Relay).
- 6 characters with fixed IDs: **Leon, Elin, Miyu, Koko, Riku, Flora** (`CharacterId` enum, values 1–6, never renumber).
- 5 worlds × 5 levels: GreenForest, CandyKingdom, OceanAdventure, CrystalCave, SkyCastle (`WorldId` 1–5).

## Folder ownership
| Folder | Owner | Rule |
|---|---|---|
| `Assets/WonderJump/` | Code | Runtime, Editor, Tests; one asmdef each |
| `Assets/WonderJumpGenerated/` | Generator | Read-only. Never hand-edit; regenerated from `AssetCatalog.cs` |
| `Assets/WonderJumpContent/` | Authored | Prefab Variants, scenes, ScriptableObject data, final art. Generator only ADDS missing variants |
| `Reference/AssetPack_v1/` | Archive | Original pack, kept for traceability. Not compiled |
| `Tools/CompileCheck/` | CI helper | Out-of-Unity compile check with API stubs |

## Code rules
- Movement never reads input devices or netcode directly: `IPlayerInputSource → PlayerInputFrame → IPlayerMotor`.
- Shared outcomes (collectibles, enemy AI/damage, checkpoints, level end) are decided only where `GameAuthority.Current.HasSharedStateAuthority` is true (Offline or Host).
- Gameplay depends on interfaces in `Runtime/*Contracts.cs`, not on NGO/UGS types. Netcode lives in `Runtime/Net`.
- Prefab layout: Root (gameplay components + colliders) / `VisualRoot` (meshes only, no colliders) / `Hitbox` (enemy trigger).
- Asset definitions live ONLY in `Editor/Generation/AssetCatalog.cs`. Changing build logic → bump `PrefabBuilder.BuildVersion`; changing mesh builders → bump `ProceduralMeshes.Version`.
- Write C# that also compiles with Mono `mcs` (C# 7.x): no records, `init`, switch expressions or target-typed `new`.
- Mobile first: URP materials only, SRP Batcher, no per-frame allocations in gameplay loops.

## Verification honesty (mandatory)
Always state which level each claim reached, and never claim a higher one:
1. **Inspected** (read)
2. **Corrected** (edited)
3. **Stub-compiled** (`bash Tools/CompileCheck/run.sh`, which is NOT Unity)
4. **Unity-compiled** (0 errors in the Unity 6 Console)
5. **EditMode/PlayMode tested** (Unity Test Runner)
6. **Android device tested** (physical device, model recorded)

Never claim an Android build or multiplayer works unless it was actually built and run. Record results in `Documentation/QA_TestPlan.md`.

## Phase gate
Work in small phases. Do not start a new phase until the previous one is reviewed and approved by the owner (see `Documentation/TASKS.md`).
