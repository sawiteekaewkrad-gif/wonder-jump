import { PLAYER_HITBOX } from './catalog.js';
import type { ColliderBox, LevelDef } from './level.js';
import { collidersAt } from './level.js';
import {
  DEFAULT_MOVEMENT, doubleJumpVelocity, gravity, jumpVelocity,
  type InputFrame, type MovementSettings, type PlayerStateKey,
} from './player.js';

/**
 * Deterministic kinematic platformer motor (decision D-02). Pure: same inputs → same outputs,
 * so the Phase 2 server can run it authoritatively and clients can predict/reconcile with it.
 * The player is an axis-aligned box centred on x with its bottom at y (the feet).
 * Always call with a FIXED dt (FIXED_DT); the client accumulates real time.
 */
export const FIXED_DT = 1 / 60;
export const PLAYER_WIDTH = PLAYER_HITBOX.radius * 2;
export const PLAYER_HEIGHT = PLAYER_HITBOX.height;
const SKIN = 1e-4;
const LAND_TIME = 0.1;
const ATTACK_TIME = 0.32;
const ATTACK_COOLDOWN = 0.12;
const HARD_LANDING_SPEED = 9;

export interface PlayerSim {
  x: number;
  y: number;
  vx: number;
  vy: number;
  grounded: boolean;
  /** Collider id under the feet ("s3", "m0") or null. */
  groundId: string | null;
  timeSinceGrounded: number;
  timeSinceJumpPressed: number;
  /** 0 = on ground, 1 = first jump used (or coyote expired), 2 = double jump used. */
  jumpsUsed: number;
  jumpCut: boolean;
  facing: 1 | -1;
  state: PlayerStateKey;
  stateTime: number;
  landTimer: number;
  attackTimer: number;
  attackCooldown: number;
}

export type MotorEvent = 'jump' | 'doubleJump' | 'land' | 'attack' | 'fellOut' | 'bonk';

export function createPlayer(x: number, y: number): PlayerSim {
  return {
    x, y, vx: 0, vy: 0, grounded: true, groundId: null,
    timeSinceGrounded: 0, timeSinceJumpPressed: Infinity, jumpsUsed: 0, jumpCut: false,
    facing: 1, state: 'Idle', stateTime: 0, landTimer: 0, attackTimer: 0, attackCooldown: 0,
  };
}

const approach = (v: number, target: number, maxDelta: number) =>
  v < target ? Math.min(v + maxDelta, target) : Math.max(v - maxDelta, target);

const overlapsX = (x: number, b: ColliderBox) => x + PLAYER_WIDTH / 2 > b.x + SKIN && x - PLAYER_WIDTH / 2 < b.x + b.w - SKIN;
const overlapsY = (y: number, b: ColliderBox) => y + PLAYER_HEIGHT > b.y + SKIN && y < b.y + b.h - SKIN;

/**
 * Advances the player by one fixed step at simulation time `time` (seconds since level start).
 * Mutates `p`, returns the events that happened this step.
 */
