import { describe, expect, it } from 'vitest';
import {
  DEFAULT_MOVEMENT, FIXED_DT, NO_INPUT, PLAYER_HEIGHT, TEST_MOVEMENT,
  createPlayer, stepPlayer, validateLevel, collidersAt, movingPosition,
  type InputFrame, type LevelDef, type MotorEvent, type PlayerSim,
} from '../src/index.js';

/** Flat floor (top y=0) from x=-50 to 50, plus optional extras. */
function arena(extra: Partial<LevelDef> = {}): LevelDef {
  return {
    id: 'T', name: 'test', world: 'GreenForest', spawn: { x: 0, y: 0 },
    bounds: { minX: -50, maxX: 50, minY: -20 },
    solids: [{ kind: 'solid', x: -50, y: -5, w: 100, h: 5 }],
    moving: [], items: [], goal: { x: 10, y: 0 },
    ...extra,
  };
}

const input = (o: Partial<InputFrame> = {}): InputFrame => ({ ...NO_INPUT, ...o });

/** Runs `frames` steps; `inputAt(frame)` provides the input. Returns all events. */
function run(p: PlayerSim, level: LevelDef, frames: number, inputAt: (f: number) => InputFrame, t0 = 0) {
  const events: MotorEvent[] = [];
  let maxY = p.y;
  for (let f = 0; f < frames; f++) {
    events.push(...stepPlayer(p, inputAt(f), level, t0 + f * FIXED_DT));
    maxY = Math.max(maxY, p.y);
  }
  return { events, maxY };
}

describe('motor: ground and walls', () => {
  it('rests on the floor without drifting', () => {
    const p = createPlayer(0, 0);
    run(p, arena(), 120, () => NO_INPUT);
    expect(p.grounded).toBe(true);
    expect(p.y).toBeCloseTo(0, 6);
    expect(p.state).toBe('Idle');
  });

  it('accelerates to max run speed and stops at a wall', () => {
    const level = arena({ solids: [...arena().solids, { kind: 'solid', x: 5, y: 0, w: 1, h: 4 }] });
    const p = createPlayer(0, 0);
    run(p, level, 30, () => input({ moveX: 1, jumpHeld: false }));
    expect(p.state).toBe('Run');
    run(p, level, 120, () => input({ moveX: 1 }));
    expect(p.x).toBeCloseTo(5 - 0.322, 3);
    expect(p.vx).toBe(0);
  });

  it('walks with a small stick tilt', () => {
    const p = createPlayer(0, 0);
    run(p, arena(), 60, () => input({ moveX: 0.3 }));
    expect(p.state).toBe('Walk');
    expect(p.vx).toBeCloseTo(0.3 * DEFAULT_MOVEMENT.maxRunSpeed, 3);
  });
});

