import * as THREE from 'three';
import { createShowcase } from './scenes/showcase.js';

declare const __APP_VERSION__: string;

const app = document.getElementById('app')!;

const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.outputColorSpace = THREE.SRGBColorSpace;
app.appendChild(renderer.domElement);

const showcase = createShowcase();

// ---- HUD
const hud = document.createElement('div');
hud.className = 'hud';
document.body.appendChild(hud);

const hint = document.createElement('div');
hint.className = 'hint';
hint.textContent = 'แตะตัวละครเพื่อดูข้อมูล';
document.body.appendChild(hint);

const card = document.createElement('div');
card.className = 'card hidden';
document.body.appendChild(card);

const rotate = document.createElement('div');
rotate.className = 'rotate';
rotate.innerHTML = '<div class="icon">📱↻</div><div>หมุนโทรศัพท์เป็นแนวนอน<br/>Please rotate to landscape</div>';
document.body.appendChild(rotate);

function resize(): void {
  const w = window.innerWidth;
  const h = window.innerHeight;
  renderer.setSize(w, h, false);
  showcase.resize(w, h);
}
window.addEventListener('resize', resize);
resize();

let cardTimer = 0;
renderer.domElement.addEventListener('pointerdown', (e) => {
  const rect = renderer.domElement.getBoundingClientRect();
  const x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
  const y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
  const def = showcase.pick(x, y);
  if (!def) return;
  card.innerHTML = `<h2>${def.displayName}</h2><div class="title">${def.title} · ${def.signatureItem}</div><p>“${def.tagline}”</p>`;
  card.classList.remove('hidden');
  window.clearTimeout(cardTimer);
  cardTimer = window.setTimeout(() => card.classList.add('hidden'), 3500);
});

// ---- loop
const clock = new THREE.Clock();
let fpsSmoothed = 60;
let hudTimer = 0;

function frame(): void {
  const dt = Math.min(clock.getDelta(), 0.1);
  const t = clock.elapsedTime;
  showcase.update(dt, t);
  renderer.render(showcase.scene, showcase.camera);

  if (dt > 0) fpsSmoothed += (1 / dt - fpsSmoothed) * 0.05;
  hudTimer -= dt;
  if (hudTimer <= 0) {
    hudTimer = 0.5;
    const info = renderer.info.render;
    hud.innerHTML =
      `<strong>Wonder Jump – Adventure Worlds</strong><br/>` +
      `<span class="dim">v${__APP_VERSION__} · Phase 0W showcase</span><br/>` +
      `${Math.round(fpsSmoothed)} FPS · ${info.calls} draws · ${window.innerWidth}×${window.innerHeight}`;
  }
  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);

// Expose a tiny hook for automated screenshot checks.
(window as unknown as { __wj: unknown }).__wj = {
  renderer,
  showcase,
  /** Screen position (CSS px) of a character's head, for automated tap tests. */
  screenOf(key: string): { x: number; y: number } | null {
    const o = showcase.scene.getObjectByName(key);
    if (!o) return null;
    const p = o.getWorldPosition(new THREE.Vector3()).add(new THREE.Vector3(0, 1.4, 0)).project(showcase.camera);
    return { x: ((p.x + 1) / 2) * window.innerWidth, y: ((1 - p.y) / 2) * window.innerHeight };
  },
};
