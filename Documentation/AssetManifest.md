# Asset Manifest — placeholder assets (web stack)

Source of truth: `packages/shared/src/catalog.ts`. The client builds Three.js meshes from it (`apps/client/src/render`).
**All assets are primitive placeholders.** Final art follows `ArtDirection.md`.

## Conventions
- Units are metres. +X right, +Y up, **+Z toward the camera (characters face +Z)**.
- Unit primitives: sphere/box/octahedron 1×1×1; capsule and cylinder 1×2; cone 1×1; torus in the XY plane, outer diameter 1.
- Pivots: **feet** (characters, enemies, props: bottom at y=0) · **top** (platforms, hazards: top surface at y=0) · **center** (items).
- Every object = `root` (gameplay data) + `VisualRoot` (meshes only), so a GLB can replace the visual later.
- One geometry per shape and one toon material per colour are shared by all objects.

## Characters (6)
Built from 14 body parts (body, belt, head, face, eyes, highlights, blush, arms, legs) plus an accessory. Colours follow the concept sheets.
| ID | Accessory parts | Signature item |
|---|---|---|
| Leon | goggles band + lenses | Sword |
| Elin | bunny ears (+ pink inner) | Bunny bag |
| Miyu | witch hat (brim, cone, star) | Crystal staff |
| Koko | fox ears (cones) | Compass |
| Riku | headphones (torus + cups) | Wind wings |
| Flora | flower crown (5 flowers) | Flower wand |

The visual is lifted 0.18 m so the feet sit on y=0 (v1 legs sank below the ground). Gameplay hitbox: capsule height 2.062, radius 0.322, bottom at the feet.
**Fix vs v1:** the face, eyes and highlights were inside the hair sphere (invisible). They now sit in front of it.

## World assets
| World | Platforms | Hazards | Enemies | Boss | Props |
|---|---|---|---|---|---|
| Green Forest | Forest_Ground | – | MushroomWalker, ForestBee | Boss_ForestGuardian | FantasyTree, TreeCanopy, MushroomProp |
| Candy Kingdom | Candy_Platform | – | CandyJumper | Boss_CandyKing | – |
| Ocean Adventure | Ocean_Platform | – | OceanPuffer | Boss_SeaMonster | – |
| Crystal Cave | Crystal_Platform | Lava_Hazard | CrystalCrawler, LavaSlime | Boss_CrystalGolem | CrystalCluster |
| Sky Castle | Sky_Platform, Stone_Platform | – | SkyDrone | Boss_SkyOverlord | – |
| Shared | Moving_Platform | – | – | – | WoodenCrate, PortalFrame |

## Collectibles (values are placeholders until the GDD economy)
| Item | Value | Shape |
|---|---|---|
| Coin | 1 | cylinder, stood upright (X 90°) to face the camera |
| Star | 1 | flat sphere |
| Heart | 1 | flat sphere |
| CrystalGem | 10 | octahedron |
| Key | 1 | thin cylinder |
| PowerUp | 1 | sphere |

## Not included yet
GLB models, rigs, the 11 animation clips, textures, backgrounds/skybox, joystick art, UI art, VFX, audio, level data.
