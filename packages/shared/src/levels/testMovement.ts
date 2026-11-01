import type { LevelDef } from '../level.js';

/**
 * Phase 1 movement playground (Green Forest). Each section tests one mechanic:
 *   0–22   flat run + walk
 *   22–26  4 m gap (single jump)
 *   26–40  staircase of blocks (1 m, 2 m, 3 m)
 *   40–52  one-way platforms stacked (jump up through them)
 *   52–60  5 m wall (needs a double jump)
 *   60–76  moving platform over a 12 m pit
 *   76–92  low ceiling tunnel (no head bonk at full run) then goal portal
 * Ground top is y = 0.
 */
export const TEST_MOVEMENT: LevelDef = {
  id: 'TEST_MOVEMENT',
  name: 'Movement Playground',
  world: 'GreenForest',
  spawn: { x: 3, y: 0 },
  bounds: { minX: -2, maxX: 94, minY: -10 },
  solids: [
    { kind: 'solid', style: 'grass', x: -2, y: -6, w: 24, h: 6 },
    { kind: 'solid', style: 'grass', x: 26, y: -6, w: 26, h: 6 },
    // staircase
    { kind: 'solid', style: 'stone', x: 29, y: 0, w: 3, h: 1 },
    { kind: 'solid', style: 'stone', x: 32, y: 0, w: 3, h: 2 },
    { kind: 'solid', style: 'stone', x: 35, y: 0, w: 3, h: 3 },
    // one-way stack
    { kind: 'oneway', style: 'wood', x: 41, y: 2.6, w: 3.5, h: 0.3 },
    { kind: 'oneway', style: 'wood', x: 45, y: 5.0, w: 3.5, h: 0.3 },
    { kind: 'oneway', style: 'wood', x: 41, y: 7.4, w: 3.5, h: 0.3 },
    // tall wall then ground continues
    { kind: 'solid', style: 'stone', x: 52, y: -6, w: 8, h: 11 },
    // landing after the moving platform pit
    { kind: 'solid', style: 'grass', x: 72, y: -6, w: 22, h: 6 },
    // tunnel ceiling
    { kind: 'solid', style: 'stone', x: 78, y: 2.8, w: 6, h: 1.5 },
  ],
  moving: [{ kind: 'solid', x: 60.5, y: 3.5, w: 3, h: 0.5, dx: 8, dy: 0, period: 6 }],
  items: [
    { uid: 'coin-1', type: 'Coin', x: 24, y: 2.5 },
    { uid: 'coin-2', type: 'Coin', x: 33.5, y: 3.5 },
    { uid: 'gem-1', type: 'CrystalGem', x: 42.75, y: 9.2 },
    { uid: 'star-1', type: 'Star', x: 66, y: 6 },
  ],
  goal: { x: 90, y: 0 },
};
