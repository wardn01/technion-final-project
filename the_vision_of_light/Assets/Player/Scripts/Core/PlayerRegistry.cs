using UnityEngine;

namespace VisionOfLight.Player
{
    /// <summary>
    /// Central access point for player components — health, stamina, movement, and combat.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerRegistry : MonoBehaviour
    {
        #region Singleton
        public static PlayerRegistry Instance { get; private set; }
        #endregion

        #region Component References
        public PlayerHealth Health { get; private set; }
        public PlayerStamina Stamina { get; private set; }
        public PlayerMovement Movement { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public PlayerInputManager Input { get; private set; }
        #endregion

        #region Public API
        /// <summary>Ensures a <see cref="PlayerRegistry"/> exists on the player root.</summary>
        public static void EnsureOn(GameObject playerRoot)
        {
            if (playerRoot.GetComponent<PlayerRegistry>() == null)
                playerRoot.AddComponent<PlayerRegistry>();

            if (playerRoot.GetComponent<PlayerInputManager>() == null && PlayerInputManager.Instance == null)
                playerRoot.AddComponent<PlayerInputManager>();
        }
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Health = GetComponentInChildren<PlayerHealth>();
            Stamina = GetComponentInChildren<PlayerStamina>();
            Movement = GetComponentInChildren<PlayerMovement>();
            Combat = GetComponentInChildren<PlayerCombat>();
            Input = GetComponentInChildren<PlayerInputManager>();

            if (Health == null) Health = GetComponent<PlayerHealth>();
            if (Stamina == null) Stamina = GetComponent<PlayerStamina>();
            if (Movement == null) Movement = GetComponent<PlayerMovement>();
            if (Combat == null) Combat = GetComponent<PlayerCombat>();
            if (Input == null) Input = GetComponent<PlayerInputManager>();

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion
    }
}
