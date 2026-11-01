import type { CharacterKey, WorldKey } from './ids.js';

/**
 * Single source of truth for every placeholder asset (ported from the Unity Phase 0 AssetCatalog).
 * Engine-free data: the client builds Three.js meshes from it; tests and (later) the server read
 * gameplay dimensions from it.
 *
 * Units are metres. Axes: +X right, +Y up, +Z towards the camera (characters face +Z).
 * Unit primitives: sphere/box/octahedron fit a 1×1×1 box; capsule and cylinder are 1 wide, 2 tall;
 * cone is 1 wide, 1 tall; torus lies in the XY plane with outer diameter 1.
 */

export type Shape = 'sphere' | 'capsule' | 'cylinder' | 'box' | 'cone' | 'torus' | 'octahedron';
export type Vec3 = readonly [number, number, number];

export type AssetCategory = 'enemy' | 'boss' | 'item' | 'platform' | 'hazard' | 'prop';
export type ColliderRole = 'none' | 'solid' | 'trigger';
/** feet: bottom at y=0 · top: top surface at y=0 · center: centred on the origin */
export type Pivot = 'feet' | 'top' | 'center';
export type CollectibleType = 'Coin' | 'Star' | 'Heart' | 'CrystalGem' | 'Key' | 'PowerUp';
export type Accessory = 'Goggles' | 'BunnyEars' | 'WitchHat' | 'FoxEars' | 'Headphones' | 'FlowerCrown';

// ---------------------------------------------------------------------------------- palette

/** Shared colours (hex). Character colours follow the concept model sheets (Documentation/Art). */
export const PALETTE = {
  cream: 0xffe0ad,
  skin: 0xffd2b8,
  dark: 0x2a1f33,
  eyeWhite: 0xffffff,
  white: 0xffffff,
  gold: 0xffb829,
  crystal: 0x26c7ff,
  purple: 0x7a38d1,
  pink: 0xff66a6,
  blue: 0x2e7aeb,
  green: 0x3db85c,
  orange: 0xff6e1f,
  grass: 0x40b847,
  earth: 0x7a4529,
  stone: 0x6b738c,
  ice: 0x73d1ff,
  lava: 0xff3d0a,
  candy: 0xff8cc7,
  ocean: 0x0d7ac7,
  leaf: 0x47a647,
  wood: 0x6e3d1f,
  red: 0xd91f29,
} as const;
export type PaletteKey = keyof typeof PALETTE;

// ---------------------------------------------------------------------------------- characters

export interface CharacterDef {
  key: CharacterKey;
  displayName: string;
  title: string;
  /** One-line personality from the concept sheets (Thai). */
  tagline: string;
  outfit: number;
  outfitAccent: number;
  hair: number;
  eyes: number;
  accessory: Accessory;
  /** Signature item shown on the model sheets; drives the future Attack visuals. */
  signatureItem: string;
}

export const CHARACTERS: readonly CharacterDef[] = [
  { key: 'Leon', displayName: 'Leon', title: 'Adventurer', tagline: 'ไม่ว่าทางจะยากแค่ไหน ฉันก็จะไปให้ถึง!',
    outfit: 0x2f5fb8, outfitAccent: 0xf2ece0, hair: 0x7a4a2a, eyes: 0x2f7fe0, accessory: 'Goggles', signatureItem: 'Sword' },
  { key: 'Elin', displayName: 'Elin', title: 'Bunny Explorer', tagline: 'นักสำรวจน้อยผู้สดใสและชื่อสัตย์',
    outfit: 0xf7a8c4, outfitAccent: 0xfff3e8, hair: 0xf58fb5, eyes: 0x9b4fd1, accessory: 'BunnyEars', signatureItem: 'Bunny Bag' },
  { key: 'Miyu', displayName: 'Miyu', title: 'Little Witch', tagline: 'แม่มดน้อยผู้รักเวทมนตร์',
    outfit: 0x5e2f99, outfitAccent: 0xffc94d, hair: 0xc2aee8, eyes: 0x8a4fe0, accessory: 'WitchHat', signatureItem: 'Crystal Staff' },
  { key: 'Koko', displayName: 'Koko', title: 'Fox Scout', tagline: 'นักสำรวจจิ้งจอกแสนซุกซน',
    outfit: 0x8a5a33, outfitAccent: 0x4e9a4a, hair: 0xf08a3c, eyes: 0xd98a1f, accessory: 'FoxEars', signatureItem: 'Compass' },
  { key: 'Riku', displayName: 'Riku', title: 'Sky Runner', tagline: 'นักวิ่งแห่งท้องฟ้า รวดเร็วและไม่หยุด',
    outfit: 0x24366e, outfitAccent: 0x39c6f0, hair: 0xa9a6d9, eyes: 0x2f8fe8, accessory: 'Headphones', signatureItem: 'Wind Wings' },
  { key: 'Flora', displayName: 'Flora', title: 'Flower Fairy', tagline: 'ภูตดอกไม้ผู้เชื่อมโยงพลังธรรมชาติ',
    outfit: 0x6dbb57, outfitAccent: 0xfff0c8, hair: 0xe8d0a0, eyes: 0xc98a2f, accessory: 'FlowerCrown', signatureItem: 'Flower Wand' },
];

