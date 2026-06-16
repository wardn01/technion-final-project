using UnityEngine;
using UnityEngine.UI;
using VisionOfLight.Player;

namespace VisionOfLight.HUD
{
    /// <summary>HUD stamina bar fill for <see cref="PlayerStamina"/>.</summary>
    public class PlayerStaminaBarUI : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] Image staminaBarFill;
        #endregion

        #region Public API
        /// <summary>Updates the stamina bar fill amount.</summary>
        public void UpdateDisplay(float current, float max)
        {
            if (staminaBarFill != null)
                staminaBarFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }
        #endregion
    }
}
