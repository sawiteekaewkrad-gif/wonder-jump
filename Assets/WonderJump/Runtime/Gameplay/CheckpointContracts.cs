using System;
using UnityEngine;

namespace WonderJump.Gameplay
{
    /// <summary>
    /// Checkpoint seam (Phase 4). Activation is decided by the authoritative peer and
    /// broadcast, so both co-op players respawn at the same checkpoint.
    /// </summary>
    public interface ICheckpointService
    {
        /// <summary>-1 = level start.</summary>
        int ActiveIndex { get; }
        event Action<int> CheckpointActivated;
        /// <summary>Returns false if index is unknown or not ahead of the active checkpoint.</summary>
        bool TryActivate(int index);
        /// <summary>playerSlot 0 or 1; slot offsets keep co-op players from spawning inside each other.</summary>
        Vector3 GetRespawnPosition(int playerSlot);
    }
}