export function getCharacter(key: CharacterKey): CharacterDef {
  const def = CHARACTERS.find((c) => c.key === key);
  if (!def) throw new Error(`Unknown character ${key}`);
  return def;
}

/** Colour slot used by a body part; resolved per character by the renderer. */
export type PartColor = 'outfit' | 'outfitAccent' | 'hair' | 'eyes' | PaletteKey;

export interface PartDef {
  name: string;
  shape: Shape;
  position: Vec3;
  scale: Vec3;
  color: PartColor;
  /** Part of the gameplay silhouette (hitbox sizing). Accessories are not. */
  core?: boolean;
  castShadow?: boolean;
}

/**
 * Chibi body layout (v1 proportions, Z mirrored so the face points at the camera).
 * Fix vs v1: the face, eyes and highlights sat INSIDE the hair sphere (front at z 0.39), so the face
 * was invisible. They now protrude in front of it.
 */
export const CHIBI_BODY: readonly PartDef[] = [
  { name: 'Body', shape: 'capsule', position: [0, 0.78, 0], scale: [0.58, 0.72, 0.42], color: 'outfit', core: true, castShadow: true },
  { name: 'Belt', shape: 'cylinder', position: [0, 0.62, 0], scale: [0.6, 0.04, 0.44], color: 'outfitAccent' },
  { name: 'Head', shape: 'sphere', position: [0, 1.55, 0], scale: [0.92, 0.88, 0.78], color: 'hair', core: true, castShadow: true },
  { name: 'Face', shape: 'sphere', position: [0, 1.46, 0.2], scale: [0.74, 0.64, 0.44], color: 'skin' },
  { name: 'Eye_L', shape: 'sphere', position: [-0.17, 1.5, 0.405], scale: [0.12, 0.17, 0.06], color: 'eyes' },
  { name: 'Eye_R', shape: 'sphere', position: [0.17, 1.5, 0.405], scale: [0.12, 0.17, 0.06], color: 'eyes' },
  { name: 'EyeHighlight_L', shape: 'sphere', position: [-0.19, 1.54, 0.435], scale: [0.04, 0.05, 0.02], color: 'white' },
  { name: 'EyeHighlight_R', shape: 'sphere', position: [0.15, 1.54, 0.435], scale: [0.04, 0.05, 0.02], color: 'white' },
  { name: 'Blush_L', shape: 'sphere', position: [-0.27, 1.4, 0.39], scale: [0.09, 0.05, 0.02], color: 'pink' },
  { name: 'Blush_R', shape: 'sphere', position: [0.27, 1.4, 0.39], scale: [0.09, 0.05, 0.02], color: 'pink' },
  { name: 'Arm_L', shape: 'sphere', position: [-0.39, 0.91, 0], scale: [0.22, 0.48, 0.23], color: 'outfit', castShadow: true },
  { name: 'Arm_R', shape: 'sphere', position: [0.39, 0.91, 0], scale: [0.22, 0.48, 0.23], color: 'outfit', castShadow: true },
  { name: 'Leg_L', shape: 'capsule', position: [-0.17, 0.27, 0], scale: [0.22, 0.45, 0.24], color: 'dark', core: true, castShadow: true },
  { name: 'Leg_R', shape: 'capsule', position: [0.17, 0.27, 0], scale: [0.22, 0.45, 0.24], color: 'dark', core: true, castShadow: true },
];