export function stepPlayer(
  p: PlayerSim,
  input: InputFrame,
  level: LevelDef,
  time: number,
  dt: number = FIXED_DT,
  s: MovementSettings = DEFAULT_MOVEMENT,
): MotorEvent[] {
  const events: MotorEvent[] = [];
  const before = collidersAt(level, time);
  const after = collidersAt(level, time + dt);

  // --- carry: move with the platform we stand on
  if (p.grounded && p.groundId) {
    const a = before.find((b) => b.id === p.groundId);
    const b = after.find((c) => c.id === p.groundId);
    if (a && b) {
      p.x += b.x - a.x;
      p.y += b.y - a.y;
    }
  }

  // --- timers
  p.timeSinceJumpPressed = input.jumpPressed ? 0 : p.timeSinceJumpPressed + dt;
  p.timeSinceGrounded = p.grounded ? 0 : p.timeSinceGrounded + dt;
  p.landTimer = Math.max(0, p.landTimer - dt);
  p.attackTimer = Math.max(0, p.attackTimer - dt);
  p.attackCooldown = Math.max(0, p.attackCooldown - dt);
  if (!p.grounded && p.jumpsUsed === 0 && p.timeSinceGrounded > s.coyoteTime) p.jumpsUsed = 1; // walked off: ground jump lost

  // --- horizontal
  const move = Math.max(-1, Math.min(1, input.moveX));
  if (move !== 0) p.facing = move > 0 ? 1 : -1;
  const target = move * s.maxRunSpeed;
  const accel = p.grounded ? (move !== 0 ? s.groundAcceleration : s.groundDeceleration) : s.airAcceleration;
  p.vx = approach(p.vx, target, accel * dt);

  // --- jump / double jump (buffered)
  if (p.timeSinceJumpPressed <= s.jumpBufferTime) {
    const canGroundJump = p.jumpsUsed === 0 && (p.grounded || p.timeSinceGrounded <= s.coyoteTime);
    if (canGroundJump) {
      p.vy = jumpVelocity(s);
      p.jumpsUsed = 1;
      events.push('jump');
    } else if (p.jumpsUsed === 1 && s.doubleJumpHeight > 0 && !p.grounded) {
      p.vy = doubleJumpVelocity(s);
      p.jumpsUsed = 2;
      events.push('doubleJump');
    }
    if (events.length > 0) {
      p.grounded = false;
      p.groundId = null;
      p.timeSinceGrounded = Infinity;
      p.timeSinceJumpPressed = Infinity;
      p.jumpCut = false;
    }
  }
  // variable height: releasing jump while rising cuts the jump once
  if (!input.jumpHeld && p.vy > 0 && !p.jumpCut && p.jumpsUsed > 0) {
    p.vy *= s.jumpCutMultiplier;
    p.jumpCut = true;
  }

  // --- gravity
  const g = gravity(s) * (p.vy < 0 ? s.fallGravityMultiplier : 1);
  p.vy = Math.max(p.vy - g * dt, -s.maxFallSpeed);

  // --- move X, resolve against solids (one-way never blocks sideways)
  p.x += p.vx * dt;
  for (const b of after) {
    if (b.kind !== 'solid' || !overlapsY(p.y, b) || !overlapsX(p.x, b)) continue;
    p.x = p.vx > 0 || (p.vx === 0 && p.x < b.x + b.w / 2) ? b.x - PLAYER_WIDTH / 2 : b.x + b.w + PLAYER_WIDTH / 2;
    p.vx = 0;
  }
  const minX = level.bounds.minX + PLAYER_WIDTH / 2;
  const maxX = level.bounds.maxX - PLAYER_WIDTH / 2;
  if (p.x < minX) { p.x = minX; p.vx = 0; }
  if (p.x > maxX) { p.x = maxX; p.vx = 0; }

  // --- move Y, resolve
  const prevY = p.y;
  const prevGroundTop = (() => {
    const g0 = p.groundId ? after.find((b) => b.id === p.groundId) : undefined;
    return g0 ? g0.y + g0.h : -Infinity;
  })();
  p.y += p.vy * dt;
  const wasGrounded = p.grounded;
  const fallSpeed = -p.vy;
  p.grounded = false;
  p.groundId = null;
  for (const b of after) {
    if (!overlapsX(p.x, b)) continue;
    const top = b.y + b.h;
    if (p.vy <= 0) {
      // landing: feet were at/above the top before this step and are now below it
      const wasAbove = prevY >= top - 0.05 || (wasGrounded && Math.abs(prevGroundTop - top) < 0.05);
      if (wasAbove && p.y <= top && p.y > b.y - PLAYER_HEIGHT * 0.5) {
        p.y = top;
        p.vy = 0;
        p.grounded = true;
        p.groundId = b.id;
      } else if (b.kind === 'solid' && overlapsY(p.y, b)) {
        // embedded (e.g. a moving platform pushed into us): exit through the nearer face
        if (p.y >= b.y + b.h / 2) {
          p.y = top;
          p.vy = 0;
          p.grounded = true;
          p.groundId = b.id;
        } else {
          p.y = b.y - PLAYER_HEIGHT;
        }
      }
    } else if (b.kind === 'solid' && overlapsY(p.y, b) && prevY + PLAYER_HEIGHT <= b.y + 0.05) {
      p.y = b.y - PLAYER_HEIGHT; // head bonk
      p.vy = 0;
      events.push('bonk');
    }
  }

  if (p.grounded) {
    p.jumpsUsed = 0;
    p.jumpCut = false;
    if (!wasGrounded) {
      events.push('land');
      if (fallSpeed >= HARD_LANDING_SPEED) p.landTimer = LAND_TIME;
    }
  }

  // --- attack
  if (input.attackPressed && p.attackTimer <= 0 && p.attackCooldown <= 0) {
    p.attackTimer = ATTACK_TIME;
    p.attackCooldown = ATTACK_TIME + ATTACK_COOLDOWN;
    events.push('attack');
  }

  // --- kill plane
  if (p.y < level.bounds.minY) events.push('fellOut');

  // --- state
  const next = resolveState(p, s);
  if (next !== p.state) {
    p.state = next;
    p.stateTime = 0;
  } else {
    p.stateTime += dt;
  }
  return events;
}

function resolveState(p: PlayerSim, s: MovementSettings): PlayerStateKey {
  if (p.attackTimer > 0) return 'Attack';
  if (!p.grounded) {
    if (p.vy > 0) return p.jumpsUsed === 2 ? 'DoubleJump' : 'Jump';
    return 'Fall';
  }
  if (p.landTimer > 0) return 'Land';
  const speed = Math.abs(p.vx);
  if (speed < 0.2) return 'Idle';
  return speed <= s.walkSpeed ? 'Walk' : 'Run';
}

/** Puts a player back at a position with zero velocity (respawn, checkpoint, catch-up). */
export function respawn(p: PlayerSim, x: number, y: number): void {
  Object.assign(p, createPlayer(x, y), { facing: p.facing });
}
