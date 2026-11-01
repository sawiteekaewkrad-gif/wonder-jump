import { hasSharedAuthority, type SimRole } from './authority.js';
import type { CollectibleType } from './catalog.js';

/** One collectible placed in a level. `uid` is unique per level (e.g. "coin-12"). */
export interface CollectibleState {
  uid: string;
  type: CollectibleType;
  value: number;
  collectedBy: string | null;
}

export type CollectResult =
  | { ok: true; uid: string; by: string; type: CollectibleType; value: number }
  | { ok: false; reason: 'not-authoritative' | 'unknown' | 'already-collected' };

/**
 * Collect-once rule (ported from Unity `Collectible.TryCollect`).
 * Only an authoritative role may resolve a pickup; the first valid request wins, every later
 * request (same or other player) is rejected. Mutates `items` in place and returns the outcome
 * the server broadcasts.
 */
export function tryCollect(
  items: Map<string, CollectibleState>,
  uid: string,
  playerId: string,
  role: SimRole,
): CollectResult {
  if (!hasSharedAuthority(role)) return { ok: false, reason: 'not-authoritative' };
  const item = items.get(uid);
  if (!item) return { ok: false, reason: 'unknown' };
  if (item.collectedBy !== null) return { ok: false, reason: 'already-collected' };
  item.collectedBy = playerId;
  return { ok: true, uid, by: playerId, type: item.type, value: item.value };
}

/** Re-arm items (checkpoint reload / level restart). */
export function resetCollectibles(items: Map<string, CollectibleState>): void {
  for (const item of items.values()) item.collectedBy = null;
}