describe('motor: jumping', () => {
  it('reaches the designed jump height (±5%)', () => {
    const p = createPlayer(0, 0);
    const { maxY, events } = run(p, arena(), 120, (f) => input({ jumpPressed: f === 0, jumpHeld: true }));
    expect(events.filter((e) => e === 'jump')).toHaveLength(1);
    expect(maxY).toBeGreaterThan(DEFAULT_MOVEMENT.jumpHeight * 0.95);
    expect(maxY).toBeLessThan(DEFAULT_MOVEMENT.jumpHeight * 1.05);
    expect(events).toContain('land');
  });

  it('jumps lower when the button is released early (variable height)', () => {
    const p = createPlayer(0, 0);
    const { maxY } = run(p, arena(), 120, (f) => input({ jumpPressed: f === 0, jumpHeld: f < 5 }));
    expect(maxY).toBeLessThan(DEFAULT_MOVEMENT.jumpHeight * 0.6);
  });

  it('double jumps once, never a third time', () => {
    const p = createPlayer(0, 0);
    const presses = new Set([0, 20, 40]);
    const { maxY, events } = run(p, arena(), 150, (f) => input({ jumpPressed: presses.has(f), jumpHeld: true }));
    expect(events.filter((e) => e === 'jump')).toHaveLength(1);
    expect(events.filter((e) => e === 'doubleJump')).toHaveLength(1);
    expect(maxY).toBeGreaterThan(DEFAULT_MOVEMENT.jumpHeight + 1.5);
  });

  it('allows a ground jump within coyote time after walking off a ledge', () => {
    const level = arena({ solids: [{ kind: 'solid', x: -50, y: -5, w: 50, h: 5 }] }); // floor ends at x=0
    const p = createPlayer(-0.5, 0);
    let offFrame = -1;
    const { events } = run(p, level, 60, (f) => {
      if (offFrame < 0 && !p.grounded) offFrame = f;
      const press = offFrame >= 0 && f === offFrame + 3; // 3 frames ≈ 0.05 s < coyote 0.1 s
      return input({ moveX: 1, jumpPressed: press, jumpHeld: true });
    });
    expect(events).toContain('jump');
    expect(events).not.toContain('doubleJump');
  });

  it('after coyote time the only air jump is the double jump', () => {
    const level = arena({ solids: [{ kind: 'solid', x: -50, y: -5, w: 50, h: 5 }] });
    const p = createPlayer(-0.5, 0);
    let offFrame = -1;
    const { events } = run(p, level, 60, (f) => {
      if (offFrame < 0 && !p.grounded) offFrame = f;
      return input({ moveX: 1, jumpPressed: offFrame >= 0 && f === offFrame + 15, jumpHeld: true });
    });
    expect(events).not.toContain('jump');
    expect(events).toContain('doubleJump');
  });

  it('buffers a jump pressed just before landing', () => {
    const p = createPlayer(0, 3); // falling from 3 m
    p.grounded = false;
    p.jumpsUsed = 2; // no air jumps left
    let pressed = false;
    const { events } = run(p, arena(), 120, () => {
      const press = !pressed && !p.grounded && p.y < 0.6 && p.vy < 0;
      if (press) pressed = true;
      return input({ jumpPressed: press, jumpHeld: true });
    });
    expect(pressed).toBe(true);
    expect(events.indexOf('land')).toBeGreaterThanOrEqual(0);
    expect(events.indexOf('jump')).toBeGreaterThan(events.indexOf('land'));
  });

  it('bonks its head on a solid ceiling', () => {
    const level = arena({ solids: [...arena().solids, { kind: 'solid', x: -5, y: 3, w: 10, h: 1 }] });
    const p = createPlayer(0, 0);
    const { maxY, events } = run(p, level, 60, (f) => input({ jumpPressed: f === 0, jumpHeld: true }));
    expect(events).toContain('bonk');
    expect(maxY + PLAYER_HEIGHT).toBeLessThanOrEqual(3 + 1e-6);
  });
});

describe('motor: special platforms', () => {
  it('jumps up through a one-way platform and lands on top', () => {
    const level = arena({ solids: [...arena().solids, { kind: 'oneway', x: -2, y: 2, w: 4, h: 0.3 }] });
    const p = createPlayer(0, 0);
    run(p, level, 120, (f) => input({ jumpPressed: f === 0, jumpHeld: true }));
    expect(p.grounded).toBe(true);
    expect(p.y).toBeCloseTo(2.3, 6);
    expect(p.groundId).toBe('s1');
  });

  it('does not let a one-way platform block sideways movement', () => {
    const level = arena({ solids: [...arena().solids, { kind: 'oneway', x: 2, y: 0.5, w: 4, h: 0.3 }] });
    const p = createPlayer(0, 0);
    run(p, level, 60, () => input({ moveX: 1 }));
    expect(p.x).toBeGreaterThan(4);
  });

  it('carries the player on a moving platform', () => {
    const plat = { kind: 'solid' as const, x: -1.5, y: 1, w: 3, h: 0.5, dx: 6, dy: 0, period: 4 };
    const level = arena({ moving: [plat] });
    const p = createPlayer(0, 1.5);
    run(p, level, 120, () => NO_INPUT); // 2 s = half a cycle → platform moved +6
    expect(p.grounded).toBe(true);
    expect(p.groundId).toBe('m0');
    const pos = movingPosition(plat, 120 * FIXED_DT);
    expect(p.x).toBeCloseTo(0 + (pos.x - plat.x), 1);
  });

  it('reports falling out of the level', () => {
    const level = arena({ solids: [{ kind: 'solid', x: -50, y: -5, w: 10, h: 5 }] });
    const p = createPlayer(0, 0);
    const { events } = run(p, level, 200, () => NO_INPUT);
    expect(events).toContain('fellOut');
  });
});

