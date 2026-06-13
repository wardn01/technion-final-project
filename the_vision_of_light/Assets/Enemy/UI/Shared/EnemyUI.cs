using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthFillImage;
    public TextMeshProUGUI nameAndLevelText;
    
    private float maxHP;

    public void SetupHealthBar(float maxHealth)
    {
        maxHP = maxHealth;
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = 1f;
        }
    }

    public void UpdateHealthBar(float currentHealth)
    {
        if (healthFillImage != null && maxHP > 0)
        {
            healthFillImage.fillAmount = currentHealth / maxHP;
        }
    }

    public void SetupEnemyInfo(string enemyName, int enemyLevel)
    {
        if (nameAndLevelText != null)
            nameAndLevelText.text = $"Lv.{enemyLevel}";
    }
}