/**
 * Who decides shared outcomes (collectibles, enemy AI and damage, checkpoints, level end).
 *
 * Decision D-03 (revised for the web stack, owner choice 2026-11-01): **server-authoritative co-op**.
 *   offline – single player: this device simulates everything, no network needed.
 *   server  – the Node game server: authoritative for every shared event in an online room.
 *   client  – a phone in an online room: predicts its own movement, renders server state,
 *             sends *requests* (e.g. "I touched coin #12"); never decides shared outcomes.
 *
 * Because rules live in this package (TypeScript on both sides), the server runs the SAME code.
 */
export type SimRole = 'offline' | 'server' | 'client';

export function hasSharedAuthority(role: SimRole): boolean {
  return role !== 'client';
}

/** Movement of a player's own character stays owner-predicted in every role (responsive controls). */
export function predictsOwnMovement(role: SimRole): boolean {
  return role !== 'server';
}
