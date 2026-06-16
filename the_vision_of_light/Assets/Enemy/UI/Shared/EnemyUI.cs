using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// World-space health bar and name label attached to individual enemies.
    /// </summary>
    public class EnemyUI : MonoBehaviour
    {
        #region UI References
        [Header("UI Elements")]
        public Image healthFillImage;
        public TextMeshProUGUI nameAndLevelText;

        private float maxHP;
        #endregion

        #region Public API
        /// <summary>Initializes the fill to full HP.</summary>
        public void SetupHealthBar(float maxHealth)
        {
            maxHP = maxHealth;
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = 1f;
            }
        }

        /// <summary>Updates the fill amount from current HP.</summary>
        public void UpdateHealthBar(float currentHealth)
        {
            if (healthFillImage != null && maxHP > 0)
            {
                healthFillImage.fillAmount = currentHealth / maxHP;
            }
        }

        /// <summary>Sets the level label shown above the enemy.</summary>
        public void SetupEnemyInfo(string enemyName, int enemyLevel)
        {
            if (nameAndLevelText != null)
                nameAndLevelText.text = $"Lv.{enemyLevel}";
        }
        #endregion
    }
}
