import * as THREE from 'three';
import { TEST_MOVEMENT, getCharacter, type CharacterKey } from '@wonderjump/shared';
import { createShowcase } from './scenes/showcase.js';
import { createLevelScene, type LevelScene } from './scenes/level.js';
import { Controls } from './input/controls.js';

declare const __APP_VERSION__: string;

const app = document.getElementById('app')!;

const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.outputColorSpace = THREE.SRGBColorSpace;
app.appendChild(renderer.domElement);

// ---------------------------------------------------------------- DOM
const el = (cls: string, html = '') => {
  const d = document.createElement('div');
  d.className = cls;
  d.innerHTML = html;
  document.body.appendChild(d);
  return d;
};
const hud = el('hud');
const hint = el('hint', 'แตะตัวละครเพื่อเลือก');
const card = el('card hidden');
const menuBtn = el('top-btn hidden', '⏸ เมนู');
const clearBox = el('clear hidden');
el('rotate', '<div class="icon">📱↻</div><div>หมุนโทรศัพท์เป็นแนวนอน<br/>Please rotate to landscape</div>');

// ---------------------------------------------------------------- modes
const showcase = createShowcase();
let level: LevelScene | null = null;
let controls: Controls | null = null;
let selected: CharacterKey = 'Leon';

function startLevel(key: CharacterKey): void {
  if (level) return; // idempotent: a focused button + Space/Enter must not restart or leak controls
  (document.activeElement as HTMLElement | null)?.blur();
  selected = key;
  controls = new Controls(document.body, { floating: true, scale: 1, deadzone: 0.15 });
  level = createLevelScene(TEST_MOVEMENT, key, controls);
  level.onEvent = (e) => {
    if (e === 'clear' && level) {
      const s = level.stats;
      clearBox.innerHTML =
        `<h2>🎉 ผ่านด่านทดสอบ!</h2><p>${getCharacter(key).displayName} · ${s.time.toFixed(1)} วินาที · ` +
        `ไอเทม ${s.coins} · ตกหลุม ${s.falls} ครั้ง</p>` +
        `<div class="row"><button data-act="again">เล่นอีกครั้ง</button><button data-act="menu">เมนู</button></div>`;
      clearBox.classList.remove('hidden');
    }
  };
  resize();
  card.classList.add('hidden');
  card.inert = true;
  hint.classList.add('hidden');
  menuBtn.classList.remove('hidden');
}

function toMenu(): void {
  level?.dispose();
  level = null;
  controls?.destroy();
  controls = null;
  clearBox.classList.add('hidden');
  menuBtn.classList.add('hidden');
  hint.classList.remove('hidden');
  (document.activeElement as HTMLElement | null)?.blur();
}

menuBtn.addEventListener('click', toMenu);
clearBox.inert = false;
clearBox.addEventListener('click', (e) => {
  const act = (e.target as HTMLElement).dataset.act;
  if (act === 'again') { toMenu(); startLevel(selected); }
  if (act === 'menu') toMenu();
});

card.addEventListener('click', (e) => {
  if ((e.target as HTMLElement).dataset.act === 'play') startLevel(selected);
});

renderer.domElement.addEventListener('pointerdown', (e) => {
  if (level) return;
  const rect = renderer.domElement.getBoundingClientRect();
  const def = showcase.pick(((e.clientX - rect.left) / rect.width) * 2 - 1, -((e.clientY - rect.top) / rect.height) * 2 + 1);
  if (!def) return;
  selected = def.key;
  card.innerHTML =
    `<h2>${def.displayName}</h2><div class="title">${def.title} · ${def.signatureItem}</div>` +
    `<p>“${def.tagline}”</p><button data-act="play">▶ เล่นด่านทดสอบ</button>`;
  card.classList.remove('hidden');
  card.inert = false;
});

function resize(): void {
  const w = window.innerWidth;
  const h = window.innerHeight;
  renderer.setSize(w, h, false);
  showcase.resize(w, h);
  level?.resize(w, h);
}
window.addEventListener('resize', resize);
resize();

// ---------------------------------------------------------------- loop
const clock = new THREE.Clock();
let fps = 60;
let hudTimer = 0;

function frame(): void {
  const dt = Math.min(clock.getDelta(), 0.1);
  if (level) {
    level.update(dt);
    renderer.render(level.scene, level.camera);
  } else {
    showcase.update(dt, clock.elapsedTime);
    renderer.render(showcase.scene, showcase.camera);
  }
  if (dt > 0) fps += (1 / dt - fps) * 0.05;
  hudTimer -= dt;
  if (hudTimer <= 0) {
    hudTimer = 0.25;
    const draws = renderer.info.render.calls;
    hud.innerHTML = level
      ? `<strong>${getCharacter(selected).displayName}</strong> · ${level.player.state}<br/>` +
        `<span class="dim">⭐ ${level.stats.coins} · ${level.stats.time.toFixed(1)}s · ${Math.round(fps)} FPS</span>`
      : `<strong>Wonder Jump – Adventure Worlds</strong><br/>` +
        `<span class="dim">v${__APP_VERSION__} · Phase 1</span><br/>${Math.round(fps)} FPS · ${draws} draws`;
    hud.classList.toggle('compact', !!level);
  }
  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);

// Hooks for automated checks (Playwright).
(window as unknown as { __wj: unknown }).__wj = {
  renderer,
  showcase,
  get level() { return level; },
  startLevel,
  toMenu,
  screenOf(key: string): { x: number; y: number } | null {
    const o = showcase.scene.getObjectByName(key);
    if (!o) return null;
    const p = o.getWorldPosition(new THREE.Vector3()).add(new THREE.Vector3(0, 1.4, 0)).project(showcase.camera);
    return { x: ((p.x + 1) / 2) * window.innerWidth, y: ((1 - p.y) / 2) * window.innerHeight };
  },
};
