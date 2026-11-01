import * as THREE from 'three';
import {
  FIXED_DT, PALETTE, PLAYER_HEIGHT, PLAYER_WIDTH, createPlayer, getAsset, getCharacter, movingPosition,
  respawn, stepPlayer, tryCollect, collidersAt,
  type CharacterKey, type CollectibleState, type LevelDef, type MotorEvent, type PlayerSim,
} from '@wonderjump/shared';
import { buildAsset } from '../render/assets.js';
import { buildChibi } from '../render/chibi.js';
import { PlayerAnimator } from '../render/playerAnimator.js';
import { toonMaterial } from '../render/primitives.js';
import type { Controls } from '../input/controls.js';

/**
 * Playable level (Phase 1: movement & controls, offline role).
 * Simulation runs at a fixed 60 Hz with the shared motor; rendering interpolates between steps,
 * so 90/120 Hz phones stay smooth and the server (Phase 2) can run the identical simulation.
 */
export interface LevelScene {
  scene: THREE.Scene;
  camera: THREE.PerspectiveCamera;
  player: PlayerSim;
  update(frameDt: number): void;
  resize(width: number, height: number): void;
  dispose(): void;
  readonly stats: { coins: number; time: number; cleared: boolean; falls: number };
  /** Debug/test hook: colliders at the current simulation time. */
  colliders(): ReturnType<typeof collidersAt>;
  onEvent?: (e: MotorEvent | 'collect' | 'clear') => void;
}

const CAM_DIST = 13;

const STYLE_COLORS: Record<string, { top: number; body: number; depth: number }> = {
  grass: { top: 0x5cc85a, body: 0x8a5a36, depth: 3 },
  stone: { top: PALETTE.stone, body: 0x5a6078, depth: 3 },
  wood: { top: 0xb07a45, body: 0x8a5a33, depth: 2 },
  gold: { top: PALETTE.gold, body: 0xd99a1f, depth: 2.2 },
};

function boxMesh(w: number, h: number, d: number, color: number): THREE.Mesh {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), toonMaterial(color));
  m.receiveShadow = true;
  return m;
}

/** A level box: body + coloured top slab, positioned by its bottom-left corner. */
function buildBlock(x: number, y: number, w: number, h: number, style: string): THREE.Group {
  const c = STYLE_COLORS[style] ?? STYLE_COLORS.grass!;
  const g = new THREE.Group();
  const topH = Math.min(0.3, h);
  const body = boxMesh(w, h - topH + 0.001, c.depth, c.body);
  body.position.set(w / 2, (h - topH) / 2, 0);
  const top = boxMesh(w, topH, c.depth + 0.02, c.top);
  top.position.set(w / 2, h - topH / 2, 0);
  g.add(body, top);
  g.position.set(x, y, 0);
  return g;
}

