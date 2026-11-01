import * as THREE from 'three';
import {
  ACCESSORY_PARTS, CHIBI_BODY, PALETTE, PLAYER_HITBOX,
  type CharacterDef, type PartColor, type PartDef,
} from '@wonderjump/shared';
import { toonMaterial, unitGeometry } from './primitives.js';

/**
 * Placeholder chibi built from primitives (same layout as the Unity Phase 0 prefabs).
 * Hierarchy mirrors the old prefab layout so final GLB art can replace `visual` later:
 *   root (gameplay: position, hitbox in userData)
 *   └ visual (meshes only; lifted so the feet sit on y = 0)
 */
export interface Chibi {
  root: THREE.Group;
  visual: THREE.Group;
  def: CharacterDef;
}

function resolveColor(def: CharacterDef, slot: PartColor): number {
  switch (slot) {
    case 'outfit': return def.outfit;
    case 'outfitAccent': return def.outfitAccent;
    case 'hair': return def.hair;
    case 'eyes': return def.eyes;
    default: return PALETTE[slot];
  }
}

function addPart(parent: THREE.Object3D, def: CharacterDef, part: PartDef): THREE.Mesh {
  const mesh = new THREE.Mesh(unitGeometry(part.shape), toonMaterial(resolveColor(def, part.color)));
  mesh.name = part.name;
  mesh.position.set(...part.position);
  mesh.scale.set(...part.scale);
  mesh.castShadow = !!part.castShadow;
  mesh.receiveShadow = false;
  mesh.userData.core = !!part.core;
  parent.add(mesh);
  return mesh;
}

export function buildChibi(def: CharacterDef): Chibi {
  const root = new THREE.Group();
  root.name = def.key;
  const visual = new THREE.Group();
  visual.name = 'VisualRoot';
  root.add(visual);

  for (const part of CHIBI_BODY) addPart(visual, def, part);
  for (const part of ACCESSORY_PARTS[def.accessory]) addPart(visual, def, part);

  // Feet on y = 0, computed from the core silhouette (accessories excluded).
  root.updateMatrixWorld(true);
  const core = new THREE.Box3();
  visual.traverse((o) => {
    if ((o as THREE.Mesh).isMesh && o.userData.core) core.expandByObject(o);
  });
  visual.position.y = -core.min.y;

  root.userData.characterKey = def.key;
  root.userData.hitbox = { ...PLAYER_HITBOX };
  return { root, visual, def };
}

/** World-space bounds of the visual (for tests and camera framing). */
export function visualBounds(chibi: Chibi): THREE.Box3 {
  chibi.root.updateMatrixWorld(true);
  return new THREE.Box3().setFromObject(chibi.visual);
}
