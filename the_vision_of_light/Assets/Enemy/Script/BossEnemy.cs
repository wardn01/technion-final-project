using UnityEngine;
using UnityEngine.UI;

public class BossEnemy : EnemyBase
{
    [Header("Boss Specific UI")]
    [SerializeField] protected string bossTitle = "The Great Boss";
    [SerializeField] protected Slider bossHealthBar;

    [Header("Boss Phases")]
    protected int currentPhase = 1;
    [SerializeField] protected float phaseTwoHealthPercentage = 0.5f;

    protected override void Start()
    {
        base.Start();

        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(true);
            bossHealthBar.maxValue = stats.MaxHealth;
            bossHealthBar.value = currentHealth;
        }

        Debug.Log($"The boss [{bossTitle}] has appeared! Prepare for the epic battle!");
    }

    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);

        if (!isDead)
        {
            CheckPhaseTransition();
        }
    }

    protected override void UpdateHealthUI()
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.value = currentHealth;
        }
    }

    private void CheckPhaseTransition()
    {
        if (currentPhase == 1 && currentHealth <= (stats.MaxHealth * phaseTwoHealthPercentage))
        {
            currentPhase = 2;
            EnterPhaseTwo();
        }
    }

    protected virtual void EnterPhaseTwo()
    {
        Debug.Log($"The boss [{bossTitle}] entered phase two! The boss is enraged!");

        if (anim != null) anim.SetTrigger("Roar");
        if (agent != null) agent.speed += 2f;
    }

    protected override void Die()
    {
        base.Die();

        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(false);
        }

        Debug.Log($"You have defeated [{bossTitle}]! The world is safe now!");
    }
}