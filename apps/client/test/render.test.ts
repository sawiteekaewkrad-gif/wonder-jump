import { describe, expect, it } from 'vitest';
import * as THREE from 'three';
import { ASSETS, CHARACTERS, CHIBI_BODY, ACCESSORY_PARTS, getAsset } from '@wonderjump/shared';
import { buildChibi, visualBounds } from '../src/render/chibi.js';
import { buildAsset } from '../src/render/assets.js';
import { unitGeometry } from '../src/render/primitives.js';

const bounds = (o: THREE.Object3D) => {
  o.updateMatrixWorld(true);
  return new THREE.Box3().setFromObject(o);
};

describe('chibi builder', () => {
  it('builds all six characters with body + accessory parts and feet on the ground', () => {
    for (const def of CHARACTERS) {
      const c = buildChibi(def);
      let meshes = 0;
      c.visual.traverse((o) => { if ((o as THREE.Mesh).isMesh) meshes++; });
      expect(meshes).toBe(CHIBI_BODY.length + ACCESSORY_PARTS[def.accessory].length);
      const core = new THREE.Box3();
      c.root.updateMatrixWorld(true);
      c.visual.traverse((o) => { if ((o as THREE.Mesh).isMesh && o.userData.core) core.expandByObject(o); });
      expect(core.min.y).toBeCloseTo(0, 3); // feet exactly on the floor (v1 sank 0.18)
      expect(c.visual.position.y).toBeCloseTo(0.18, 2);
      const b = visualBounds(c);
      expect(b.max.y).toBeGreaterThan(2); // chibi head + accessory
      expect(b.max.y).toBeLessThan(3);
      expect(c.root.userData.hitbox.height).toBeLessThanOrEqual(core.max.y - core.min.y + 1e-3);
    }
  });

  it('shares geometry between characters (mobile memory)', () => {
    const a = buildChibi(CHARACTERS[0]!);
    const b = buildChibi(CHARACTERS[1]!);
    const head = (c: ReturnType<typeof buildChibi>) => (c.visual.getObjectByName('Head') as THREE.Mesh).geometry;
    expect(head(a)).toBe(head(b));
  });
});

describe('asset builder pivots', () => {
  it('puts platform tops, enemy feet and item centres at y = 0', () => {
    for (const def of ASSETS) {
      const b = bounds(buildAsset(def));
      if (def.pivot === 'top') expect(b.max.y).toBeCloseTo(0, 3);
      if (def.pivot === 'feet') expect(b.min.y).toBeCloseTo(0, 3);
      if (def.pivot === 'center') expect((b.min.y + b.max.y) / 2).toBeCloseTo(0, 3);
    }
  });

  it('stands the coin upright facing the camera (+Z)', () => {
    const b = bounds(buildAsset(getAsset('Coin')));
    const size = b.getSize(new THREE.Vector3());
    // 18-sided cylinder: flat-to-flat width is 0.45·cos(10°) ≈ 0.443
    expect(Math.abs(size.x - 0.45)).toBeLessThan(0.01);
    expect(Math.abs(size.y - 0.45)).toBeLessThan(0.01);
    expect(size.z).toBeCloseTo(0.16, 2);
  });

  it('uses unit-sized primitives', () => {
    for (const shape of ['sphere', 'box', 'octahedron'] as const) {
      unitGeometry(shape).computeBoundingBox();
      const s = unitGeometry(shape).boundingBox!.getSize(new THREE.Vector3());
      expect(Math.max(s.x, s.y, s.z)).toBeCloseTo(1, 3);
    }
    unitGeometry('capsule').computeBoundingBox();
    expect(unitGeometry('capsule').boundingBox!.getSize(new THREE.Vector3()).y).toBeCloseTo(2, 3);
  });
});
