import { NO_INPUT, type InputFrame } from '@wonderjump/shared';

/**
 * Touch + keyboard → InputFrame. Movement code never sees touch events (CLAUDE.md rule).
 *
 * Crystal Virtual Joystick (purple/gold): floating by default — it appears under the thumb
 * anywhere in the left 45% of the screen; fixed mode keeps it in the corner.
 * Right side: Jump (big) and Attack buttons. Multi-touch safe (tracks pointer ids).
 * Keyboard (desktop testing): ←/→ or A/D, Space/W/↑/K = jump, J/X = attack, Shift = walk.
 */

export interface StickVector {
  x: number;
  y: number;
  magnitude: number;
}

/** Radial dead-zone with rescale, clamped to the unit circle. Pure (unit-tested). */
export function stickVector(dx: number, dy: number, radius: number, deadzone: number): StickVector {
  const len = Math.hypot(dx, dy);
  if (radius <= 0 || len === 0) return { x: 0, y: 0, magnitude: 0 };
  const raw = Math.min(len / radius, 1);
  if (raw <= deadzone) return { x: 0, y: 0, magnitude: 0 };
  const magnitude = Math.min(1, (raw - deadzone) / (1 - deadzone));
  return { x: (dx / len) * magnitude, y: (-dy / len) * magnitude, magnitude };
}

export interface ControlsOptions {
  floating: boolean;
  /** Joystick size multiplier (SettingsData.joystickScale). */
  scale: number;
  deadzone: number;
}

export class Controls {
  readonly element: HTMLDivElement;
  private readonly base: HTMLDivElement;
  private readonly knob: HTMLDivElement;
  private readonly zone: HTMLDivElement;
  private stickPointer: number | null = null;
  private origin = { x: 0, y: 0 };
  private stick: StickVector = { x: 0, y: 0, magnitude: 0 };
  private jumpHeldTouch = new Set<number>();
  private jumpPresses = 0;
  private attackPresses = 0;
  private readonly keys = new Set<string>();
  private readonly radius: number;
  private readonly cleanup: Array<() => void> = [];

  constructor(parent: HTMLElement, private readonly options: ControlsOptions = { floating: true, scale: 1, deadzone: 0.15 }) {
    this.radius = 62 * options.scale;
    this.element = div('controls');
    this.zone = div('stick-zone');
    this.base = div('stick-base idle');
    this.knob = div('stick-knob');
    this.base.style.setProperty('--r', `${this.radius}px`);
    this.base.appendChild(this.knob);
    this.zone.appendChild(this.base);

    const jump = div('btn btn-jump');
    jump.innerHTML = '<span>⬆</span>';
    const attack = div('btn btn-attack');
    attack.innerHTML = '<span>⚔</span>';
    this.element.append(this.zone, jump, attack);
    parent.appendChild(this.element);

    this.listen(this.zone, 'pointerdown', (e) => this.onStickDown(e));
    this.listen(this.zone, 'pointermove', (e) => this.onStickMove(e));
    for (const t of ['pointerup', 'pointercancel', 'lostpointercapture'] as const)
      this.listen(this.zone, t, (e) => this.onStickUp(e));

    this.listen(jump, 'pointerdown', (e) => {
      jump.setPointerCapture(e.pointerId);
      this.jumpHeldTouch.add(e.pointerId);
      this.jumpPresses++;
      jump.classList.add('down');
    });
    for (const t of ['pointerup', 'pointercancel', 'lostpointercapture'] as const)
      this.listen(jump, t, (e) => {
        this.jumpHeldTouch.delete(e.pointerId);
        if (this.jumpHeldTouch.size === 0) jump.classList.remove('down');
      });
    this.listen(attack, 'pointerdown', (e) => {
      this.attackPresses++;
      attack.classList.add('down');
      window.setTimeout(() => attack.classList.remove('down'), 120);
      e.preventDefault();
    });

    const onKeyDown = (e: KeyboardEvent) => {
      if (!e.repeat) {
        if (isJumpKey(e.code)) this.jumpPresses++;
        if (isAttackKey(e.code)) this.attackPresses++;
      }
      this.keys.add(e.code);
    };
    const onKeyUp = (e: KeyboardEvent) => this.keys.delete(e.code);
    const onBlur = () => this.keys.clear();
    window.addEventListener('keydown', onKeyDown);
    window.addEventListener('keyup', onKeyUp);
    window.addEventListener('blur', onBlur);
    this.cleanup.push(() => {
      window.removeEventListener('keydown', onKeyDown);
      window.removeEventListener('keyup', onKeyUp);
      window.removeEventListener('blur', onBlur);
    });
    this.placeIdle();
  }

