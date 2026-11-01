# Art Direction — Wonder Jump – Adventure Worlds

Source: owner-supplied concept model sheets (AI-generated), stored in `Documentation/Art/concepts/`:

| File | Content |
|---|---|
| `design-reference-6chars.jpg` | All 6 characters: front/side/back, expressions, action poses |
| `model-sheet-5chars-a.jpg`, `model-sheet-5chars-b.jpg` | Turnarounds, palettes, accessories, 11 animation key poses (Leon missing) |
| `leon-full-sheet.jpg` | Leon: turnaround, close-ups, expressions, sprite rows, in-game mock, props, model spec |
| `keyart-lineup-logo.jpg` | Key art: 6-character line-up, correct logo ("Wonder Jump · Adventure Worlds"), name plates |
| `character-sheet-6chars-v2.jpg` | 6 panels: hero pose + front/side/back per character (source of the in-game cut-outs) |

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
- (Older sheets) AI text errors: "Adventuer Worlds", "Wondep Jump", "Flewer Fairy", "Laugy", and mislabelled views ("Jump" instead of "Side"). **The logo must be redrawn.**
- Animation frames are tiny and inconsistent between frames. They are pose references only, not usable sprites.
- Leon is missing from two sheets, and details vary between sheets (keep `design-reference-6chars.jpg` as canonical).
- The Leon sheet's spec (8–12k tris, 2K PBR textures, "Ready for Unity") is outdated. New targets are below.

## In-game art now: paper cut-outs (D-14, 2026-11-01)
Owner asked to put the characters in "like the concept art". Until the GLB models exist (Phase 7), the game uses the owner's art directly:

| Item | Source | Processing | Output |
|---|---|---|---|
| 6 characters | Hero pose of each panel in `character-sheet-6chars-v2.jpg` | Crop → 2× upscale → background removal (rembg `isnet-anime`) → drop badges/fragments → trim → 512 px tall | `apps/client/public/characters/<Id>.webp` (40–48 KB each) |
| Logo | `keyart-lineup-logo.jpg` | rembg `u2net` → 520 px wide | `apps/client/public/ui/logo.webp` (menu) |
| Card portrait | Same character WebP | — | Character card in the menu |

- Rendering: one unlit textured plane per character (alpha-tested, silhouette shadow), feet at the image bottom, sizes in `CHARACTER_ART` (`catalog.ts`). **1 draw call per character** (the primitive chibi used ~15). Menu: 195 → 62 draws.
- Animation (`SpriteAnimator`): paper-turn mirror when changing direction, run bob + waddle, jump stretch, **cartwheel** double jump (a flat drawing cannot front-flip), landing squash, paper-spin attack with a gold slash arc.
- Fallback: `?chibi=primitive` shows the procedural chibi.
- Limits (honest): it is a flat drawing. One pose for every state (no real leg/arm motion), the side view is not used, edges have a faint matte halo at close range, and Miyu's staff is part of the drawing (not a separate weapon). The key-art logo is now usable, so "logo must be redrawn" applies only to the older sheets.
- To go further without 3D: per-state drawings (idle/run/jump/attack poses) from the same artist/AI prompt would plug into the same plane (swap texture per state).

## Production pipeline (Three.js)
1. Turnaround (front/side/back) → AI image-to-3D (tool and budget: Q3) → GLB.
2. Blender cleanup: ≤ 8k triangles, one mesh, one 1024² atlas (toon, no PBR), T-pose, origin at the feet, facing +Z.
3. Auto-rig (Mixamo or Rigify) → **one shared animation set** retargeted to all 6 (11 clips).
4. Export GLB (Draco/meshopt compressed, KTX2 textures) → `apps/client/public/models/<Id>.glb`.
5. Replace the placeholder `visual` group. Gameplay and the hitbox stay unchanged.

Budget per character on mobile WebView: ≤ 8k tris, ≤ 30 bones, 1 material, ≤ 1.5 MB GLB.
