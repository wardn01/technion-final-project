using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VisionOfLight.Player;

namespace VisionOfLight.HUD
{
    /// <summary>HUD health bar, HP text, and death screen toggles for <see cref="PlayerHealth"/>.</summary>
    public class PlayerHealthBarUI : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] Image healthBarFill;
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] GameObject deathScreen;
        [SerializeField] GameObject hudScreen;
        #endregion

        #region Public API
        /// <summary>Updates the fill amount and HP text display.</summary>
        public void UpdateDisplay(int current, int max)
        {
            if (healthBarFill != null)
                healthBarFill.fillAmount = max > 0 ? (float)current / max : 0f;

            if (hpText != null)
                hpText.text = current + "/" + max;
        }

        /// <summary>Shows the death screen and hides the HUD.</summary>
        public void ShowDeath()
        {
            if (deathScreen != null)
                deathScreen.SetActive(true);

            if (hudScreen != null)
                hudScreen.SetActive(false);
        }

        /// <summary>Hides the death screen and restores the HUD.</summary>
        public void ShowAlive()
        {
            if (deathScreen != null)
                deathScreen.SetActive(false);

            if (hudScreen != null)
                hudScreen.SetActive(true);
        }
        #endregion
    }
}
