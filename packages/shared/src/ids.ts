/**
 * Fixed identifiers. Numeric values are written to save files and sent over the network:
 * NEVER renumber or reorder.
 */

export const CharacterId = {
  Leon: 1,
  Elin: 2,
  Miyu: 3,
  Koko: 4,
  Riku: 5,
  Flora: 6,
} as const;
export type CharacterKey = keyof typeof CharacterId;
export type CharacterId = (typeof CharacterId)[CharacterKey];

export const CHARACTER_KEYS: readonly CharacterKey[] = ['Leon', 'Elin', 'Miyu', 'Koko', 'Riku', 'Flora'];

/** Exact, case-sensitive match. Numeric strings are rejected. */
export function parseCharacterKey(value: unknown): CharacterKey | null {
  return typeof value === 'string' && (CHARACTER_KEYS as readonly string[]).includes(value)
    ? (value as CharacterKey)
    : null;
}

export const WorldId = {
  Shared: 0,
  GreenForest: 1,
  CandyKingdom: 2,
  OceanAdventure: 3,
  CrystalCave: 4,
  SkyCastle: 5,
} as const;
export type WorldKey = keyof typeof WorldId;
export type WorldId = (typeof WorldId)[WorldKey];

export const WORLD_COUNT = 5;
export const LEVELS_PER_WORLD = 5;
export const PLAYABLE_WORLDS: readonly Exclude<WorldKey, 'Shared'>[] = [
  'GreenForest',
  'CandyKingdom',
  'OceanAdventure',
  'CrystalCave',
  'SkyCastle',
];

export const WORLD_DISPLAY_NAME: Record<WorldKey, string> = {
  Shared: 'Shared',
  GreenForest: 'Green Forest',
  CandyKingdom: 'Candy Kingdom',
  OceanAdventure: 'Ocean Adventure',
  CrystalCave: 'Crystal Cave',
  SkyCastle: 'Sky Castle',
};

/** One of the 25 levels. Canonical string "W{world}_L{level}", e.g. "W1_L3". */
export interface LevelId {
  world: number; // 1..5
  index: number; // 1..5
}

export function isValidLevel(id: LevelId): boolean {
  return (
    Number.isInteger(id.world) &&
    Number.isInteger(id.index) &&
    id.world >= 1 &&
    id.world <= WORLD_COUNT &&
    id.index >= 1 &&
    id.index <= LEVELS_PER_WORLD
  );
}

export function formatLevel(id: LevelId): string {
  return `W${id.world}_L${id.index}`;
}

export function parseLevel(text: string): LevelId | null {
  const m = /^W(\d+)_L(\d+)$/.exec(text);
  if (!m) return null;
  const id = { world: Number(m[1]), index: Number(m[2]) };
  return isValidLevel(id) ? id : null;
}
