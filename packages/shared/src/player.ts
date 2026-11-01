/**
 * Player movement tuning and states. Jump is designed by height and time-to-apex; gravity and
 * jump velocity are derived (g = 2h/t², v = 2h/t). Double jump comes from the concept sheets.
 */
export interface MovementSettings {
  maxRunSpeed: number;
  walkSpeed: number;
  groundAcceleration: number;
  groundDeceleration: number;
  airAcceleration: number;
  jumpHeight: number;
  timeToApex: number;
  /** Second jump height (0 disables double jump). */
  doubleJumpHeight: number;
  fallGravityMultiplier: number;
  jumpCutMultiplier: number;
  maxFallSpeed: number;
  coyoteTime: number;
  jumpBufferTime: number;
}

export const DEFAULT_MOVEMENT: Readonly<MovementSettings> = {
  maxRunSpeed: 7,
  walkSpeed: 3,
  groundAcceleration: 60,
  groundDeceleration: 70,
  airAcceleration: 40,
  jumpHeight: 3.2,
  timeToApex: 0.38,
  doubleJumpHeight: 2.2,
  fallGravityMultiplier: 1.6,
  jumpCutMultiplier: 0.5,
  maxFallSpeed: 20,
  coyoteTime: 0.1,
  jumpBufferTime: 0.12,
};

export function gravity(s: MovementSettings): number {
  return (2 * s.jumpHeight) / (s.timeToApex * s.timeToApex);
}

export function jumpVelocity(s: MovementSettings): number {
  return (2 * s.jumpHeight) / s.timeToApex;
}

/** Velocity giving `doubleJumpHeight` under the same gravity: v = sqrt(2 g h). */
export function doubleJumpVelocity(s: MovementSettings): number {
  return Math.sqrt(2 * gravity(s) * s.doubleJumpHeight);
}

/**
 * Gameplay state = animation set from the model sheets (11 clips) + revive for co-op.
 * Values are network-stable.
 */
export const PlayerState = {
  Idle: 0,
  Walk: 1,
  Run: 2,
  Jump: 3,
  DoubleJump: 4,
  Fall: 5,
  Land: 6,
  Attack: 7,
  Hurt: 8,
  Death: 9,
  Victory: 10,
  Reviving: 11,
} as const;
export type PlayerStateKey = keyof typeof PlayerState;

/** One frame of player intent, independent of the input device. */
export interface InputFrame {
  moveX: number; // -1..1
  moveY: number; // -1..1
  jumpPressed: boolean;
  jumpHeld: boolean;
  attackPressed: boolean;
}

export const NO_INPUT: Readonly<InputFrame> = { moveX: 0, moveY: 0, jumpPressed: false, jumpHeld: false, attackPressed: false };