describe('motor: states and determinism', () => {
  it('goes Idle → Run → Jump → Fall → Idle', () => {
    const p = createPlayer(0, 0);
    const seen: string[] = [];
    run(p, arena(), 200, (f) => {
      if (seen[seen.length - 1] !== p.state) seen.push(p.state);
      if (f < 30) return input({ moveX: 1 });
      if (f === 30) return input({ moveX: 1, jumpPressed: true, jumpHeld: true });
      if (f < 60) return input({ jumpHeld: true });
      return NO_INPUT;
    });
    seen.push(p.state);
    expect(seen).toEqual(expect.arrayContaining(['Idle', 'Run', 'Jump', 'Fall']));
    expect(seen[seen.length - 1]).toBe('Idle');
  });

  it('enters Attack and returns', () => {
    const p = createPlayer(0, 0);
    const { events } = run(p, arena(), 2, (f) => input({ attackPressed: f === 0 }));
    expect(events).toContain('attack');
    expect(p.state).toBe('Attack');
    run(p, arena(), 40, () => NO_INPUT);
    expect(p.state).toBe('Idle');
  });

  it('is deterministic', () => {
    const script = (f: number) => input({ moveX: Math.sin(f / 10), jumpPressed: f % 47 === 0, jumpHeld: f % 47 < 20, attackPressed: f % 90 === 5 });
    const a = createPlayer(3, 0);
    const b = createPlayer(3, 0);
    run(a, TEST_MOVEMENT, 900, script);
    run(b, TEST_MOVEMENT, 900, script);
    expect(a).toEqual(b);
  });
});

describe('test level', () => {
  it('is valid', () => {
    expect(validateLevel(TEST_MOVEMENT)).toEqual([]);
  });

  it('gap after the start is crossable with a running single jump', () => {
    const p = createPlayer(16, 0);
    run(p, TEST_MOVEMENT, 300, (f) => input({ moveX: 1, jumpPressed: p.grounded && p.x > 21, jumpHeld: true }));
    expect(p.x).toBeGreaterThan(26); // crossed the 22–26 gap
    expect(p.y).toBeGreaterThanOrEqual(0);
  });

  it('the 5 m wall needs a double jump', () => {
    const wallFace = 52;
    const single = createPlayer(48, 0);
    run(single, TEST_MOVEMENT, 240, (f) => input({ moveX: 1, jumpPressed: f === 20, jumpHeld: true }));
    expect(single.x).toBeLessThan(wallFace);
    const dbl = createPlayer(48, 0);
    run(dbl, TEST_MOVEMENT, 300, (f) => input({ moveX: dbl.grounded && dbl.x > 53.5 ? 0 : 1, jumpPressed: f === 20 || f === 42, jumpHeld: true }));
    expect(dbl.x).toBeGreaterThan(wallFace);
    expect(dbl.y).toBeCloseTo(5, 3); // standing on top of the wall
  });

  it('exposes colliders including the moving platform', () => {
    const c = collidersAt(TEST_MOVEMENT, 0);
    expect(c.some((b) => b.id === 'm0')).toBe(true);
    expect(c.filter((b) => b.kind === 'oneway')).toHaveLength(3);
  });
});
