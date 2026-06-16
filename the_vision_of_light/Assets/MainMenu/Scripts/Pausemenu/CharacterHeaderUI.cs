using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VisionOfLight.Player;

/// <summary>
/// Manages the character's header UI, displaying essential information such as level, experience progress, ascension stars, and world level.
/// </summary>
public class CharacterHeaderUI : MonoBehaviour
{
    #region Data & UI References
    [Header("Data Reference")]
    /// <summary>Reference to the player's active data containing level, XP, and ascension stats.</summary>
    public PlayerData playerData;

    [Header("UI Elements")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpInfoText;
    public Image xpFill;
    public Image[] stars;
    public TextMeshProUGUI worldLevelText;
    #endregion

    #region Visual Settings
    [Header("Star Settings")]
    /// <summary>The color applied to stars representing achieved ascension levels.</summary>
    public Color litColor = Color.white;
    
    /// <summary>The color applied to stars representing unachieved ascension levels.</summary>
    public Color unlitColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Ensures the UI is immediately refreshed with the latest data whenever this screen is opened.
    /// </summary>
    private void OnEnable()
    {
        RefreshUI();
    }
    #endregion

    #region UI Updates
    /// <summary>
    /// Pulls current data from the PlayerData profile and updates all relevant UI text, fill bars, and star colors.
    /// </summary>
    public void RefreshUI()
    {
        if (playerData == null) return;

        // Note: Character name is currently hardcoded.
        if (characterNameText != null)
            characterNameText.text = "Xiao";

        if (levelText != null)
            levelText.text = $"{playerData.currentLevel}/{playerData.maxLevelCap}";

        if (xpInfoText != null)
            xpInfoText.text = $"{playerData.currentXP}/{playerData.xpToNextLevel}";

        if (xpFill != null)
            xpFill.fillAmount = (float)playerData.currentXP / playerData.xpToNextLevel;

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                // Stars below the current ascension index are lit; otherwise, they remain unlit.
                stars[i].color = (i < playerData.currentAscensionIndex) ? litColor : unlitColor;
            }
        }

        if (worldLevelText != null)
            worldLevelText.text = playerData.currentAscensionIndex.ToString();
    }
    #endregion
}