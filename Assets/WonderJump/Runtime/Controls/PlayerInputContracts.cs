using UnityEngine;

namespace WonderJump.Controls
{
    /// <summary>
    /// One frame of player intent, independent of where it came from
    /// (Crystal Joystick, gamepad, keyboard in editor, network replay, test script).
    /// Movement code only ever sees this struct, never touch or Input System types.
    /// </summary>
    public struct PlayerInputFrame
    {
        /// <summary>Joystick vector, each axis -1..1. 2.5D movement uses X; Y is for menus/ladders later.</summary>
        public Vector2 Move;

        /// <summary>True only on the frame the jump button went down.</summary>
        public bool JumpPressed;

        /// <summary>True while jump is held (variable jump height).</summary>
        public bool JumpHeld;

        /// <summary>True only on the frame the action button went down.</summary>
        public bool ActionPressed;

        public static PlayerInputFrame None
        {
            get { return new PlayerInputFrame(); }
        }
    }

    /// <summary>
    /// Produces input frames. Phase 1 adds: TouchInputSource (Crystal Joystick via Input System
    /// on-screen controls) and an editor keyboard source. Remote players have no input source.
    /// </summary>
    public interface IPlayerInputSource
    {
        PlayerInputFrame ReadFrame();
    }
}
