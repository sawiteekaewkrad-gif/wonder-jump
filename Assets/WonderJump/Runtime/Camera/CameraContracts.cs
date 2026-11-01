using UnityEngine;

namespace WonderJump.CameraSystem
{
    /// <summary>
    /// Camera seam. Decision D-05: each phone follows ONLY its local player (single target, also in co-op).
    /// Separation is handled by the partner indicator and the host-decided catch-up rule (Blueprint 4.5).
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
