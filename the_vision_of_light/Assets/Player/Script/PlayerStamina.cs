using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("UI References")]
    public Image staminaBarFill;

    [Header("Stamina Settings")]
    [HideInInspector] public float maxStamina;
    public float currentStamina;
    public float staminaRegenRate = 20f;
    public float staminaRegenDelay = 1.5f;

    private float regenTimer = 0f;

    void Start()
    {
        UpdateMaxStaminaFromData();
        currentStamina = maxStamina;
    }

    public void UpdateMaxStaminaFromData()
    {
        if (PlayerData.Instance != null)
        {
            maxStamina = PlayerData.Instance.GetTotalMaxStamina();
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
        else
        {
            maxStamina = 100f;
        }
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

        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = Mathf.Clamp01(currentStamina / maxStamina);
        }
    }

    public bool HasStamina(float amount = 0.1f)
    {
        amount = Mathf.Max(0f, amount);
        return currentStamina >= amount;
    }

    public void ConsumeStamina(float amount)
    {
        if (amount <= 0f) return;

        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        regenTimer = staminaRegenDelay;
    }
}