export function createLevelScene(level: LevelDef, characterKey: CharacterKey, controls: Controls): LevelScene {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x8cc8ff);
  scene.fog = new THREE.Fog(0x8cc8ff, 30, 70);

  const camera = new THREE.PerspectiveCamera(40, 16 / 9, 0.3, 120);

  scene.add(new THREE.HemisphereLight(0xdff1ff, 0x6f9a5a, 1.4));
  const sun = new THREE.DirectionalLight(0xffffff, 2.2);
  sun.castShadow = true;
  sun.shadow.mapSize.set(1024, 1024);
  Object.assign(sun.shadow.camera, { left: -14, right: 14, top: 10, bottom: -6, near: 1, far: 50 });
  scene.add(sun, sun.target);

  // --- level geometry
  for (const s of level.solids) {
    const style = s.style ?? (s.kind === 'oneway' ? 'wood' : 'grass');
    scene.add(buildBlock(s.x, s.y, s.w, s.h, style));
  }
  const movers = level.moving.map((m) => {
    const g = buildBlock(m.x, m.y, m.w, m.h, 'gold');
    scene.add(g);
    return { def: m, g };
  });
  // backdrop: lawn + decor
  // Backdrop lawn sits far behind and LOW, so pits read as pits (they show a dark chasm, not grass).
  const spanX = level.bounds.maxX - level.bounds.minX + 60;
  const lawn = boxMesh(spanX, 0.4, 10, 0x9fd6a8); // paler = reads as distant, not walkable
  lawn.position.set((level.bounds.minX + level.bounds.maxX) / 2, -0.6, -11);
  const chasm = boxMesh(spanX, 0.2, 6, 0x3b2a4a);
  chasm.position.set(lawn.position.x, level.bounds.minY + 2, -2);
  scene.add(lawn, chasm);
  for (let x = level.bounds.minX + 4; x < level.bounds.maxX; x += 9) {
    const tree = buildAsset(getAsset('FantasyTree'));
    tree.position.set(x + (x % 5), -0.4, -8.5 - (x % 3));
    const canopy = buildAsset(getAsset('TreeCanopy'));
    canopy.position.set(tree.position.x, 2.1, tree.position.z);
    scene.add(tree, canopy);
  }

  // --- items (offline role: this device is the authority → shared tryCollect)
  const items = new Map<string, CollectibleState>();
  const itemMeshes = new Map<string, THREE.Object3D>();
  for (const it of level.items) {
    const def = getAsset(it.type);
    items.set(it.uid, { uid: it.uid, type: it.type, value: def.item?.value ?? 1, collectedBy: null });
    const g = buildAsset(def);
    g.position.set(it.x, it.y, 0);
    scene.add(g);
    itemMeshes.set(it.uid, g);
  }

  const portal = buildAsset(getAsset('PortalFrame'));
  portal.position.set(level.goal.x, level.goal.y, 0);
  scene.add(portal);

  // --- player
  const chibi = buildChibi(getCharacter(characterKey));
  scene.add(chibi.root);
  const animator = new PlayerAnimator(chibi);
  const player = createPlayer(level.spawn.x, level.spawn.y);
  const prev = { x: player.x, y: player.y };

  const stats = { coins: 0, time: 0, cleared: false, falls: 0 };
  let simTime = 0;
  let acc = 0;
  let blink = 0;
  const camLook = new THREE.Vector3(player.x, player.y + 1.4, 0);
  let lookAhead = 0;

  const self: LevelScene = {
    scene,
    camera,
    player,
    stats,
    update(frameDt) {
      acc += Math.min(frameDt, 0.25);
      while (acc >= FIXED_DT) {
        acc -= FIXED_DT;
        prev.x = player.x;
        prev.y = player.y;
        const input = stats.cleared ? { ...controls.sample(), moveX: 0, jumpPressed: false, attackPressed: false } : controls.sample();
        const events = stepPlayer(player, input, level, simTime);
        simTime += FIXED_DT;
        if (!stats.cleared) stats.time += FIXED_DT;
        for (const e of events) {
          if (e === 'fellOut') {
            stats.falls++;
            respawn(player, level.spawn.x, level.spawn.y);
            prev.x = player.x;
            prev.y = player.y;
            blink = 1;
          }
          self.onEvent?.(e);
        }
        // pickups: player box vs item (0.5 m) overlap
        for (const [uid, mesh] of itemMeshes) {
          if (!mesh.visible) continue;
          if (Math.abs(mesh.position.x - player.x) < PLAYER_WIDTH / 2 + 0.35 &&
              mesh.position.y > player.y - 0.35 && mesh.position.y < player.y + PLAYER_HEIGHT + 0.35) {
            const r = tryCollect(items, uid, 'p1', 'offline');
            if (r.ok) {
              mesh.visible = false;
              stats.coins += r.value;
              self.onEvent?.('collect');
            }
          }
        }
        if (!stats.cleared && player.grounded && Math.abs(player.x - level.goal.x) < 1) {
          stats.cleared = true;
          self.onEvent?.('clear');
        }
      }

      // --- render state (interpolated)
      const a = acc / FIXED_DT;
      const rx = prev.x + (player.x - prev.x) * a;
      const ry = prev.y + (player.y - prev.y) * a;
      chibi.root.position.set(rx, ry, 0);
      animator.update(player, frameDt);
      blink = Math.max(0, blink - frameDt);
      chibi.root.visible = blink === 0 || Math.floor(blink * 12) % 2 === 0;

      movers.forEach(({ def, g }) => {
        const pos = movingPosition(def, simTime - FIXED_DT + acc);
        g.position.set(pos.x, pos.y, 0);
      });
      const t = simTime;
      itemMeshes.forEach((m, uid) => {
        m.rotation.y = t * 2 + uid.length;
      });
      portal.rotation.y = Math.sin(t) * 0.25;

      // --- camera: look-ahead in the facing direction, smooth follow, clamp to level,
      //     and hard constraints so the player can never leave the frame (fast double jumps).
      lookAhead += (player.facing * 2.2 - lookAhead) * Math.min(1, frameDt * 2.5);
      const halfH = Math.tan(THREE.MathUtils.degToRad(camera.fov / 2)) * CAM_DIST;
      const halfW = halfH * camera.aspect;
      const tx = THREE.MathUtils.clamp(rx + lookAhead, level.bounds.minX + halfW, level.bounds.maxX - halfW);
      const ty = Math.max(ry, -1) + 1.4;
      camLook.x += (tx - camLook.x) * Math.min(1, frameDt * 5);
      camLook.y += (ty - camLook.y) * Math.min(1, frameDt * (ry > camLook.y ? 6 : 3));
      camLook.x = THREE.MathUtils.clamp(camLook.x, rx - halfW * 0.55, rx + halfW * 0.55);
      // lower bound keeps head + flip arc (0.9) + camera tilt + margin inside the top edge
      camLook.y = THREE.MathUtils.clamp(camLook.y, ry + PLAYER_HEIGHT + 1.9 - halfH, ry + halfH * 0.35);
      camera.position.set(camLook.x, camLook.y + 0.9, CAM_DIST);
      camera.lookAt(camLook);
      sun.position.set(camLook.x - 6, camLook.y + 12, 8);
      sun.target.position.set(camLook.x, camLook.y - 1, 0);
    },
    colliders() {
      return collidersAt(level, simTime);
    },
    resize(width, height) {
      camera.aspect = width / Math.max(1, height);
      camera.updateProjectionMatrix();
    },
    dispose() {
      scene.traverse((o) => {
        const m = o as THREE.Mesh;
        if (m.isMesh && m.geometry.type === 'BoxGeometry') m.geometry.dispose();
      });
    },
  };
  return self;
}
