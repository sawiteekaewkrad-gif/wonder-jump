import type { CollectibleType } from './catalog.js';
import type { WorldKey } from './ids.js';

/**
 * Level data: plain JSON-friendly objects read by the client (rendering) and, from Phase 2, by
 * the authoritative server (collision). Gameplay runs on the X/Y plane.
 * Boxes are axis-aligned; (x, y) is the bottom-left corner, in metres.
 */
export interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

/** solid: blocks from every side · oneway: only blocks from above (jump up through it) */
export type SolidKind = 'solid' | 'oneway';

export interface SolidDef extends Box {
  kind: SolidKind;
  /** Visual style hint for the renderer. */
  style?: 'grass' | 'stone' | 'wood' | 'gold';
}

/** A platform moving back and forth between its start position and start + (dx, dy). */
export interface MovingPlatformDef extends Box {
  kind: SolidKind;
  dx: number;
  dy: number;
  /** Seconds for a full there-and-back cycle. */
  period: number;
  /** Phase offset 0..1. */
  phase?: number;
}

export interface LevelItem {
  uid: string;
  type: CollectibleType;
  x: number;
  y: number;
}

export interface LevelDef {
  id: string;
  name: string;
  world: WorldKey;
  /** Feet position of player 1 (player 2 spawns 1.2 m to the left). */
  spawn: { x: number; y: number };
  /** Horizontal walls and the kill plane (falling below minY = fell out). */
  bounds: { minX: number; maxX: number; minY: number };
  solids: SolidDef[];
  moving: MovingPlatformDef[];
  items: LevelItem[];
  goal: { x: number; y: number };
}

/** Collision box at a moment in time; `id` is stable ("s3" solid, "m1" moving). */
export interface ColliderBox extends Box {
  id: string;
  kind: SolidKind;
}

/** Smooth ping-pong 0→1→0 over one period. */
export function movingFraction(def: MovingPlatformDef, time: number): number {
  const p = def.period > 0 ? (time / def.period + (def.phase ?? 0)) % 1 : 0;
  return 0.5 - 0.5 * Math.cos(p * Math.PI * 2);
}

export function movingPosition(def: MovingPlatformDef, time: number): { x: number; y: number } {
  const f = movingFraction(def, time);
  return { x: def.x + def.dx * f, y: def.y + def.dy * f };
}

/** All colliders of a level at `time`. */
export function collidersAt(level: LevelDef, time: number): ColliderBox[] {
  const out: ColliderBox[] = level.solids.map((s, i) => ({ id: `s${i}`, kind: s.kind, x: s.x, y: s.y, w: s.w, h: s.h }));
  level.moving.forEach((m, i) => {
    const p = movingPosition(m, time);
    out.push({ id: `m${i}`, kind: m.kind, x: p.x, y: p.y, w: m.w, h: m.h });
  });
  return out;
}

/** Static sanity checks; returns a list of problems (empty = valid). */
export function validateLevel(level: LevelDef): string[] {
  const errors: string[] = [];
  const uids = new Set<string>();
  for (const it of level.items) {
    if (uids.has(it.uid)) errors.push(`duplicate item uid ${it.uid}`);
    uids.add(it.uid);
  }
  for (const [i, s] of [...level.solids, ...level.moving].entries()) {
    if (!(s.w > 0 && s.h > 0)) errors.push(`collider #${i} has non-positive size`);
  }
  const standsOn = (x: number, y: number) =>
    level.solids.some((s) => x >= s.x && x <= s.x + s.w && Math.abs(s.y + s.h - y) < 1e-6);
  if (!standsOn(level.spawn.x, level.spawn.y)) errors.push('spawn is not on top of a solid');
  if (!standsOn(level.goal.x, level.goal.y)) errors.push('goal is not on top of a solid');
  if (level.spawn.x < level.bounds.minX || level.goal.x > level.bounds.maxX) errors.push('spawn/goal outside bounds');
  if (level.bounds.minY >= Math.min(...level.solids.map((s) => s.y + s.h))) errors.push('kill plane above the ground');
  return errors;
}
