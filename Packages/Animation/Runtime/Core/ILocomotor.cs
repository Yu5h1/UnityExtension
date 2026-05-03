using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Common locomotion abstraction implemented by anything that can be "driven":
    /// boats, NPCs, vehicles, the player. A controller (e.g. <see cref="SplineFollower"/>,
    /// AI brain, VR input) computes a desired world-space velocity vector and calls
    /// <see cref="Drive"/>; the implementer decides how to translate it into actual
    /// motion (momentum, instant rotation, animation parameters, etc.).
    ///
    /// Conventions:
    ///   Drive(velocity) : world-space vector. Direction = where to go.
    ///                     Magnitude = desired speed in metres / second.
    ///                     Vector3.zero = stop.
    ///   Velocity        : actual speed the implementer is achieving this frame
    ///                     (frame-to-frame displacement / dt). Used by Animator
    ///                     blend trees, IK foot planting, audio, etc.
    ///
    /// The single-vector design works uniformly across:
    ///   - CharacterController : cc.Move(v * dt)
    ///   - NPC walking         : face along v, walk forward at v.magnitude
    ///   - Boat / vehicle      : decompose into forward thrust + steer angle
    ///   - Player input        : new Vector3(stickX, 0, stickY) * runSpeed
    ///
    /// Implementers are free to clamp, smooth, ignore Y, or otherwise interpret
    /// the input to fit their physics model. A boat may respond with inertia;
    /// an NPC may translate immediately and feed |Velocity| into a walk animation.
    /// </summary>
    public interface ILocomotor
    {
        /// <summary>Actual world-space velocity this locomotor achieved this frame (m/s).</summary>
        Vector3 Velocity { get; }

        /// <summary>Command: world-space desired velocity. Magnitude = m/s. Zero stops.</summary>
        void Drive(Vector3 velocity);
    }
}
