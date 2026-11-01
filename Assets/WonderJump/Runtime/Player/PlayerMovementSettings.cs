using UnityEngine;

namespace WonderJump.Player
{
    /// <summary>
    /// Tuning data for the kinematic player motor. One shared asset for MVP; per-character
    /// overrides are possible later (GDD decides whether characters differ in gameplay).
    /// Jump is designed by height and time-to-apex; gravity and jump velocity are derived.
    /// </summary>
    [CreateAssetMenu(menuName = "Wonder Jump/Player Movement Settings", fileName = "PlayerMovementSettings")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [Header("Run")]
        [Min(0f)] public float maxRunSpeed = 7f;
        [Min(0f)] public float groundAcceleration = 60f;
        [Min(0f)] public float groundDeceleration = 70f;
        [Min(0f)] public float airAcceleration = 40f;

        [Header("Jump")]
        [Min(0.1f)] public float jumpHeight = 3.2f;
        [Min(0.05f)] public float timeToApex = 0.38f;
        [Min(1f)] public float fallGravityMultiplier = 1.6f;
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
        [Min(0f)] public float maxFallSpeed = 20f;

        [Header("Forgiveness")]
        [Min(0f)] public float coyoteTime = 0.10f;
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Header("Collision")]
        [Min(0.001f)] public float skinWidth = 0.02f;
        [Min(0.001f)] public float groundProbeDistance = 0.08f;
        [Range(0f, 89f)] public float maxSlopeAngle = 45f;

        /// <summary>Upward gravity magnitude (positive): g = 2h / t².</summary>
        public float Gravity
        {
            get { return 2f * jumpHeight / (timeToApex * timeToApex); }
        }

        /// <summary>Initial jump velocity: v = 2h / t.</summary>
        public float JumpVelocity
        {
            get { return 2f * jumpHeight / timeToApex; }
        }
    }
}
