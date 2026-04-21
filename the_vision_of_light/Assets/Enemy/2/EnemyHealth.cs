using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

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

 public void TakeDamage(float damageAmount)
    {
        if (isDead)
            return;

        currentHealth -= damageAmount;
        Debug.Log($"Enemy [{gameObject.name}] took damage: {damageAmount} | Remaining health: {currentHealth}");

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

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

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