  /** One InputFrame per fixed simulation step; button presses are consumed exactly once. */
  sample(): InputFrame {
    const k = this.keys;
    let moveX = this.stick.x;
    let moveY = this.stick.y;
    const kx = (k.has('ArrowRight') || k.has('KeyD') ? 1 : 0) - (k.has('ArrowLeft') || k.has('KeyA') ? 1 : 0);
    if (kx !== 0) moveX = kx * (k.has('ShiftLeft') || k.has('ShiftRight') ? 0.35 : 1);
    const ky = (k.has('ArrowUp') ? 1 : 0) - (k.has('ArrowDown') || k.has('KeyS') ? 1 : 0);
    if (ky !== 0) moveY = ky;
    const frame: InputFrame = {
      ...NO_INPUT,
      moveX,
      moveY,
      jumpPressed: this.jumpPresses > 0,
      jumpHeld: this.jumpHeldTouch.size > 0 || [...k].some(isJumpKey),
      attackPressed: this.attackPresses > 0,
    };
    this.jumpPresses = 0;
    this.attackPresses = 0;
    return frame;
  }

  destroy(): void {
    for (const c of this.cleanup) c();
    this.element.remove();
  }

  // ------------------------------------------------------------------ stick

  private onStickDown(e: PointerEvent): void {
    if (this.stickPointer !== null) return;
    this.stickPointer = e.pointerId;
    this.zone.setPointerCapture(e.pointerId);
    const r = this.zone.getBoundingClientRect();
    if (this.options.floating) {
      this.origin = { x: e.clientX - r.left, y: e.clientY - r.top };
    } else {
      this.origin = this.idleOrigin();
    }
    this.base.classList.remove('idle');
    this.positionBase();
    this.onStickMove(e);
  }

  private onStickMove(e: PointerEvent): void {
    if (e.pointerId !== this.stickPointer) return;
    const r = this.zone.getBoundingClientRect();
    const dx = e.clientX - r.left - this.origin.x;
    const dy = e.clientY - r.top - this.origin.y;
    this.stick = stickVector(dx, dy, this.radius, this.options.deadzone);
    const len = Math.hypot(dx, dy);
    const k = len > this.radius ? this.radius / len : 1;
    this.knob.style.transform = `translate(${dx * k}px, ${dy * k}px) rotate(45deg)`;
  }

  private onStickUp(e: PointerEvent): void {
    if (e.pointerId !== this.stickPointer) return;
    this.stickPointer = null;
    this.stick = { x: 0, y: 0, magnitude: 0 };
    this.knob.style.transform = 'translate(0px, 0px) rotate(45deg)';
    this.placeIdle();
  }

  private idleOrigin(): { x: number; y: number } {
    const r = this.zone.getBoundingClientRect();
    return { x: this.radius + 36, y: r.height - this.radius - 30 };
  }

  private placeIdle(): void {
    this.origin = this.idleOrigin();
    this.base.classList.add('idle');
    this.positionBase();
  }

  private positionBase(): void {
    this.base.style.left = `${this.origin.x - this.radius}px`;
    this.base.style.top = `${this.origin.y - this.radius}px`;
  }

  private listen<K extends keyof HTMLElementEventMap>(el: HTMLElement, type: K, fn: (e: HTMLElementEventMap[K]) => void): void {
    el.addEventListener(type, fn as EventListener);
    this.cleanup.push(() => el.removeEventListener(type, fn as EventListener));
  }
}

const isJumpKey = (c: string) => c === 'Space' || c === 'KeyW' || c === 'ArrowUp' || c === 'KeyK';
const isAttackKey = (c: string) => c === 'KeyJ' || c === 'KeyX';

function div(className: string): HTMLDivElement {
  const d = document.createElement('div');
  d.className = className;
  return d;
}
