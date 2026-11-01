import * as THREE from 'three';
import { CHARACTER_ART, PLAYER_HITBOX, type CharacterDef, type CharacterKey } from '@wonderjump/shared';
import type { Chibi } from './chibi.js';

/**
 * Interim character visual (D-14): the owner's concept art as a 2.5D paper cut-out.
 *   root   (gameplay position, hitbox in userData — same contract as the primitive chibi)
 *   └ visual  (animated: bob, squash, tilt, flips; feet at y = 0)
 *     └ flip  (scale.x = ±1 mirrors the drawing toward the facing direction)
 *       └ Sprite (unlit textured plane, alpha-tested, casts a silhouette shadow)
 * One draw call per character (the primitive chibi used ~15).
 */
export interface SpriteChibi extends Chibi {
  kind: 'sprite';
  flip: THREE.Group;
  sprite: THREE.Mesh;
}

const textures = new Map<CharacterKey, THREE.Texture>();
const plane = new THREE.PlaneGeometry(1, 1).translate(0, 0.5, 0); // bottom-centre pivot

/** Cached texture per character (loaded once, shared by menu and level). */
export function characterTexture(key: CharacterKey): THREE.Texture {
  let t = textures.get(key);
  if (!t) {
    t = new THREE.TextureLoader().load(CHARACTER_ART[key].file);
    t.colorSpace = THREE.SRGBColorSpace;
    t.anisotropy = 4;
    textures.set(key, t);
  }
  return t;
}

export function buildSpriteChibi(def: CharacterDef, texture: THREE.Texture = characterTexture(def.key)): SpriteChibi {
  const art = CHARACTER_ART[def.key];
  const root = new THREE.Group();
  root.name = def.key;
  const visual = new THREE.Group();
  visual.name = 'VisualRoot';
  const flip = new THREE.Group();
  flip.name = 'Flip';

  const material = new THREE.MeshBasicMaterial({ map: texture, alphaTest: 0.5, side: THREE.DoubleSide });
  const sprite = new THREE.Mesh(plane, material);
  sprite.name = 'Sprite';
  sprite.scale.set(art.height * art.aspect, art.height, 1);
  sprite.castShadow = true;
  // silhouette shadow instead of a rectangle
  sprite.customDepthMaterial = new THREE.MeshDepthMaterial({
    depthPacking: THREE.RGBADepthPacking, map: texture, alphaTest: 0.5, side: THREE.DoubleSide,
  });
  sprite.userData.core = true;

  flip.add(sprite);
  visual.add(flip);
  root.add(visual);
  root.userData.characterKey = def.key;
  root.userData.hitbox = { ...PLAYER_HITBOX };
  return { kind: 'sprite', root, visual, flip, sprite, def };
}

export function isSpriteChibi(c: Chibi): c is SpriteChibi {
  return (c as SpriteChibi).kind === 'sprite';
}

/** Mirror sign for the flip group so the drawing looks toward `facing` (+1 right / -1 left). */
export function mirrorFor(key: CharacterKey, facing: number): 1 | -1 {
  return (facing >= 0 ? 1 : -1) === CHARACTER_ART[key].artFacing ? 1 : -1;
}
