using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    
    [Header("UI Settings")]
    public Image healthBarFill;
    public GameObject healthBarCanvas;

    [Header("Damage Text Settings")]
    public GameObject damageTextPrefab;
    public Transform textSpawnPoint;

    public bool isDead = false;

    private float currentHealth;
    private Animator anim;
    private EnemyAI aiScript;
    private NavMeshAgent agent;
    private EnemyStatusEffects statusEffects;

    private void Awake()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        aiScript = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
        statusEffects = GetComponent<EnemyStatusEffects>();
    }

    private void Start()
    {
        UpdateHealthUI();
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead)
            return;

        currentHealth -= damageAmount;
        Debug.Log($"En [{gameObject.name}] took damage: {damageAmount} | Remaining health: {currentHealth}");

        UpdateHealthUI();

        if (damageTextPrefab != null && textSpawnPoint != null)
        {
            GameObject dmgText = Instantiate(damageTextPrefab, textSpawnPoint.position, Quaternion.identity);
            DamageText textScript = dmgText.GetComponent<DamageText>();
            if (textScript != null)
            {
                textScript.Setup(damageAmount);
            }
        }

        if (currentHealth > 0f)
        {
            if (anim != null)
                anim.SetTrigger("Hit");

            if (aiScript != null)
                aiScript.OnHit();
        }
        else
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (healthBarCanvas != null)
            healthBarCanvas.SetActive(false);

        if (statusEffects != null)
            statusEffects.ResetAllEffects();

        if (anim != null)
        {
            anim.speed = 1f;
            anim.SetTrigger("Die");
        }

        if (aiScript != null)
            aiScript.enabled = false;

        if (agent != null)
            agent.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Collider coll = GetComponent<Collider>();
        if (coll != null)
            coll.enabled = false;

        Destroy(gameObject, 3f);
    }
}