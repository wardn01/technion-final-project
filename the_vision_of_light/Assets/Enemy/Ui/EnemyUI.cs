using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthFillImage;
    
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
}