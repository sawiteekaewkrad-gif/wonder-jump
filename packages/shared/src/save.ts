import { parseCharacterKey, parseLevel, type CharacterKey } from './ids.js';

/** Local save schema. Bump SAVE_SCHEMA_VERSION on breaking changes and add a migration. */
export const SAVE_SCHEMA_VERSION = 1;

export interface LevelProgress {
  levelId: string; // "W1_L1"
  unlocked: boolean;
  completed: boolean;
  stars: number; // 0..3
  bestTimeSeconds: number;
}

export interface SaveData {
  schemaVersion: number;
  selectedCharacter: CharacterKey;
  totalCoins: number;
  levels: LevelProgress[];
  settings: { musicVolume: number; sfxVolume: number; joystickScale: number; joystickFloating: boolean };
}

export function newSave(): SaveData {
  return {
    schemaVersion: SAVE_SCHEMA_VERSION,
    selectedCharacter: 'Leon',
    totalCoins: 0,
    levels: [{ levelId: 'W1_L1', unlocked: true, completed: false, stars: 0, bestTimeSeconds: 0 }],
    settings: { musicVolume: 0.8, sfxVolume: 1, joystickScale: 1, joystickFloating: true },
  };
}

const clamp = (v: unknown, lo: number, hi: number, fallback: number): number =>
  typeof v === 'number' && Number.isFinite(v) ? Math.min(hi, Math.max(lo, v)) : fallback;

/** Parses untrusted JSON (corrupt files, old versions) into a valid SaveData; never throws. */
export function loadSave(json: string | null | undefined): SaveData {
  const fresh = newSave();
  if (!json) return fresh;
  let raw: unknown;
  try {
    raw = JSON.parse(json);
  } catch {
    return fresh;
  }
  if (typeof raw !== 'object' || raw === null) return fresh;
  const r = raw as Partial<SaveData>;
  const levels = Array.isArray(r.levels)
    ? r.levels
        .filter((l): l is LevelProgress => !!l && typeof l.levelId === 'string' && parseLevel(l.levelId) !== null)
        .map((l) => ({
          levelId: l.levelId,
          unlocked: !!l.unlocked,
          completed: !!l.completed,
          stars: clamp(l.stars, 0, 3, 0),
          bestTimeSeconds: clamp(l.bestTimeSeconds, 0, 86400, 0),
        }))
    : fresh.levels;
  return {
    schemaVersion: SAVE_SCHEMA_VERSION,
    selectedCharacter: parseCharacterKey(r.selectedCharacter) ?? fresh.selectedCharacter,
    totalCoins: Math.floor(clamp(r.totalCoins, 0, 1e9, 0)),
    levels: levels.length > 0 ? levels : fresh.levels,
    settings: {
      musicVolume: clamp(r.settings?.musicVolume, 0, 1, fresh.settings.musicVolume),
      sfxVolume: clamp(r.settings?.sfxVolume, 0, 1, fresh.settings.sfxVolume),
      joystickScale: clamp(r.settings?.joystickScale, 0.5, 2, fresh.settings.joystickScale),
      joystickFloating: typeof r.settings?.joystickFloating === 'boolean' ? r.settings.joystickFloating : true,
    },
  };
}
