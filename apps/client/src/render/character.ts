import type { CharacterDef } from '@wonderjump/shared';
import { buildChibi, type Chibi } from './chibi.js';
import { buildSpriteChibi } from './spriteChibi.js';

/**
 * Which character visual to use. `art` (default) = owner's concept art as paper cut-outs (D-14);
 * `primitive` = the procedural chibi, kept as a fallback and for comparison (`?chibi=primitive`).
 */
export type CharacterVisualMode = 'art' | 'primitive';

export function visualModeFrom(search: string): CharacterVisualMode {
  return new URLSearchParams(search).get('chibi') === 'primitive' ? 'primitive' : 'art';
}

export function buildCharacter(def: CharacterDef, mode: CharacterVisualMode): Chibi {
  return mode === 'primitive' ? buildChibi(def) : buildSpriteChibi(def);
}
