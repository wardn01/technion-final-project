using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using VisionOfLight.Player;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Shared enemy foundation: scaled stats, NavMesh movement, damage, loot, and animation-driven audio.
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Player Data Reference")]
        public PlayerData playerData;

        [Header("Base Data")]
        [SerializeField] protected EnemyBaseStats stats; 

        [Header("Current Scaled Stats")]
        public int enemyLevel = 1;
        public float currentMaxHealth;
        public float currentAttack;
        public float currentDefense;
        protected float currentHealth;

        [Header("Base Components")]
        protected Animator anim;
        protected NavMeshAgent agent;
        protected bool isDead = false;
        public bool IsDead => isDead;
        protected EnemyUI enemyUI;
        protected Transform target;
        private EnemyAudioEmitter enemyAudio;

        [Header("Combat States")]
        public bool isHitBase = false;
        public bool isAttackingBase = false;

        [Header("UI & Effects")]
        public GameObject damageTextPrefab;
        public Transform textSpawnPoint;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            anim = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            enemyUI = GetComponentInChildren<EnemyUI>();
            enemyAudio = GetComponent<EnemyAudioEmitter>();
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (stats == null || Application.isPlaying) return;

            int previewLevel = Mathf.Max(1, enemyLevel);
            currentMaxHealth = stats.BaseMaxHealth + (previewLevel * stats.HpScale);
            currentAttack = stats.BaseAttack + (previewLevel * stats.AtkScale);
            currentDefense = stats.BaseDefense + (previewLevel * stats.DefScale);
        }
    #endif

        /// <summary>Animation event entry point — forwards to <see cref="EnemyAudioEmitter"/>.</summary>
        public void PlayEnemySound(string actionName)
        {
            if (enemyAudio == null)
                enemyAudio = GetComponent<EnemyAudioEmitter>();

            enemyAudio?.PlayClip(actionName);
        }

        protected virtual IEnumerator Start()
        {
            yield return new WaitForSeconds(0.15f); 

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;

            if (stats != null)
            {
                ScaleStatsWithPlayer();
                currentHealth = currentMaxHealth;

                if (agent != null) agent.speed = stats.WalkSpeed; 

                if (enemyUI != null) 
                {
                    enemyUI.SetupHealthBar(currentMaxHealth);
                    enemyUI.SetupEnemyInfo(stats.EnemyName, enemyLevel);
                }
            }
        }
        #endregion

        #region Stat Scaling
        /// <summary>Rolls level from player ascension and applies HP/ATK/DEF scaling from <see cref="stats"/>.</summary>
        protected virtual void ScaleStatsWithPlayer()
        {
            if (playerData != null && stats != null)
            {
                int worldLevel = playerData.currentAscensionIndex; 

                int minEnemyLevel = Mathf.Max(1, worldLevel * 10);
                int maxEnemyLevel = minEnemyLevel + 9;

                if (worldLevel >= 10)
                {
                    minEnemyLevel = 100;
                    maxEnemyLevel = 110;
                }

                enemyLevel = Random.Range(minEnemyLevel, maxEnemyLevel + 1);

                currentMaxHealth = stats.BaseMaxHealth + (enemyLevel * stats.HpScale);
                currentAttack = stats.BaseAttack + (enemyLevel * stats.AtkScale);
                currentDefense = stats.BaseDefense + (enemyLevel * stats.DefScale);
            }
            else if (stats != null)
            {
                enemyLevel = 10; 
                currentMaxHealth = stats.BaseMaxHealth + (10 * stats.HpScale);
                currentAttack = stats.BaseAttack + (10 * stats.AtkScale);
                currentDefense = stats.BaseDefense + (10 * stats.DefScale);
            }
        }
        #endregion

        #region Combat
        /// <summary>Clears hit/attack flags and resumes NavMesh after an animation finishes.</summary>
        public virtual void ResetCombatStates()
        {
            isHitBase = false;
            isAttackingBase = false;

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !isDead)
            {
                agent.isStopped = false;
            }
        }

        /// <summary>Stops NavMesh movement and zeroes velocity.</summary>
        public void StopAgent()
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        protected void FaceTarget()
        {
            if (target == null || stats == null) return;
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; 
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * stats.RotationSpeed);
        }

        /// <summary>Applies defense-scaled damage, floating numbers, hit reaction, and death when HP reaches zero.</summary>
        public virtual void TakeDamage(float incomingDamage, bool playHitReaction = true)
        {
            if (isDead) return;

            float damageMultiplier = 100f / (100f + currentDefense);
            float finalDamage = incomingDamage * damageMultiplier;

            int finalDamageInt = Mathf.RoundToInt(finalDamage);
            finalDamageInt = Mathf.Max(1, finalDamageInt); 

            currentHealth -= finalDamageInt;
            UpdateHealthUI();

            if (damageTextPrefab != null)
            {
                Vector3 spawnPos = textSpawnPoint != null ? textSpawnPoint.position : transform.position + Vector3.up * 2f;
                GameObject textObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
                textObj.GetComponent<DamageText>()?.Setup(finalDamageInt);
            }

            if (currentHealth <= 0) Die();
            else if (playHitReaction) PlayHitEffect();
        }

        protected virtual void PlayHitEffect()
        {
            if (anim != null) anim.SetTrigger("Hit");
        }

        protected virtual void UpdateHealthUI()
        {
            if (enemyUI != null) enemyUI.UpdateHealthBar(currentHealth);
        }

        /// <summary>Plays death animation, disables colliders, drops loot, grants XP, and destroys the object.</summary>
        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            EnemyStatusEffects statusEffects = GetComponent<EnemyStatusEffects>();
            if (statusEffects != null)
            {
                statusEffects.RemoveBurn();
                statusEffects.RemoveSlow();
            }

            if (anim != null) anim.SetTrigger("Die");
            StopAgent();
            if (agent != null) agent.enabled = false;

            foreach (Collider col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            if (enemyUI != null) enemyUI.gameObject.SetActive(false);

            DropLoot();
            GiveXP();

            QuestMonster questEnemy = GetComponent<QuestMonster>();
            if (questEnemy != null)
            {
                questEnemy.OnMonsterDeath();
            }

            Destroy(gameObject, 5f);
        }

        private void GiveXP()
        {
            if (stats == null || playerData == null) return;

            playerData.AddXP(stats.XPReward);
        }

        private void DropLoot()
        {
            if (InventoryManager.Instance == null || stats == null || stats.LootTable == null) return;

            foreach (var loot in stats.LootTable)
            {
                float roll = Random.Range(0f, 100f);

                if (roll <= loot.dropChance)
                {
                    int amount = Random.Range(loot.minAmount, loot.maxAmount + 1);
                    InventoryManager.Instance.AddItem(loot.item, amount);
                }
            }
        }

        /// <summary>Deals melee damage to the player when in range and facing the target.</summary>
        protected void ExecuteMeleeAttack(float damageMultiplier = 1f, float attackRange = 2f, float maxAngle = 60f)
        {
            if (target == null) return;

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= attackRange + 0.8f)
            {
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                directionToTarget.y = 0; 
                float angle = Vector3.Angle(transform.forward, directionToTarget);

                if (angle <= maxAngle) 
                {
                    PlayerHealth pHealth = target.GetComponent<PlayerHealth>();
                    if (pHealth != null)
                    {
                        float finalDamage = currentAttack * damageMultiplier;
                        pHealth.TakeDamage(finalDamage);
                    }
                }
            }
        }
        #endregion
    }
}
