using UnityEngine;

namespace WonderJump.CameraSystem
{
    /// <summary>
    /// Camera seam. Phase 1: single follow target. Phase 2: co-op framing (decision D-05, OPEN).
    /// Implementation will wrap Cinemachine; gameplay code only talks to this interface.
    /// Note: generated characters face -Z, so the gameplay camera sits at -Z looking toward +Z.
    /// </summary>
    public interface ICameraTargetGroup
    {
        void AddTarget(Transform target, float weight);
        void RemoveTarget(Transform target);
        /// <summary>World-space bounds the camera must stay inside (level edges).</summary>
        void SetLevelBounds(Bounds bounds);
    }
}
