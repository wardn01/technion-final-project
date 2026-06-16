using UnityEngine;
using VisionOfLight.HUD;

namespace VisionOfLight.Player
{
    /// <summary>
    /// Player stamina pool, consumption, regeneration, and HUD updates driven by <see cref="PlayerData"/>.
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Player Data Reference")]
        public PlayerData playerData;

        [Header("UI References")]
        [SerializeField] PlayerStaminaBarUI staminaBarUI;

        [Header("Stamina Settings")]
        [HideInInspector] public float maxStamina;
        public float currentStamina;
        [Tooltip("Stamina restored per second after the regen delay expires.")]
        public float staminaRegenRate = 20f;
        [Tooltip("Seconds to wait after spending stamina before regeneration begins.")]
        public float staminaRegenDelay = 1.5f;
        #endregion

        #region Runtime State
        private float regenTimer = 0f;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            UpdateMaxStaminaFromData();
            currentStamina = maxStamina;
            UpdateStaminaUI();
        }

        void Update()
        {
            if (staminaRegenRate < 0f) staminaRegenRate = 0f;
            if (staminaRegenDelay < 0f) staminaRegenDelay = 0f;

            if (regenTimer > 0f)
            {
                regenTimer -= Time.deltaTime;
            }
            else if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            }

            UpdateStaminaUI();
        }
        #endregion

        #region Public API
        /// <summary>Recalculates max stamina from <see cref="PlayerData"/>.</summary>
        public void UpdateMaxStaminaFromData()
        {
            if (playerData != null)
            {
                maxStamina = playerData.GetTotalMaxStamina();
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
            else
            {
                maxStamina = 100f;
            }
        }

        /// <summary>Returns true when current stamina meets or exceeds <paramref name="amount"/>.</summary>
        public bool HasStamina(float amount = 0.1f)
        {
            amount = Mathf.Max(0f, amount);
            return currentStamina >= amount;
        }

        /// <summary>Spends stamina and resets the regen delay timer.</summary>
        public void ConsumeStamina(float amount)
        {
            if (amount <= 0f) return;

            currentStamina -= amount;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            regenTimer = staminaRegenDelay;
            UpdateStaminaUI();
        }
        #endregion

        #region Save/Load
        /// <summary>Applies saved stamina or defaults to full when no save value exists.</summary>
        public void ApplyLoadedStamina(bool hasSaved, float value)
        {
            UpdateMaxStaminaFromData();

            if (hasSaved)
                currentStamina = Mathf.Clamp(value, 0f, maxStamina);
            else
                currentStamina = maxStamina;

            UpdateStaminaUI();
        }
        #endregion

        #region Private Helpers
        private void UpdateStaminaUI()
        {
            staminaBarUI?.UpdateDisplay(currentStamina, maxStamina);
        }
        #endregion
    }
}