export const ACCESSORY_PARTS: Record<Accessory, readonly PartDef[]> = {
  Goggles: [
    { name: 'GogglesBand', shape: 'cylinder', position: [0, 1.89, 0], scale: [0.48, 0.07, 0.48], color: 'earth' },
    { name: 'Goggle_L', shape: 'sphere', position: [-0.17, 1.86, 0.3], scale: [0.18, 0.14, 0.1], color: 'crystal' },
    { name: 'Goggle_R', shape: 'sphere', position: [0.17, 1.86, 0.3], scale: [0.18, 0.14, 0.1], color: 'crystal' },
  ],
  WitchHat: [
    { name: 'HatBrim', shape: 'cylinder', position: [0, 2.02, 0], scale: [0.68, 0.08, 0.58], color: 'outfit' },
    { name: 'HatCone', shape: 'cone', position: [0, 2.3, 0], scale: [0.42, 0.6, 0.42], color: 'outfit', castShadow: true },
    { name: 'HatStar', shape: 'sphere', position: [0.25, 2.12, 0.27], scale: [0.12, 0.12, 0.08], color: 'gold' },
  ],
  BunnyEars: [
    { name: 'Ear_L', shape: 'capsule', position: [-0.25, 2.08, 0], scale: [0.18, 0.48, 0.15], color: 'outfitAccent', castShadow: true },
    { name: 'Ear_R', shape: 'capsule', position: [0.25, 2.08, 0], scale: [0.18, 0.48, 0.15], color: 'outfitAccent', castShadow: true },
    { name: 'EarInner_L', shape: 'capsule', position: [-0.25, 2.08, 0.06], scale: [0.1, 0.36, 0.05], color: 'pink' },
    { name: 'EarInner_R', shape: 'capsule', position: [0.25, 2.08, 0.06], scale: [0.1, 0.36, 0.05], color: 'pink' },
  ],
  FoxEars: [
    { name: 'Ear_L', shape: 'cone', position: [-0.3, 1.98, 0], scale: [0.28, 0.4, 0.22], color: 'hair', castShadow: true },
    { name: 'Ear_R', shape: 'cone', position: [0.3, 1.98, 0], scale: [0.28, 0.4, 0.22], color: 'hair', castShadow: true },
  ],
  Headphones: [
    { name: 'Headphones', shape: 'torus', position: [0, 1.84, 0], scale: [0.62, 0.62, 0.18], color: 'outfit' },
    { name: 'EarCup_L', shape: 'cylinder', position: [-0.47, 1.58, 0], scale: [0.16, 0.06, 0.16], color: 'crystal' },
    { name: 'EarCup_R', shape: 'cylinder', position: [0.47, 1.58, 0], scale: [0.16, 0.06, 0.16], color: 'crystal' },
  ],
  FlowerCrown: [0, 1, 2, 3, 4].map((i): PartDef => {
    const a = (i * Math.PI * 2) / 5;
    return { name: `Flower_${i}`, shape: 'sphere', position: [Math.cos(a) * 0.37, 1.94, Math.sin(a) * 0.3], scale: [0.16, 0.16, 0.16], color: 'pink' };
  }),
};

/** Visual height of the core body before the feet lift (legs reach 0.18 below the origin). */
export const CHIBI_FEET_LIFT = 0.18;

/**
 * Gameplay hitbox for every character (from Unity Phase 0, measured on the builder):
 * capsule bottom exactly at the feet (stable ground checks), narrower than the visual.
 */
export const PLAYER_HITBOX = { height: 2.062, radius: 0.322 } as const;

// ---------------------------------------------------------------------------------- world assets

export interface AssetDef {
  id: string;
  category: AssetCategory;
  world: WorldKey;
  shape: Shape;
  size: Vec3;
  color: PaletteKey;
  collider: ColliderRole;
  pivot: Pivot;
  /** Euler rotation of the visual in degrees (coins stand upright to face the camera). */
  rotation?: Vec3;
  item?: { type: CollectibleType; value: number };
}

const platform = (id: string, world: WorldKey, color: PaletteKey): AssetDef =>
  ({ id, category: 'platform', world, shape: 'box', size: [4, 0.5, 1], color, collider: 'solid', pivot: 'top' });
const enemy = (id: string, world: WorldKey, shape: Shape, size: Vec3, color: PaletteKey): AssetDef =>
  ({ id, category: 'enemy', world, shape, size, color, collider: 'solid', pivot: 'feet' });
const boss = (id: string, world: WorldKey, shape: Shape, size: Vec3, color: PaletteKey): AssetDef =>
  ({ id, category: 'boss', world, shape, size, color, collider: 'solid', pivot: 'feet' });
const prop = (id: string, world: WorldKey, shape: Shape, size: Vec3, color: PaletteKey, collider: ColliderRole = 'none'): AssetDef =>
  ({ id, category: 'prop', world, shape, size, color, collider, pivot: 'feet' });
const item = (id: string, shape: Shape, size: Vec3, color: PaletteKey, type: CollectibleType, value: number, rotation?: Vec3): AssetDef =>
  ({ id, category: 'item', world: 'Shared', shape, size, color, collider: 'trigger', pivot: 'center', item: { type, value }, ...(rotation ? { rotation } : {}) });

