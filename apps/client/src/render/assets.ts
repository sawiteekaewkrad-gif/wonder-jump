import * as THREE from 'three';
import { PALETTE, type AssetDef } from '@wonderjump/shared';
import { toonMaterial, unitGeometry } from './primitives.js';

const DEG = Math.PI / 180;

/** Builds a placeholder for any catalog asset, applying its pivot rule. */
export function buildAsset(def: AssetDef): THREE.Group {
  const root = new THREE.Group();
  root.name = def.id;
  const visual = new THREE.Group();
  visual.name = 'VisualRoot';
  root.add(visual);

  const mesh = new THREE.Mesh(unitGeometry(def.shape), toonMaterial(PALETTE[def.color]));
  mesh.name = 'Mesh';
  mesh.scale.set(...def.size);
  if (def.rotation) mesh.rotation.set(def.rotation[0] * DEG, def.rotation[1] * DEG, def.rotation[2] * DEG);
  mesh.castShadow = def.category !== 'item' && def.category !== 'platform';
  mesh.receiveShadow = def.category === 'platform';
  visual.add(mesh);

  root.updateMatrixWorld(true);
  const b = new THREE.Box3().setFromObject(visual);
  visual.position.y = def.pivot === 'feet' ? -b.min.y : def.pivot === 'top' ? -b.max.y : -(b.min.y + b.max.y) / 2;

  root.userData.assetId = def.id;
  root.userData.category = def.category;
  return root;
}
