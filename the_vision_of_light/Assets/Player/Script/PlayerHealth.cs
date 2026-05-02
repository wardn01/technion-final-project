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
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Components")]
    public Animator animator;
    public MonoBehaviour playerMovementScript;
    public CharacterController characterController;

    public bool isDead = false;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private Collider mainCharacterCollider;

    void Start()
    {
        ValidateSettings();
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

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        int finalDamage = Mathf.RoundToInt(damageAmount);
        
        finalDamage = Mathf.Max(0, finalDamage);
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
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

    private void ValidateSettings()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }
}