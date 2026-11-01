# Wonder Jump 3D Asset Pack — Android Starter

This is a Unity editor-based procedural 3D asset generator. It creates reusable Unity prefabs and materials from primitive stylized meshes.

## Requirements
- Unity 6 LTS (URP recommended)
- Android Build Support installed through Unity Hub
- A new or existing Unity project

## Install
1. Extract this ZIP.
2. Copy `Assets/WonderJump` into your Unity project's `Assets` folder.
3. Wait for Unity to compile.
4. In Unity, select **Wonder Jump → Generate Complete 3D Asset Pack**.
5. Generated assets appear under `Assets/WonderJumpGenerated/`.

## Included generated assets
- Six selectable character concept prefabs: Leon, Elin, Miyu, Koko, Riku, Flora
- Seven enemy placeholder prefabs
- Five boss placeholder prefabs
- Collectibles: Coin, Star, Heart, CrystalGem, Key, PowerUp
- World platforms and environment props
- Reusable materials

## Important limitations
This pack generates stylized primitive-based 3D placeholders inside Unity. It does NOT contain finished sculpted FBX/GLB character models, production-ready humanoid rigs, animation clips, polished hand-painted textures, complete level scenes, or a tested Android APK. The generated characters have basic colliders and rigidbodies, but require proper rigging, animation, gameplay scripts, and balancing before release.

The generator intentionally avoids external asset dependencies and can run offline after Unity imports the scripts.

## Android setup
- Set Player Settings → Resolution and Presentation → Default Orientation to Landscape Left or Landscape Right.
- Switch Build Settings to Android.
- Use ARM64 for release builds.
- Test on a physical Android device.
