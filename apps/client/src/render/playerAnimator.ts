import * as THREE from 'three';
import { DEFAULT_MOVEMENT, type PlayerSim } from '@wonderjump/shared';
import type { Chibi } from './chibi.js';
import { toonMaterial } from './primitives.js';
import { isSpriteChibi, mirrorFor, type SpriteChibi } from './spriteChibi.js';

/**
 * Placeholder procedural animation driven by the gameplay state (until the shared GLB clip set,
 * Phase 7). Keeps the 11-state vocabulary visible: idle breathing, walk/run cycle, jump stretch,
 * double-jump front flip, fall, landing squash, attack spin + slash arc.
 */
export class PlayerAnimator {
  private readonly basePos = new Map<THREE.Object3D, THREE.Vector3>();
  private readonly legs: THREE.Object3D[];
  private readonly arms: THREE.Object3D[];
  private readonly slash: THREE.Mesh;
  private yaw = 0;
  private phase = 0;
  /** Feet lift computed by buildChibi; captured ONCE (re-reading it each frame accumulated offsets). */
  private readonly lift: number;

  constructor(private readonly chibi: Chibi) {
    const v = chibi.visual;
    this.lift = v.position.y;
    this.legs = ['Leg_L', 'Leg_R'].map((n) => v.getObjectByName(n)!).filter(Boolean);
    this.arms = ['Arm_L', 'Arm_R'].map((n) => v.getObjectByName(n)!).filter(Boolean);
    for (const o of [...this.legs, ...this.arms]) this.basePos.set(o, o.position.clone());

    this.slash = makeSlash();
    chibi.root.add(this.slash);
  }

  update(p: PlayerSim, dt: number): void {
    const v = this.chibi.visual;
    const lift = this.lift;
    const t = p.stateTime;

    // turn toward the movement direction but keep the face partly visible (side-scroller 3/4 view)
    this.yaw += (p.facing * 0.85 - this.yaw) * Math.min(1, dt * 14);
    v.rotation.set(0, this.yaw, 0);
    v.scale.set(1, 1, 1);
    v.position.set(0, lift, 0);
    for (const [o, base] of this.basePos) {
      o.position.copy(base);
      o.rotation.set(0, 0, 0);
    }
    this.slash.visible = false;

    const speed = Math.abs(p.vx) / DEFAULT_MOVEMENT.maxRunSpeed;
    switch (p.state) {
      case 'Idle': {
        const s = 1 + Math.sin(performance.now() / 320) * 0.025;
        v.scale.set(1 / Math.sqrt(s), s, 1 / Math.sqrt(s));
        break;
      }
      case 'Walk':
      case 'Run': {
        this.phase += dt * (6 + speed * 10);
        const sw = Math.sin(this.phase);
        v.position.y = lift + Math.abs(Math.cos(this.phase)) * 0.07 * (0.5 + speed);
        const [l, r] = this.legs;
        if (l && r) {
          l.position.z += sw * 0.16 * (0.4 + speed);
          r.position.z -= sw * 0.16 * (0.4 + speed);
          l.position.y += Math.max(0, sw) * 0.08;
          r.position.y += Math.max(0, -sw) * 0.08;
        }
        const [al, ar] = this.arms;
        if (al && ar) {
          al.rotation.x = sw * 0.7 * (0.4 + speed);
          ar.rotation.x = -sw * 0.7 * (0.4 + speed);
        }
        v.rotation.z = -p.facing * speed * 0.08; // lean into the run
        break;
      }
      case 'Jump':
        v.scale.set(0.93, 1.1, 0.93);
        this.raiseArms(0.9);
        break;
      case 'DoubleJump': {
        // front flip (pivot at the feet: a little arc reads well at phone size)
        const k = Math.min(1, t / 0.36);
        v.rotation.set(-k * Math.PI * 2, this.yaw, 0, 'YXZ');
        v.position.y = lift + Math.sin(k * Math.PI) * 0.9;
        this.raiseArms(1.4);
        break;
      }
      case 'Fall':
        this.raiseArms(1.8);
        v.scale.set(1.02, 0.98, 1.02);
        break;
      case 'Land':
        v.scale.set(1.18, 0.82, 1.18);
        break;
      case 'Attack': {
        const k = Math.min(1, t / 0.28);
        v.rotation.y = this.yaw + p.facing * k * Math.PI * 2;
        this.slash.visible = k < 1;
        this.slash.position.set(p.facing * 0.35, 1.0, 0.3);
        this.slash.rotation.set(0, 0, p.facing > 0 ? -0.6 + k * 1.2 : Math.PI + 0.6 - k * 1.2);
        (this.slash.material as THREE.MeshToonMaterial).opacity = 0.9 * (1 - k);
        break;
      }
      default:
        break;
    }
  }

