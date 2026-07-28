using UnityEngine;
using VisionOfLight.Player;

namespace VisionOfLight.DeveloperTools
{
    /// <summary>
    /// Central developer cheats manager. Lives on a single scene object named "DeveloperCheats".
    ///
    /// Cheats only run in the Unity Editor or Development Builds AND while the
    /// "Enable Developer Cheats" master switch is ticked (off by default).
    /// In release builds every hook returns neutral values and no input is processed.
    ///
    /// Keys: F1 panel, F2 high damage, F3 full heal, Shift+F3 auto heal,
    ///       F4 unlimited stamina, F5 invincibility, F6 fast move, F7 fly.
    ///
    /// Removal: untick the master switch, or delete Assets/DeveloperTools/Cheats/
    /// and the few lines marked with "DEV-CHEATS" in
    /// EnemyBase / PlayerHealth / PlayerStamina / PlayerMovement.
    /// </summary>
    public class DeveloperCheatsManager : MonoBehaviour
    {
        [Header("Master Switch")]
        [Tooltip("Must be ON for any cheat to work. Leave OFF for normal gameplay / release.")]
        [SerializeField] private bool enableDeveloperCheats = false;

        [Header("Config")]
        [SerializeField] private DeveloperCheatsConfig config = new DeveloperCheatsConfig();

        public static DeveloperCheatsManager Instance { get; private set; }

        // ------------------------------------------------------------------
        // Static hook API — safe to call from anywhere, in any build.
        // Returns neutral values when cheats are unavailable or disabled.
        // ------------------------------------------------------------------

        /// <summary>Multiplier applied once to player damage in EnemyBase.TakeDamage.</summary>
        public static float PlayerDamageMultiplier
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DeveloperCheatsManager m = Instance;
                return (m != null && m.CheatsAllowed && m.highDamageOn)
                    ? Mathf.Max(1f, m.config.damageMultiplier)
                    : 1f;
#else
                return 1f;
#endif
            }
        }

        /// <summary>True while the player should ignore all incoming damage.</summary>
        public static bool PlayerInvincible
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DeveloperCheatsManager m = Instance;
                return m != null && m.CheatsAllowed && m.invincibleOn;
#else
                return false;
#endif
            }
        }

        /// <summary>True while stamina must not decrease.</summary>
        public static bool UnlimitedStamina
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DeveloperCheatsManager m = Instance;
                return m != null && m.CheatsAllowed && m.unlimitedStaminaOn;
#else
                return false;
#endif
            }
        }

        /// <summary>Movement speed multiplier (1 when disabled).</summary>
        public static float MoveSpeedMultiplier
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DeveloperCheatsManager m = Instance;
                return (m != null && m.CheatsAllowed && m.fastMoveOn)
                    ? Mathf.Max(1f, m.config.moveSpeedMultiplier)
                    : 1f;
#else
                return 1f;
#endif
            }
        }

        /// <summary>True while developer Fly Mode is enabled (handled by <see cref="DeveloperFlyController"/>).</summary>
        public static bool IsFlyModeEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DeveloperCheatsManager m = Instance;
                return m != null && m.CheatsAllowed && m.flyOn;
#else
                return false;
#endif
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ------------------------------------------------------------------
        // Development-only state and logic.
        // ------------------------------------------------------------------

        private bool highDamageOn;
        private bool autoHealOn;
        private bool unlimitedStaminaOn;
        private bool invincibleOn;
        private bool fastMoveOn;
        private bool flyOn;
        private bool panelVisible;

        private float autoHealAccumulator;
        private PlayerHealth playerHealth;
        private PlayerStamina playerStamina;

        /// <summary>Master switch state (development environments only).</summary>
        public bool CheatsAllowed => enableDeveloperCheats;

        public bool HighDamageOn => highDamageOn;
        public bool AutoHealOn => autoHealOn;
        public bool UnlimitedStaminaOn => unlimitedStaminaOn;
        public bool InvincibleOn => invincibleOn;
        public bool FastMoveOn => fastMoveOn;
        public bool FlyOn => flyOn;
        public bool PanelVisible => panelVisible;
        public DeveloperCheatsConfig Config => config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (GetComponent<DeveloperCheatsPanel>() == null)
                gameObject.AddComponent<DeveloperCheatsPanel>();

            if (GetComponent<DeveloperFlyController>() == null)
                gameObject.AddComponent<DeveloperFlyController>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!enableDeveloperCheats)
                return;

            HandleKeys();
            HandleAutoHeal();
        }

        private void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                panelVisible = !panelVisible;

            if (Input.GetKeyDown(KeyCode.F2))
            {
                highDamageOn = !highDamageOn;
                Log("High Damage x" + config.damageMultiplier, highDamageOn);
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    autoHealOn = !autoHealOn;
                    Log("Auto Heal", autoHealOn);
                }
                else
                {
                    FullHeal();
                }
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                unlimitedStaminaOn = !unlimitedStaminaOn;
                if (unlimitedStaminaOn)
                    RestoreStaminaToMax();
                Log("Unlimited Stamina", unlimitedStaminaOn);
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                invincibleOn = !invincibleOn;
                Log("Invincibility", invincibleOn);
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                fastMoveOn = !fastMoveOn;
                Log("Fast Move x" + config.moveSpeedMultiplier, fastMoveOn);
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                flyOn = !flyOn;
                Log("Fly Mode (WASD camera-relative, Space/LCtrl, Shift fast)", flyOn);
            }
        }

        private void HandleAutoHeal()
        {
            if (!autoHealOn)
            {
                autoHealAccumulator = 0f;
                return;
            }

            PlayerHealth health = GetPlayerHealth();
            if (health == null || health.isDead || health.currentHealth >= health.maxHealth)
                return;

            autoHealAccumulator += config.autoHealPerSecond * Time.deltaTime;
            if (autoHealAccumulator >= 1f)
            {
                int amount = Mathf.FloorToInt(autoHealAccumulator);
                autoHealAccumulator -= amount;
                health.HealPlayer(amount);
            }
        }

        private void FullHeal()
        {
            PlayerHealth health = GetPlayerHealth();
            if (health == null || health.isDead)
                return;

            health.HealPlayer(health.maxHealth);
            Debug.Log("[DevCheats] Full heal → " + health.maxHealth + " HP.");
        }

        private void RestoreStaminaToMax()
        {
            PlayerStamina stamina = GetPlayerStamina();
            if (stamina != null)
                stamina.currentStamina = stamina.maxStamina;
        }

        private PlayerHealth GetPlayerHealth()
        {
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            return playerHealth;
        }

        private PlayerStamina GetPlayerStamina()
        {
            if (playerStamina == null)
                playerStamina = FindFirstObjectByType<PlayerStamina>();
            return playerStamina;
        }

        private static void Log(string cheat, bool state)
        {
            Debug.Log("[DevCheats] " + cheat + ": " + (state ? "ON" : "OFF"));
        }
#endif
    }
}
