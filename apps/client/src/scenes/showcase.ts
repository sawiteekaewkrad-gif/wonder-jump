import * as THREE from 'three';
import { ASSETS, CHARACTERS, PLAYABLE_WORLDS, getAsset, type CharacterDef } from '@wonderjump/shared';
import type { Chibi } from '../render/chibi.js';
import { buildCharacter, type CharacterVisualMode } from '../render/character.js';
import { buildAsset } from '../render/assets.js';

/**
 * Phase 0W showcase: all 6 characters, every collectible and every enemy on Green Forest ground.
 * Purpose: check the placeholder assets on a real phone (landscape, colours, proportions, FPS).
 * Tap a character to make it hop and show its card.
 */
export interface Showcase {
  scene: THREE.Scene;
  camera: THREE.PerspectiveCamera;
  update(dt: number, t: number): void;
  resize(width: number, height: number): void;
  pick(ndcX: number, ndcY: number): CharacterDef | null;
}

const VISIBLE_HALF_WIDTH = 10.5; // metres visible either side of centre
const CAMERA_DISTANCE = 15;
const LOOK_AT_Y = 1.7;

export function createShowcase(visualMode: CharacterVisualMode = 'art'): Showcase {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x8cc8ff);
  scene.fog = new THREE.Fog(0x8cc8ff, 24, 46);

  const camera = new THREE.PerspectiveCamera(38, 16 / 9, 0.3, 100);
  camera.position.set(0, 2.6, CAMERA_DISTANCE);
  camera.lookAt(0, LOOK_AT_Y, 0);

  scene.add(new THREE.HemisphereLight(0xdff1ff, 0x6f9a5a, 1.4));
  const sun = new THREE.DirectionalLight(0xffffff, 2.2);
  sun.position.set(-6, 12, 8);
  sun.castShadow = true;
  sun.shadow.mapSize.set(1024, 1024);
  const sc = sun.shadow.camera;
  sc.left = -16; sc.right = 16; sc.top = 8; sc.bottom = -4; sc.near = 1; sc.far = 40;
  scene.add(sun);

  // Earth block under the grass so the lower screen is not empty sky.
  const earth = new THREE.Mesh(new THREE.BoxGeometry(60, 8, 14), new THREE.MeshToonMaterial({ color: 0x8a5a36 }));
  earth.position.set(0, -4.5, -4);
  earth.receiveShadow = true;
  scene.add(earth);
  const lawn = new THREE.Mesh(new THREE.BoxGeometry(60, 0.5, 10), new THREE.MeshToonMaterial({ color: 0x5cc85a }));
  lawn.position.set(0, -0.3, -9);
  lawn.receiveShadow = true;
  scene.add(lawn);

  // Ground rows: front (characters) and back (enemies). Top surface at y = 0.
  for (const z of [0, -3]) {
    for (let i = 0; i < 8; i++) {
      const p = buildAsset(getAsset('Forest_Ground'));
      p.position.set(-14 + i * 4, 0, z);
      scene.add(p);
    }
  }

  // Background decor
  const decor: Array<[string, number, number]> = [
    ['FantasyTree', -12, -6], ['FantasyTree', 7, -7], ['CrystalCluster', 12.5, -5.5], ['MushroomProp', -9.5, -5],
  ];
  for (const [id, x, z] of decor) {
    const d = buildAsset(getAsset(id));
    d.position.set(x, 0, z);
    scene.add(d);
    if (id === 'FantasyTree') {
      const canopy = buildAsset(getAsset('TreeCanopy'));
      canopy.position.set(x, 2.1, z);
      scene.add(canopy);
    }
  }

  // Characters
  const chibis: Chibi[] = CHARACTERS.map((def, i) => {
    const c = buildCharacter(def, visualMode);
    c.root.position.set(-6.25 + i * 2.5, 0, 0);
    scene.add(c.root);
    return c;
  });

  // Collectibles floating above
  const items = ASSETS.filter((a) => a.category === 'item').map((def, i) => {
    const g = buildAsset(def);
    g.position.set(-6.25 + i * 2.5, 3.5, 0);
    scene.add(g);
    return g;
  });

  // Enemies behind, grouped by world order
  let ex = -9.5;
  for (const world of PLAYABLE_WORLDS) {
    for (const def of ASSETS.filter((a) => a.category === 'enemy' && a.world === world)) {
      const e = buildAsset(def);
      e.position.set(ex, 0, -3);
      scene.add(e);
      ex += 3.1;
    }
  }

  const portal = buildAsset(getAsset('PortalFrame'));
  portal.position.set(9.2, 0, 0);
  scene.add(portal);

  const hops = new Map<Chibi, number>(); // chibi -> hop start time
  let now = 0;

  return {
    scene,
    camera,
    update(dt, t) {
      now = t;
      items.forEach((g, i) => {
        g.rotation.y = t * 1.6 + i;
        g.position.y = 3.5 + Math.sin(t * 2.4 + i) * 0.12;
      });
      chibis.forEach((c, i) => {
        // idle breathing (squash & stretch) until real animations exist
        const s = 1 + Math.sin(t * 3 + i * 0.7) * 0.025;
        c.visual.scale.set(1 / Math.sqrt(s), s, 1 / Math.sqrt(s));
        c.visual.rotation.z = Math.sin(t * 1.3 + i) * 0.03; // gentle sway (reads well on cut-outs)
        const start = hops.get(c);
        if (start !== undefined) {
          const k = (t - start) / 0.55;
          if (k >= 1) { hops.delete(c); c.root.position.y = 0; }
          else c.root.position.y = Math.sin(k * Math.PI) * 1.4;
        }
      });
      portal.rotation.z = Math.sin(t * 0.8) * 0.05;
      void dt;
    },
    resize(width, height) {
      const aspect = width / Math.max(1, height);
      camera.aspect = aspect;
      // Keep the whole line-up visible on any landscape ratio.
      const halfH = VISIBLE_HALF_WIDTH / aspect;
      camera.fov = THREE.MathUtils.clamp((2 * Math.atan(halfH / CAMERA_DISTANCE) * 180) / Math.PI, 28, 60);
      camera.updateProjectionMatrix();
    },
    pick(ndcX, ndcY) {
      const ray = new THREE.Raycaster();
      ray.setFromCamera(new THREE.Vector2(ndcX, ndcY), camera);
      const hit = ray.intersectObjects(chibis.map((c) => c.root), true)[0];
      if (!hit) return null;
      const chibi = chibis.find((c) => {
        let o: THREE.Object3D | null = hit.object;
        while (o) { if (o === c.root) return true; o = o.parent; }
        return false;
      });
      if (!chibi) return null;
      if (!hops.has(chibi)) hops.set(chibi, now);
      return chibi.def;
    },
  };
}