export const ASSETS: readonly AssetDef[] = [
  // 1. Green Forest
  platform('Forest_Ground', 'GreenForest', 'grass'),
  enemy('MushroomWalker', 'GreenForest', 'sphere', [0.8, 0.65, 0.7], 'red'),
  enemy('ForestBee', 'GreenForest', 'sphere', [0.55, 0.45, 0.55], 'gold'),
  boss('Boss_ForestGuardian', 'GreenForest', 'capsule', [2.2, 2.4, 1.8], 'green'),
  prop('FantasyTree', 'GreenForest', 'cylinder', [0.35, 1.2, 0.35], 'wood'),
  prop('TreeCanopy', 'GreenForest', 'sphere', [1.6, 1.5, 1.4], 'leaf'),
  prop('MushroomProp', 'GreenForest', 'capsule', [0.6, 0.8, 0.6], 'pink'),
  // 2. Candy Kingdom
  platform('Candy_Platform', 'CandyKingdom', 'candy'),
  enemy('CandyJumper', 'CandyKingdom', 'sphere', [0.75, 0.75, 0.75], 'pink'),
  boss('Boss_CandyKing', 'CandyKingdom', 'capsule', [2.2, 2.5, 1.8], 'pink'),
  // 3. Ocean Adventure
  platform('Ocean_Platform', 'OceanAdventure', 'ocean'),
  enemy('OceanPuffer', 'OceanAdventure', 'sphere', [0.8, 0.8, 0.8], 'ocean'),
  boss('Boss_SeaMonster', 'OceanAdventure', 'sphere', [2.5, 2.2, 2], 'ocean'),
  // 4. Crystal Cave (also hosts the lava content)
  platform('Crystal_Platform', 'CrystalCave', 'ice'),
  enemy('CrystalCrawler', 'CrystalCave', 'box', [0.8, 0.55, 0.8], 'purple'),
  enemy('LavaSlime', 'CrystalCave', 'sphere', [0.8, 0.55, 0.8], 'lava'),
  { id: 'Lava_Hazard', category: 'hazard', world: 'CrystalCave', shape: 'box', size: [3, 0.25, 1], color: 'lava', collider: 'trigger', pivot: 'top' },
  boss('Boss_CrystalGolem', 'CrystalCave', 'box', [2.3, 2.5, 1.8], 'crystal'),
  prop('CrystalCluster', 'CrystalCave', 'octahedron', [0.7, 1.4, 0.7], 'crystal'),
  // 5. Sky Castle (Stone_Platform = castle masonry)
  platform('Sky_Platform', 'SkyCastle', 'white'),
  platform('Stone_Platform', 'SkyCastle', 'stone'),
  enemy('SkyDrone', 'SkyCastle', 'sphere', [0.65, 0.65, 0.65], 'gold'),
  boss('Boss_SkyOverlord', 'SkyCastle', 'capsule', [2.4, 2.8, 1.8], 'purple'),
  // Shared
  { id: 'Moving_Platform', category: 'platform', world: 'Shared', shape: 'box', size: [2, 0.35, 1], color: 'gold', collider: 'solid', pivot: 'top' },
  prop('WoodenCrate', 'Shared', 'box', [1, 1, 1], 'wood', 'solid'),
  prop('PortalFrame', 'Shared', 'torus', [1.5, 1.8, 0.3], 'gold', 'trigger'),
  // Collectibles (values are placeholders until the GDD economy)
  item('Coin', 'cylinder', [0.45, 0.08, 0.45], 'gold', 'Coin', 1, [90, 0, 0]),
  item('Star', 'sphere', [0.45, 0.45, 0.2], 'gold', 'Star', 1),
  item('Heart', 'sphere', [0.45, 0.45, 0.25], 'pink', 'Heart', 1),
  item('CrystalGem', 'octahedron', [0.45, 0.65, 0.35], 'crystal', 'CrystalGem', 10),
  item('Key', 'cylinder', [0.18, 0.55, 0.18], 'gold', 'Key', 1),
  item('PowerUp', 'sphere', [0.5, 0.5, 0.5], 'purple', 'PowerUp', 1),
];

export function getAsset(id: string): AssetDef {
  const def = ASSETS.find((a) => a.id === id);
  if (!def) throw new Error(`Unknown asset ${id}`);
  return def;
}

/** Height of a unit primitive (before scaling) along Y. */
export function unitHeight(shape: Shape): number {
  return shape === 'capsule' || shape === 'cylinder' ? 2 : 1;
}
