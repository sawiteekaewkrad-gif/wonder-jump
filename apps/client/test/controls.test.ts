import { describe, expect, it } from 'vitest';
import { stickVector } from '../src/input/controls.js';

describe('Crystal Joystick maths', () => {
  it('ignores small movement inside the dead-zone', () => {
    expect(stickVector(5, 0, 60, 0.15)).toEqual({ x: 0, y: 0, magnitude: 0 });
    expect(stickVector(0, 0, 60, 0.15).magnitude).toBe(0);
  });
  it('rescales after the dead-zone and clamps to 1', () => {
    expect(stickVector(60, 0, 60, 0.15).x).toBeCloseTo(1);
    expect(stickVector(600, 0, 60, 0.15).x).toBeCloseTo(1);
    const half = stickVector(0.575 * 60, 0, 60, 0.15); // raw 0.575 → (0.575-0.15)/0.85 = 0.5
    expect(half.x).toBeCloseTo(0.5, 5);
  });
  it('maps screen-down to negative y (up is positive)', () => {
    expect(stickVector(0, 60, 60, 0.1).y).toBeCloseTo(-1);
    expect(stickVector(0, -60, 60, 0.1).y).toBeCloseTo(1);
  });
  it('keeps direction on diagonals', () => {
    const v = stickVector(60, -60, 60, 0.1);
    expect(v.magnitude).toBeCloseTo(1);
    expect(v.x).toBeCloseTo(Math.SQRT1_2, 5);
    expect(v.y).toBeCloseTo(Math.SQRT1_2, 5);
  });
});
