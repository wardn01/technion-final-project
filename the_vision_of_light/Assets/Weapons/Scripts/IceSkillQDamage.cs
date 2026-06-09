using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ice Sword [Q] freezing aura. Ticks damage while enemies remain inside and applies a slow
/// that is removed when they exit or when the aura is destroyed.
/// </summary>
public class IceSkillQDamage : MonoBehaviour
{
    #region Aura Settings
    [Header("Aura Settings")]
    public float tickInterval = 1f;
    public float lifeTime = 8f;
    public float slowPercentage = 0.5f;
    #endregion

    #region State
    private float damagePerTick;
    private float nextDamageTime;
    private readonly List<EnemyBase> enemiesInAura = new List<EnemyBase>();
    #endregion

    #region Public API
    /// <summary>Called by <c>PlayerCombat.OnSkillQ</c> after the prefab is spawned.</summary>
    public void SetDamage(float damage) { damagePerTick = damage; }
    #endregion

    #region Unity Lifecycle
    private void Start() { Destroy(gameObject, lifeTime); }

    private void Update()
    {
        bool dealDamageThisFrame = Time.time >= nextDamageTime;
        if (dealDamageThisFrame) nextDamageTime = Time.time + tickInterval;

        for (int i = enemiesInAura.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = enemiesInAura[i];

            if (enemy == null || enemy.IsDead)
            {
                enemiesInAura.RemoveAt(i);
                continue;
            }

            if (dealDamageThisFrame) enemy.TakeDamage(damagePerTick);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < enemiesInAura.Count; i++)
        {
            EnemyBase enemy = enemiesInAura[i];
            if (enemy == null || enemy.IsDead) continue;

            EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
            if (status != null) status.RemoveSlow();
        }
        enemiesInAura.Clear();
    }
    #endregion

    #region Trigger Logic
    private void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

        if (enemy == null || enemy.IsDead || enemiesInAura.Contains(enemy)) return;

        enemiesInAura.Add(enemy);

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null) status.ApplySlow(slowPercentage);
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

        if (enemy == null || !enemiesInAura.Contains(enemy)) return;

        enemiesInAura.Remove(enemy);

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null) status.RemoveSlow();
    }
    #endregion
}
