using UnityEngine;
using System.Collections;
using VisionOfLight.HUD;

namespace VisionOfLight.Player
{
    /// <summary>
    /// Player health, damage mitigation, healing, death/revive, and HUD updates driven by <see cref="PlayerData"/>.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Player Data")]
        public PlayerData playerData;

        [Header("UI References")]
        [SerializeField] PlayerHealthBarUI healthBarUI;

        [Header("Health Settings")]
        [HideInInspector] public int maxHealth;
        public int currentHealth;

        [Header("Components")]
        public Animator animator;
        public PlayerMovement playerMovementScript;
        public CharacterController characterController;

        [Header("Floating Text UI")]
        public GameObject uiFloatingTextPrefab;
        public Transform uiTextSpawnPoint;
        #endregion

        #region Runtime State
        public bool isDead = false;

        private Rigidbody[] ragdollRigidbodies;
        private Collider[] ragdollColliders;
        private Collider mainCharacterCollider;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            PlayerRegistry.EnsureOn(gameObject);
        }

        void Start()
        {
            UpdateMaxHealthFromData();
            UpdateHealthUI();

            healthBarUI?.ShowAlive();

            ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            ragdollColliders = GetComponentsInChildren<Collider>();

            if (characterController != null)
            {
                mainCharacterCollider = characterController.GetComponent<Collider>();
            }

            SetRagdollState(false);
        }
        #endregion

        #region Public API
        /// <summary>Refreshes max HP from <see cref="PlayerData"/> after stat changes.</summary>
        public void UpdateStatsFromData()
        {
            UpdateMaxHealthFromData();
        }

        /// <summary>Applies incoming damage after defense scaling from <see cref="PlayerData"/>.</summary>
        public void TakeDamage(float incomingDamage)
        {
            if (isDead)
                return;

            int playerDefense = playerData != null ? playerData.GetTotalDefense() : 0;
            float damageMultiplier = 100f / (100f + playerDefense);
            int finalDamage = Mathf.RoundToInt(incomingDamage * damageMultiplier);
            finalDamage = Mathf.Max(0, finalDamage);

            if (finalDamage <= 0)
                return;

            currentHealth -= finalDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            ShowFloatingText("-" + finalDamage.ToString(), Color.red);
            UpdateHealthUI();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>Instant heal only — delegates to the overload with zero tick values.</summary>
        public void HealPlayer(float healAmount)
        {
            HealPlayer(healAmount, 0f, 0f, 0);
        }

        /// <summary>Applies instant heal and optional tick-based regeneration.</summary>
        public void HealPlayer(float instantAmount, float tickAmount, float interval, int count)
        {
            if (isDead)
                return;

            if (instantAmount > 0)
            {
                int finalHeal = Mathf.RoundToInt(instantAmount);
                currentHealth += finalHeal;
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

                ShowFloatingText("+" + finalHeal.ToString(), Color.green);
                UpdateHealthUI();
            }

            if (tickAmount > 0 && count > 0)
            {
                StartCoroutine(HealTickedCoroutine(tickAmount, interval, count));
            }
        }

        /// <summary>Restores the player to full health and re-enables movement and animation.</summary>
        public void Revive()
        {
            isDead = false;

            UpdateMaxHealthFromData();
            currentHealth = maxHealth;
            UpdateHealthUI();

            healthBarUI?.ShowAlive();

            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }

            GameplayCursorPolicy.RequestApply();

            SetRagdollState(false);

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            if (animator != null)
            {
                animator.enabled = true;
                animator.SetFloat("Speed", 0f);
            }
        }
        #endregion

        #region Save/Load
        /// <summary>Called after save load. Uses full HP when <paramref name="savedHealth"/> is negative or save has no health data.</summary>
        public void ApplyLoadedHealth(bool hasSavedValue, int savedHealth)
        {
            UpdateMaxHealthFromData();

            if (hasSavedValue)
                currentHealth = Mathf.Clamp(savedHealth, 0, maxHealth);
            else
                currentHealth = maxHealth;

            if (currentHealth > 0 && isDead)
                Revive();

            UpdateHealthUI();

            if (currentHealth <= 0 && !isDead)
                Die();
        }
        #endregion

        #region Stats
        /// <summary>Recalculates max HP from <see cref="PlayerData"/> while preserving health percentage.</summary>
        public void UpdateMaxHealthFromData()
        {
            if (playerData != null)
            {
                int oldMaxHealth = maxHealth;
                int newMaxHealth = playerData.GetTotalMaxHealth();

                if (oldMaxHealth > 0)
                {
                    float healthPercentage = (float)currentHealth / oldMaxHealth;
                    maxHealth = newMaxHealth;
                    currentHealth = Mathf.RoundToInt(maxHealth * healthPercentage);
                }
                else
                {
                    maxHealth = newMaxHealth;
                }

                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                UpdateHealthUI();
            }
            else
            {
                maxHealth = 100;
            }
        }
        #endregion

        #region Private Helpers
        private IEnumerator HealTickedCoroutine(float amountPerTick, float interval, int totalTicks)
        {
            int ticksDone = 0;

            while (ticksDone < totalTicks)
            {
                yield return new WaitForSeconds(interval);

                if (isDead)
                    yield break;

                int healInt = Mathf.RoundToInt(amountPerTick);
                if (healInt <= 0)
                {
                    ticksDone++;
                    continue;
                }

                currentHealth = Mathf.Min(currentHealth + healInt, maxHealth);

                UpdateHealthUI();
                ShowFloatingText("+" + healInt.ToString(), new Color(0.2f, 1f, 0.2f));

                ticksDone++;
            }
        }

        private void UpdateHealthUI()
        {
            healthBarUI?.UpdateDisplay(currentHealth, maxHealth);
        }

        private void Die()
        {
            isDead = true;

            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = false;
            }

            healthBarUI?.ShowDeath();

            GameplayCursorPolicy.RequestApply();

            if (animator != null)
            {
                animator.enabled = false;
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            SetRagdollState(true);
        }

        private void ShowFloatingText(string text, Color color)
        {
            if (uiFloatingTextPrefab == null || uiTextSpawnPoint == null || string.IsNullOrEmpty(text))
                return;

            RectTransform spawnRect = uiTextSpawnPoint as RectTransform;
            if (spawnRect == null)
                return;

            GameObject obj = Instantiate(uiFloatingTextPrefab, spawnRect.parent);
            RectTransform objRect = obj.transform as RectTransform;

            UIFloatingText.PlaceAtSpawn(objRect, spawnRect, UIFloatingText.GetNextSpawnOffset());

            UIFloatingText floatingText = obj.GetComponent<UIFloatingText>();
            floatingText?.Setup(text, color);
        }

        private void SetRagdollState(bool state)
        {
            if (ragdollRigidbodies != null)
            {
                foreach (Rigidbody rb in ragdollRigidbodies)
                {
                    if (rb != null)
                    {
                        rb.isKinematic = !state;
                    }
                }
            }

            if (ragdollColliders != null)
            {
                foreach (Collider col in ragdollColliders)
                {
                    if (col != null && col != mainCharacterCollider)
                    {
                        col.enabled = state;
                    }
                }
            }
        }
        #endregion
    }
}
