# Art Direction — Wonder Jump – Adventure Worlds

Source: owner-supplied concept model sheets (AI-generated), stored in `Documentation/Art/concepts/`:

| File | Content |
|---|---|
| `design-reference-6chars.jpg` | All 6 characters: front/side/back, expressions, action poses |
| `model-sheet-5chars-a.jpg`, `model-sheet-5chars-b.jpg` | Turnarounds, palettes, accessories, 11 animation key poses (Leon missing) |
| `leon-full-sheet.jpg` | Leon: turnaround, close-ups, expressions, sprite rows, in-game mock, props, model spec |

## Style
Cute 3D chibi (head ≈ 45% of height), big glossy eyes, soft cel shading, saturated fantasy palette, chunky boots. Bright sky, castle on a floating island, flower-filled meadows.

## Characters (canonical; colours used by the placeholders in `catalog.ts`)
| ID | Title | Hair | Outfit | Accessory | Signature item | Tagline (TH) |
|---|---|---|---|---|---|---|
| Leon | Adventurer | brown | blue cape + white shirt, brown belt | goggles | Sword | ไม่ว่าทางจะยากแค่ไหน ฉันก็จะไปให้ถึง! |
| Elin | Bunny Explorer | pink twin braids | pink/cream coat | bunny ears + goggles | Bunny bag | นักสำรวจน้อยผู้สดใส |
| Miyu | Little Witch | lavender | purple dress, gold trim | witch hat (star) | Crystal staff, spell book | แม่มดน้อยผู้รักเวทมนตร์ |
| Koko | Fox Scout | orange | brown vest, green scarf | fox ears + goggles, tail | Compass, backpack | นักสำรวจจิ้งจอกแสนซุกซน |
| Riku | Sky Runner | silver-lavender | navy/white jacket, cyan glow | headphones | Wind wings / energy | นักวิ่งแห่งท้องฟ้า |
| Flora | Flower Fairy | blonde | leaf dress, fairy wings | flower crown | Flower wand | ภูตดอกไม้ผู้เชื่อมโยงธรรมชาติ |

## Gameplay facts revealed by the sheets (feed the GDD)
- **Animation set (11):** Idle, Walk, Run, Jump, **Double Jump**, Fall, Land, **Attack**, Hurt, Death, Victory. This is reflected in `PlayerState`.
- **Double jump** and an **attack button** exist. HUD mock: purple-gold crystal joystick (left), Jump + Attack buttons (right), 3 hearts, coin counter, pause.
- **Expressions (6–8):** Normal, Happy, Laugh, Wink, Surprised, Sad, Angry/Determined. These map to UI portraits and emotes.

## Problems in the concept art (do not use as-is)
- AI text errors: "Adventuer Worlds", "Wondep Jump", "Flewer Fairy", "Laugy", and mislabelled views ("Jump" instead of "Side"). **The logo must be redrawn.**
- Animation frames are tiny and inconsistent between frames. They are pose references only, not usable sprites.
- Leon is missing from two sheets, and details vary between sheets (keep `design-reference-6chars.jpg` as canonical).
- The Leon sheet's spec (8–12k tris, 2K PBR textures, "Ready for Unity") is outdated. New targets are below.

## Production pipeline (Three.js)
1. Turnaround (front/side/back) → AI image-to-3D (tool and budget: Q3) → GLB.
2. Blender cleanup: ≤ 8k triangles, one mesh, one 1024² atlas (toon, no PBR), T-pose, origin at the feet, facing +Z.
3. Auto-rig (Mixamo or Rigify) → **one shared animation set** retargeted to all 6 (11 clips).
4. Export GLB (Draco/meshopt compressed, KTX2 textures) → `apps/client/public/models/<Id>.glb`.
5. Replace the placeholder `visual` group. Gameplay and the hitbox stay unchanged.

Budget per character on mobile WebView: ≤ 8k tris, ≤ 30 bones, 1 material, ≤ 1.5 MB GLB.
