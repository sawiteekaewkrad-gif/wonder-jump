import { describe, expect, it } from 'vitest';
import {
  ASSETS, CHARACTERS, CHARACTER_KEYS, CharacterId, PLAYABLE_WORLDS, DEFAULT_MOVEMENT, PlayerState,
  formatLevel, parseLevel, parseCharacterKey, gravity, jumpVelocity, doubleJumpVelocity,
  tryCollect, resetCollectibles, hasSharedAuthority, loadSave, newSave, getAsset, ACCESSORY_PARTS,
  type CollectibleState,
} from '../src/index.js';

describe('ids', () => {
  it('keeps the six character IDs and their fixed numbers', () => {
    expect(CHARACTER_KEYS).toEqual(['Leon', 'Elin', 'Miyu', 'Koko', 'Riku', 'Flora']);
    expect(Object.values(CharacterId)).toEqual([1, 2, 3, 4, 5, 6]);
    expect(CHARACTERS.map((c) => c.key)).toEqual(CHARACTER_KEYS);
  });
  it('parses character keys exactly', () => {
    expect(parseCharacterKey('Flora')).toBe('Flora');
    expect(parseCharacterKey('flora')).toBeNull();
    expect(parseCharacterKey('3')).toBeNull();
    expect(parseCharacterKey(undefined)).toBeNull();
  });
  it('round-trips level ids and rejects invalid ones', () => {
    expect(formatLevel({ world: 4, index: 5 })).toBe('W4_L5');
    expect(parseLevel('W4_L5')).toEqual({ world: 4, index: 5 });
    for (const bad of ['W6_L1', 'W1_L6', 'W0_L1', 'garbage', 'W1_L1x']) expect(parseLevel(bad)).toBeNull();
  });
});

describe('catalog', () => {
  it('has unique ids and the v1 counts', () => {
    const ids = ASSETS.map((a) => a.id);
    expect(new Set(ids).size).toBe(ids.length);
    const count = (c: string) => ASSETS.filter((a) => a.category === c).length;
    expect([count('enemy'), count('boss'), count('item'), count('prop')]).toEqual([7, 5, 6, 6]);
    expect(count('platform') + count('hazard')).toBe(8);
  });
  it('follows the world assignment brief', () => {
    expect(getAsset('LavaSlime').world).toBe('CrystalCave');
    expect(getAsset('Lava_Hazard').world).toBe('CrystalCave');
    expect(getAsset('OceanPuffer').world).toBe('OceanAdventure');
    expect(getAsset('SkyDrone').world).toBe('SkyCastle');
    expect(getAsset('Stone_Platform').world).toBe('SkyCastle');
  });
  it('gives every world a boss, a platform and an enemy', () => {
    for (const w of PLAYABLE_WORLDS) {
      const inWorld = ASSETS.filter((a) => a.world === w);
      expect(inWorld.filter((a) => a.category === 'boss')).toHaveLength(1);
      expect(inWorld.some((a) => a.category === 'platform')).toBe(true);
      expect(inWorld.some((a) => a.category === 'enemy')).toBe(true);
    }
  });
  it('makes items triggers and stands the coin upright', () => {
    for (const a of ASSETS.filter((x) => x.category === 'item')) expect(a.collider).toBe('trigger');
    expect(getAsset('Coin').rotation?.[0]).toBe(90);
  });
  it('defines accessory parts for every character', () => {
    for (const c of CHARACTERS) expect(ACCESSORY_PARTS[c.accessory].length).toBeGreaterThan(0);
  });
});

describe('player', () => {
  it('derives gravity and jump velocity from height and time-to-apex', () => {
    const s = { ...DEFAULT_MOVEMENT, jumpHeight: 3, timeToApex: 0.5, doubleJumpHeight: 3 };
    expect(gravity(s)).toBeCloseTo(24);
    expect(jumpVelocity(s)).toBeCloseTo(12);
    expect(doubleJumpVelocity(s)).toBeCloseTo(12);
    expect((jumpVelocity(s) ** 2) / (2 * gravity(s))).toBeCloseTo(3);
  });
  it('covers the 11 animation states from the model sheets', () => {
    for (const k of ['Idle', 'Walk', 'Run', 'Jump', 'DoubleJump', 'Fall', 'Land', 'Attack', 'Hurt', 'Death', 'Victory'])
      expect(PlayerState).toHaveProperty(k);
  });
});

describe('collectibles (server-authoritative)', () => {
  const fresh = () =>
    new Map<string, CollectibleState>([['coin-1', { uid: 'coin-1', type: 'Coin', value: 1, collectedBy: null }]]);

  it('lets only the first request win', () => {
    const items = fresh();
    expect(tryCollect(items, 'coin-1', 'p1', 'server')).toMatchObject({ ok: true, by: 'p1' });
    expect(tryCollect(items, 'coin-1', 'p2', 'server')).toEqual({ ok: false, reason: 'already-collected' });
    expect(tryCollect(items, 'coin-1', 'p1', 'server')).toEqual({ ok: false, reason: 'already-collected' });
  });
  it('never lets a client decide', () => {
    const items = fresh();
    expect(tryCollect(items, 'coin-1', 'p1', 'client')).toEqual({ ok: false, reason: 'not-authoritative' });
    expect(items.get('coin-1')?.collectedBy).toBeNull();
    expect(hasSharedAuthority('offline')).toBe(true);
  });
  it('rejects unknown items and re-arms on reset', () => {
    const items = fresh();
    expect(tryCollect(items, 'nope', 'p1', 'offline')).toEqual({ ok: false, reason: 'unknown' });
    tryCollect(items, 'coin-1', 'p1', 'offline');
    resetCollectibles(items);
    expect(tryCollect(items, 'coin-1', 'p2', 'offline')).toMatchObject({ ok: true, by: 'p2' });
  });
});

describe('save', () => {
  it('falls back to a fresh save on corrupt input', () => {
    expect(loadSave('{not json')).toEqual(newSave());
    expect(loadSave(null)).toEqual(newSave());
    expect(loadSave('42')).toEqual(newSave());
  });
  it('sanitises values', () => {
    const s = loadSave(JSON.stringify({ selectedCharacter: 'Mario', totalCoins: -5, levels: [{ levelId: 'W9_L9' }], settings: { sfxVolume: 7 } }));
    expect(s.selectedCharacter).toBe('Leon');
    expect(s.totalCoins).toBe(0);
    expect(s.levels[0]?.levelId).toBe('W1_L1');
    expect(s.settings.sfxVolume).toBe(1);
  });
  it('keeps valid data', () => {
    const s = loadSave(JSON.stringify({ selectedCharacter: 'Riku', totalCoins: 120, levels: [{ levelId: 'W1_L2', unlocked: true, stars: 2 }] }));
    expect(s.selectedCharacter).toBe('Riku');
    expect(s.totalCoins).toBe(120);
    expect(s.levels[0]).toMatchObject({ levelId: 'W1_L2', unlocked: true, stars: 2 });
  });
});
