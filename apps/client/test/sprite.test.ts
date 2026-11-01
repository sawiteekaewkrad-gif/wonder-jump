import { describe, expect, it } from 'vitest';
import * as THREE from 'three';
import { readFileSync, statSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { CHARACTERS, CHARACTER_ART, UI_ART, createPlayer } from '@wonderjump/shared';
import { buildSpriteChibi, mirrorFor } from '../src/render/spriteChibi.js';
import { SpriteAnimator, animatorFor, PlayerAnimator } from '../src/render/playerAnimator.js';
import { buildChibi } from '../src/render/chibi.js';
import { visualModeFrom } from '../src/render/character.js';

const publicDir = fileURLToPath(new URL('../public/', import.meta.url));
const blank = () => new THREE.Texture();

/** Canvas size from a WebP header (VP8X / VP8L / VP8). */
function webpSize(buf: Buffer): { w: number; h: number } {
  expect(buf.toString('ascii', 0, 4)).toBe('RIFF');
  expect(buf.toString('ascii', 8, 12)).toBe('WEBP');
  const chunk = buf.toString('ascii', 12, 16);
  if (chunk === 'VP8X') return { w: 1 + buf.readUIntLE(24, 3), h: 1 + buf.readUIntLE(27, 3) };
  if (chunk === 'VP8L') {
    const b = buf.readUInt32LE(21);
    return { w: 1 + (b & 0x3fff), h: 1 + ((b >> 14) & 0x3fff) };
  }
  return { w: buf.readUInt16LE(26) & 0x3fff, h: buf.readUInt16LE(28) & 0x3fff };
}

describe('character art files', () => {
  it('ships one small WebP per character whose aspect matches the catalog', () => {
    for (const def of CHARACTERS) {
      const art = CHARACTER_ART[def.key];
      const path = publicDir + art.file;
      expect(statSync(path).size).toBeLessThan(150_000); // mobile budget
      const { w, h } = webpSize(readFileSync(path));
      expect(h).toBe(512);
      expect(w / h).toBeCloseTo(art.aspect, 2);
    }
    expect(statSync(publicDir + UI_ART.logo).size).toBeLessThan(150_000);
  });
});

describe('sprite chibi', () => {
  it('stands on y = 0 with the catalog height, one mesh, silhouette shadow', () => {
    for (const def of CHARACTERS) {
      const c = buildSpriteChibi(def, blank());
      c.root.updateMatrixWorld(true);
      const b = new THREE.Box3().setFromObject(c.visual);
      expect(b.min.y).toBeCloseTo(0, 5);
      expect(b.max.y).toBeCloseTo(CHARACTER_ART[def.key].height, 5);
      let meshes = 0;
      c.visual.traverse((o) => { if ((o as THREE.Mesh).isMesh) meshes++; });
      expect(meshes).toBe(1);
      expect(c.sprite.castShadow).toBe(true);
      expect(c.sprite.customDepthMaterial).toBeInstanceOf(THREE.MeshDepthMaterial);
      expect(c.root.userData.hitbox.height).toBeCloseTo(2.062, 3);
    }
  });

  it('mirrors the drawing toward the facing direction', () => {
    // The concept art is drawn facing right (capes, tails and wings trail to the left).
    for (const def of CHARACTERS) {
      const drawn = CHARACTER_ART[def.key].artFacing;
      expect(mirrorFor(def.key, drawn)).toBe(1); // facing the drawn way: no mirror
      expect(mirrorFor(def.key, -drawn)).toBe(-1); // facing the other way: mirrored
    }
  });

  it('animator settles back to rest after every state and ends up mirrored correctly', () => {
    const c = buildSpriteChibi(CHARACTERS[0]!, blank());
    const anim = animatorFor(c);
    expect(anim).toBeInstanceOf(SpriteAnimator);
    const p = createPlayer(0, 0);
    const states = ['Run', 'Jump', 'DoubleJump', 'Fall', 'Land', 'Attack', 'Walk'] as const;
    for (let i = 0; i < 3000; i++) {
      p.state = states[Math.floor(i / 40) % states.length]!;
      p.stateTime = (i % 40) / 60;
      p.vx = 7;
      p.facing = i % 400 < 200 ? 1 : -1;
      anim.update(p, 1 / 60);
    }
    p.state = 'Fall'; // a state with no positional offset
    p.facing = 1;
    for (let i = 0; i < 60; i++) anim.update(p, 1 / 60);
    expect(c.visual.position.y).toBeCloseTo(0, 6);
    expect(c.visual.rotation.z).toBeCloseTo(0, 6);
    expect(c.flip.position.y).toBeCloseTo(0, 6);
    expect(c.flip.scale.x).toBeCloseTo(mirrorFor('Leon', 1), 3);
  });

  it('keeps the primitive chibi as a fallback (?chibi=primitive)', () => {
    expect(visualModeFrom('')).toBe('art');
    expect(visualModeFrom('?chibi=primitive')).toBe('primitive');
    expect(animatorFor(buildChibi(CHARACTERS[2]!))).toBeInstanceOf(PlayerAnimator);
  });
});
