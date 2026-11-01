import * as THREE from 'three';
import type { Shape } from '@wonderjump/shared';

/**
 * Shared unit geometries (one instance per shape) and cached cel-shaded materials (one per colour).
 * Sharing keeps draw-call setup and memory low on mobile WebViews.
 * Unit sizes match the catalog convention: sphere/box/octahedron 1×1×1, capsule & cylinder 1×2,
 * cone 1×1, torus in the XY plane with outer diameter 1.
 */
const geometries = new Map<Shape, THREE.BufferGeometry>();

export function unitGeometry(shape: Shape): THREE.BufferGeometry {
  let g = geometries.get(shape);
  if (g) return g;
  switch (shape) {
    case 'sphere': g = new THREE.SphereGeometry(0.5, 18, 12); break;
    case 'capsule': g = new THREE.CapsuleGeometry(0.5, 1, 4, 12); break;
    case 'cylinder': g = new THREE.CylinderGeometry(0.5, 0.5, 2, 18); break;
    case 'box': g = new THREE.BoxGeometry(1, 1, 1); break;
    case 'cone': g = new THREE.ConeGeometry(0.5, 1, 18); break;
    case 'torus': g = new THREE.TorusGeometry(0.375, 0.125, 10, 28); break;
    case 'octahedron': g = new THREE.OctahedronGeometry(0.5); break;
  }
  geometries.set(shape, g);
  return g;
}

let gradient: THREE.DataTexture | null = null;

/** 3-band ramp → cute cartoon shading (cheaper than PBR on mobile). */
function toonGradient(): THREE.DataTexture {
  if (gradient) return gradient;
  const data = new Uint8Array([90, 90, 90, 255, 185, 185, 185, 255, 255, 255, 255, 255]);
  gradient = new THREE.DataTexture(data, 3, 1, THREE.RGBAFormat);
  gradient.minFilter = THREE.NearestFilter;
  gradient.magFilter = THREE.NearestFilter;
  gradient.needsUpdate = true;
  return gradient;
}

const materials = new Map<number, THREE.MeshToonMaterial>();

export function toonMaterial(color: number): THREE.MeshToonMaterial {
  let m = materials.get(color);
  if (!m) {
    m = new THREE.MeshToonMaterial({ color, gradientMap: toonGradient() });
    materials.set(color, m);
  }
  return m;
}

export function materialCount(): number {
  return materials.size;
}
