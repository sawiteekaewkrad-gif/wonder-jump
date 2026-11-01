using System;
using UnityEngine;
using WonderJump.Controls;

namespace WonderJump.Player
{
    // ---------------------------------------------------------------------------------
    // Player architecture (Phase 0 = contracts only; implementations arrive in Phase 1).
    //
    //   IPlayerInputSource ──► PlayerInputFrame ──► IPlayerMotor.Simulate()  (pure movement)
    //                                                     │
    //                                                     ▼
    //                                               MovementState ──► IPlayerStateMachine
    //                                                     │                    │
    //                                                     ▼                    ▼
    //                                     Transform / Rigidbody.MovePosition   IPlayerAnimationDriver
    //
    // Networking (Phase 2) wraps this: the owning peer runs input + motor; remote peers only
    // receive the replicated transform + PlayerStateId. Movement never references netcode.
    // ---------------------------------------------------------------------------------

    /// <summary>Simulation state of one player. Plain data so it can be logged, tested and replicated.</summary>
    public struct MovementState
    {
        public Vector3 Position;
        public Vector2 Velocity;
        public bool IsGrounded;
        public Vector3 GroundNormal;
        public float TimeSinceGrounded;
        public float TimeSinceJumpPressed;
        /// <summary>+1 facing right, -1 facing left.</summary>
        public int FacingSign;
    }

    /// <summary>Result of a ground probe below the player's feet.</summary>
    public struct GroundHit
    {
        public bool Hit;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
        public Collider Collider;
    }

    /// <summary>
    /// Ground detection seam. Phase 1 implementation: SphereCast from the capsule bottom against
    /// WonderJumpLayers.GroundMask. Tests can substitute a fake probe.
    /// </summary>
    public interface IGroundProbe
    {
        bool Probe(Vector3 feetPosition, float radius, float maxDistance, out GroundHit hit);
    }

    /// <summary>
    /// Custom kinematic motor (decision D-02): capsule casts + kinematic Rigidbody, Z locked.
    /// Must not read Unity input or netcode directly.
    /// </summary>
    public interface IPlayerMotor
    {
        MovementState State { get; }
        void Simulate(PlayerInputFrame input, float deltaTime);
        /// <summary>Instant move without collision (respawn, checkpoint, co-op leash).</summary>
        void Teleport(Vector3 position);
    }

    /// <summary>Gameplay-level state, replicated to remote peers and used to drive animation.</summary>
    public enum PlayerStateId
    {
        Idle = 0,
        Run = 1,
        Jump = 2,
        Fall = 3,
        Land = 4,
        Hurt = 5,
        KnockedOut = 6,
        Reviving = 7,
        Victory = 8
    }

    public interface IPlayerStateMachine
    {
        PlayerStateId Current { get; }
        /// <summary>(previous, next)</summary>
        event Action<PlayerStateId, PlayerStateId> StateChanged;
        void Evaluate(MovementState movement, float deltaTime);
        /// <summary>Hurt / KO / Victory are pushed by gameplay systems, not derived from movement.</summary>
        void Force(PlayerStateId state);
    }

    /// <summary>
    /// Maps state to visuals. Phase 1: placeholder squash/stretch on VisualRoot.
    /// Phase 7: Humanoid Animator with the shared 9-clip set.
    /// </summary>
    public interface IPlayerAnimationDriver
    {
        void Apply(PlayerStateId state, MovementState movement);
    }
}
