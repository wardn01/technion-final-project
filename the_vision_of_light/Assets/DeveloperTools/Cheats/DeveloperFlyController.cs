using UnityEngine;
using VisionOfLight.Player;

namespace VisionOfLight.DeveloperTools
{
    /// <summary>
    /// Smooth camera-relative 3D flight for the developer Fly Mode (F7).
    /// Lives on the "DeveloperCheats" object next to <see cref="DeveloperCheatsManager"/>.
    ///
    /// Controls while flying:
    ///   W/S = forward/back along the camera look direction (pitch included),
    ///   A/D = strafe, Space = up, LeftCtrl = down, LeftShift = fast.
    ///
    /// Movement is applied through the player's CharacterController, so normal
    /// collision stays active and the player slides along obstacles.
    /// Compiled to a no-op stub in release builds.
    /// </summary>
    public class DeveloperFlyController : MonoBehaviour
    {
        [Header("Flight Speeds (m/s)")]
        [SerializeField] private float normalFlySpeed = 12f;
        [SerializeField] private float fastFlySpeed = 30f;
        [SerializeField] private float verticalFlySpeed = 10f;

        [Header("Smoothing")]
        [Tooltip("How fast the fly velocity ramps up (m/s^2).")]
        [SerializeField] private float acceleration = 30f;
        [Tooltip("How fast the fly velocity ramps down when keys are released (m/s^2).")]
        [SerializeField] private float deceleration = 35f;
        [Tooltip("How fast the character body turns toward the movement direction.")]
        [SerializeField] private float rotationSpeed = 12f;

        public static DeveloperFlyController Instance { get; private set; }

        /// <summary>
        /// Called once per frame from PlayerMovement.Update. Returns true when Fly Mode
        /// fully handled the player's movement this frame (normal movement must skip).
        /// Always returns false in release builds and while cheats are disabled.
        /// </summary>
        public static bool TryHandleFlight(PlayerMovement player)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DeveloperFlyController fly = Instance;

            if (fly == null || player == null || player.isSwimming
                || !DeveloperCheatsManager.IsFlyModeEnabled)
            {
                if (fly != null && fly.wasFlying)
                {
                    // Exiting fly mode: drop all flying momentum; gravity resumes from zero.
                    fly.currentVelocity = Vector3.zero;
                    fly.wasFlying = false;
                    if (player != null)
                        player.ResetVelocity();
                }
                return false;
            }

            fly.HandleFlight(player);
            return true;
#else
            return false;
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private Vector3 currentVelocity;
        private bool wasFlying;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void HandleFlight(PlayerMovement player)
        {
            if (!wasFlying)
            {
                // Entering fly mode: stop falling instantly, keep current position.
                wasFlying = true;
                currentVelocity = Vector3.zero;
                player.ResetVelocity();
            }

            // No carry-over fall damage from flight altitude.
            player.ResetFallDamage();

            float dt = Time.deltaTime;
            Vector3 desired = ComputeDesiredVelocity(player);

            // Smooth acceleration / deceleration (no snapping, no per-frame speed growth).
            float rate = desired.sqrMagnitude > 0.001f ? acceleration : deceleration;
            currentVelocity = Vector3.MoveTowards(currentVelocity, desired, Mathf.Max(1f, rate) * dt);

            CharacterController controller = player.controller;
            if (controller != null && controller.enabled)
                controller.Move(currentVelocity * dt);   // CharacterController keeps collision.

            RotateBody(player, dt);
            ApplyNeutralAnimation(player, dt);
        }

        private Vector3 ComputeDesiredVelocity(PlayerMovement player)
        {
            // WASD via the project's input manager (same values normal movement uses).
            float x = PlayerInputManager.Instance != null ? PlayerInputManager.Instance.Horizontal : 0f;
            float z = PlayerInputManager.Instance != null ? PlayerInputManager.Instance.Vertical : 0f;
            Vector2 planar = Vector2.ClampMagnitude(new Vector2(x, z), 1f);

            float vertical = 0f;
            if (Input.GetKey(KeyCode.Space)) vertical += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) vertical -= 1f;

            // Camera-relative direction. Forward keeps the camera pitch, so looking up
            // and pressing W flies upward. Strafe is flattened so it never changes height.
            Transform camT = player.cam;
            if (camT == null && Camera.main != null)
                camT = Camera.main.transform;

            Vector3 forward = camT != null ? camT.forward : player.transform.forward;
            Vector3 right = camT != null ? camT.right : player.transform.right;
            right.y = 0f;
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

            // Normalized so diagonal flight is never faster than straight flight.
            Vector3 move = forward * planar.y + right * planar.x;
            if (move.sqrMagnitude > 1f)
                move.Normalize();

            float speed = Input.GetKey(KeyCode.LeftShift)
                ? Mathf.Max(normalFlySpeed, fastFlySpeed)
                : Mathf.Max(1f, normalFlySpeed);

            return move * speed + Vector3.up * (vertical * Mathf.Max(1f, verticalFlySpeed));
        }

        private void RotateBody(PlayerMovement player, float dt)
        {
            // Keep the character upright: only yaw toward the horizontal movement direction.
            Vector3 flat = currentVelocity;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.25f)
                return;

            Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up);
            player.transform.rotation = Quaternion.Slerp(
                player.transform.rotation, target, Mathf.Max(0.1f, rotationSpeed) * dt);
        }

        private static void ApplyNeutralAnimation(PlayerMovement player, float dt)
        {
            // Freeze a neutral grounded idle so no falling/landing animations trigger.
            Animator animator = player.animator;
            if (animator == null)
                return;

            animator.SetFloat("Speed", 0f, 0.1f, dt);
            animator.SetFloat("VerticalVelocity", 0f);
            animator.SetBool("IsGrounded", true);
            animator.SetFloat("GroundDistance", 0f);
            animator.SetBool("isSwimming", false);
            animator.SetBool("isRolling", false);
        }
#endif
    }
}
