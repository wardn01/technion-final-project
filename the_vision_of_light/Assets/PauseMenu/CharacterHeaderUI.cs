using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterHeaderUI : MonoBehaviour
{
    [Header("Data Reference")]
    public PlayerData playerData;

    [Header("UI Elements")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpInfoText;
    public Image xpFill;
    public Image[] stars;
    public TextMeshProUGUI worldLevelText;

    [Header("Star Settings")]
    public Color litColor = Color.white;
    public Color unlitColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (playerData == null) return;

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
                stars[i].color = (i < playerData.currentAscensionIndex) ? litColor : unlitColor;
            }
        }

        if (worldLevelText != null)
            worldLevelText.text = playerData.currentAscensionIndex.ToString();
    }
}