  private raiseArms(amount: number): void {
    const [al, ar] = this.arms;
    if (al) al.rotation.z = amount * 0.6;
    if (ar) ar.rotation.z = -amount * 0.6;
  }
}

/**
 * Animation for the paper cut-out characters (D-14). A flat drawing can't swing limbs or yaw,
 * so the same state vocabulary is expressed with screen-plane motion: a paper-turn when changing
 * direction, run bob + waddle, jump stretch, a cartwheel double jump, landing squash and a
 * spin-slash attack.
 */
export class SpriteAnimator {
  private readonly slash: THREE.Mesh;
  private mirror: number;
  private phase = 0;
  private readonly half: number;

  constructor(private readonly chibi: SpriteChibi) {
    this.mirror = mirrorFor(chibi.def.key, 1);
    this.half = chibi.sprite.scale.y / 2;
    this.slash = makeSlash();
    chibi.root.add(this.slash);
  }

  update(p: PlayerSim, dt: number): void {
    const { visual: v, flip, def } = this.chibi;
    const t = p.stateTime;
    // paper turn: the drawing narrows through zero and widens mirrored
    const target = mirrorFor(def.key, p.facing);
    this.mirror += (target - this.mirror) * Math.min(1, dt * 18);
    v.position.set(0, 0, 0);
    v.rotation.set(0, 0, 0);
    v.scale.set(1, 1, 1);
    flip.position.set(0, 0, 0);
    flip.scale.set(this.mirror, 1, 1);
    this.slash.visible = false;

    const speed = Math.abs(p.vx) / DEFAULT_MOVEMENT.maxRunSpeed;
    switch (p.state) {
      case 'Idle': {
        const s = 1 + Math.sin(performance.now() / 320) * 0.025;
        v.scale.set(1 / Math.sqrt(s), s, 1);
        break;
      }
      case 'Walk':
      case 'Run': {
        this.phase += dt * (6 + speed * 10);
        v.position.y = Math.abs(Math.cos(this.phase)) * 0.09 * (0.5 + speed);
        v.rotation.z = Math.sin(this.phase) * 0.07 * (0.4 + speed) - p.facing * speed * 0.1;
        break;
      }
      case 'Jump':
        v.scale.set(0.92, 1.1, 1);
        break;
      case 'DoubleJump': {
        // cartwheel around the body centre (a flat drawing can't front-flip)
        const k = Math.min(1, t / 0.36);
        v.position.y = this.half + Math.sin(k * Math.PI) * 0.6;
        flip.position.y = -this.half;
        v.rotation.z = -p.facing * k * Math.PI * 2;
        break;
      }
      case 'Fall':
        v.scale.set(0.97, 1.04, 1);
        break;
      case 'Land':
        v.scale.set(1.16, 0.84, 1);
        break;
      case 'Attack': {
        const k = Math.min(1, t / 0.28);
        flip.scale.x = this.mirror * Math.cos(k * Math.PI * 2); // paper spin
        this.slash.visible = k < 1;
        this.slash.position.set(p.facing * 0.35, 1.0, 0.3);
        this.slash.rotation.set(0, 0, p.facing > 0 ? -0.6 + k * 1.2 : Math.PI + 0.6 - k * 1.2);
        (this.slash.material as THREE.MeshToonMaterial).opacity = 0.9 * (1 - k);
        break;
      }
      default:
        break;
    }
  }
}

function makeSlash(): THREE.Mesh {
  const arcMat = toonMaterial(0xffd65c).clone();
  arcMat.transparent = true;
  arcMat.opacity = 0.85;
  const m = new THREE.Mesh(new THREE.TorusGeometry(0.9, 0.08, 6, 20, Math.PI * 0.9), arcMat);
  m.visible = false;
  return m;
}

export type CharacterAnimator = PlayerAnimator | SpriteAnimator;

/** Picks the animator matching the character visual. */
export function animatorFor(c: Chibi): CharacterAnimator {
  return isSpriteChibi(c) ? new SpriteAnimator(c) : new PlayerAnimator(c);
}
