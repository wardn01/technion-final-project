using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("UI References")]
    public Image healthBarFill;
    public TextMeshProUGUI hpText;
    public GameObject deathScreen;
    public GameObject hudScreen;

    [Header("Health Settings")]
    [HideInInspector] public int maxHealth;
    public int currentHealth;

    [Header("Components")]
    public Animator animator;
    public MonoBehaviour playerMovementScript;
    public CharacterController characterController;

    [Header("Floating Text UI")]
    public GameObject uiFloatingTextPrefab;
    public Transform uiTextSpawnPoint;

    public bool isDead = false;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private Collider mainCharacterCollider;

    void Start()
    {
        UpdateMaxHealthFromData();
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (deathScreen != null) deathScreen.SetActive(false);
        if (hudScreen != null) hudScreen.SetActive(true);

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        if (characterController != null)
        {
            mainCharacterCollider = characterController.GetComponent<Collider>();
        }

        SetRagdollState(false);
    }

    public void UpdateMaxHealthFromData()
    {
        if (PlayerData.Instance != null)
        {
            maxHealth = PlayerData.Instance.GetTotalMaxHealth();
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHealthUI();
        }
        else
        {
            maxHealth = 100;
        }
    }

    public void TakeDamage(float incomingDamage)
    {
        if (isDead) return;

        int playerDefense = PlayerData.Instance != null ? PlayerData.Instance.GetTotalDefense() : 0;
        float damageMultiplier = 100f / (100f + playerDefense);
        
        int finalDamage = Mathf.RoundToInt(incomingDamage * damageMultiplier);
        finalDamage = Mathf.Max(0, finalDamage);
        
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        ShowFloatingText("-" + finalDamage.ToString(), Color.red);
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void HealPlayer(float healAmount)
    {
        if (isDead) return;

        int finalHeal = Mathf.RoundToInt(healAmount);
        
        currentHealth += finalHeal;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        ShowFloatingText("+" + healAmount.ToString(), Color.green);
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        if (hpText != null)
            hpText.text = $"{currentHealth}/{maxHealth}";
    }

    private void Die()
    {
        isDead = true;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (deathScreen != null)
            deathScreen.SetActive(true);
            
        if (hudScreen != null)
            hudScreen.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (animator != null)
            animator.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        SetRagdollState(true);
    }

    public void Revive()
    {
        isDead = false;
        UpdateMaxHealthFromData();
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (deathScreen != null)
            deathScreen.SetActive(false);
            
        if (hudScreen != null)
            hudScreen.SetActive(true);

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetRagdollState(false);

        if (characterController != null)
            characterController.enabled = true;

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetFloat("Speed", 0f);
        }
    }

    private void ShowFloatingText(string text, Color color)
    {
        if (uiFloatingTextPrefab != null && uiTextSpawnPoint != null)
        {
            GameObject obj = Instantiate(uiFloatingTextPrefab, uiTextSpawnPoint.position, Quaternion.identity, uiTextSpawnPoint.parent);
            
            UIFloatingText floatingText = obj.GetComponent<UIFloatingText>();
            if (floatingText != null)
            {
                floatingText.Setup(text, color);
            }
        }
    }

    private void SetRagdollState(bool state)
    {
        if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0) return;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != null)
                rb.isKinematic = !state;
        }

        if (ragdollColliders == null || ragdollColliders.Length == 0) return;

        foreach (Collider col in ragdollColliders)
        {
            if (col != null && col != mainCharacterCollider)
            {
                col.enabled = state;
            }
        }
    }
}