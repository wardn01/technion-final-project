using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Mid-tier normal enemy — melee sword and bone throw at range. Data: Goblin/Data/GoblinData.asset.
    /// </summary>
    [RequireComponent(typeof(EnemyAudioEmitter))]
    public class Goblin : NormalEnemy
    {
        [Header("Goblin Visuals")]
        [SerializeField] private GameObject swordModel;

        [Header("Ranged Attack")]
        [SerializeField] private GameObject bonePrefab;
        [SerializeField] private Transform throwPoint;

        private GoblinStats RangedStats => stats as GoblinStats;

        protected override void Update()
        {
            if (isDead || target == null || playerHealth == null || playerHealth.isDead)
            {
                StopAgent();
                isAttackingBase = false;
                if (anim != null) anim.SetFloat("Speed", 0f);

                if (playerHealth != null && playerHealth.isDead && swordModel != null)
                    swordModel.SetActive(false);

                return;
            }

            UpdateBlendTree();

            if (isReturningToCamp)
            {
                if (swordModel != null) swordModel.SetActive(false);
                ReturnToCampBehavior();
                return;
            }

            if (isHitBase || isAttackingBase)
            {
                StopAgent();
                if (isAttackingBase) FaceTarget();
                return;
            }

            if (MeleeStats != null)
            {
                float distanceFromCamp = Vector3.Distance(transform.position, startingPosition);
                if (distanceFromCamp > MeleeStats.MaxLeashDistance)
                {
                    TriggerCampReset();
                    return;
                }
            }

            distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (MeleeStats != null && distanceToTarget <= MeleeStats.NormalAttackRange)
                HandleAttack(isMelee: true);
            else if (RangedStats != null && distanceToTarget <= RangedStats.MeleeChargeRange)
            {
                if (swordModel != null && !swordModel.activeSelf) swordModel.SetActive(true);
                ChaseBehavior();
            }
            else if (RangedStats != null && distanceToTarget <= RangedStats.RangedAttackRange)
                HandleAttack(isMelee: false);
            else if (distanceToTarget <= stats.ChaseRange)
            {
                if (swordModel != null && swordModel.activeSelf) swordModel.SetActive(false);
                ChaseBehavior();
            }
            else
            {
                if (swordModel != null && swordModel.activeSelf) swordModel.SetActive(false);
                PatrolBehavior();
            }
        }

        private void HandleAttack(bool isMelee)
        {
            if (isMelee && MeleeStats == null) return;
            if (!isMelee && RangedStats == null) return;

            StopAgent();
            FaceTarget();

            float cooldown = isMelee
                ? MeleeStats.NormalAttackCooldown
                : RangedStats.RangedAttackCooldown;

            if (Time.time < lastAttackTime + cooldown) return;

            isAttackingBase = true;
            lastAttackTime = Time.time;

            if (isMelee)
            {
                if (swordModel != null) swordModel.SetActive(true);
                if (anim != null) anim.SetTrigger("MeleeAttack");
            }
            else
            {
                if (swordModel != null) swordModel.SetActive(false);
                if (anim != null) anim.SetTrigger("RangedAttack");
            }
        }

        /// <summary>Animation event on Throw clip — spawns <see cref="BoneProjectile"/>.</summary>
        public void ShootBone()
        {
            if (RangedStats == null || target == null || bonePrefab == null || throwPoint == null)
                return;

            GameObject boneObj = Instantiate(bonePrefab, throwPoint.position, throwPoint.rotation);

            if (boneObj.TryGetComponent(out BoneProjectile boneScript))
                boneScript.SetDamage(currentAttack * (RangedStats.RangedDamage / 100f));

            if (boneObj.TryGetComponent(out Rigidbody rb))
            {
                Vector3 aimTarget = target.position + Vector3.up * 2f;
                Vector3 direction = (aimTarget - throwPoint.position).normalized;
                direction.y += Vector3.Distance(transform.position, target.position) * 0.015f;
                rb.AddForce(direction * RangedStats.ProjectileSpeed, ForceMode.Impulse);
            }
        }

        /// <summary>Animation event on melee Attack clip.</summary>
        public void AnimHit()
        {
            if (MeleeStats == null) return;

            ExecuteMeleeAttack(
                MeleeStats.NormalDamage / 100f,
                MeleeStats.NormalAttackRange);
        }

        protected override void PerformAttack() { }

        protected override void TriggerCampReset()
        {
            if (anim != null)
            {
                anim.ResetTrigger("MeleeAttack");
                anim.ResetTrigger("RangedAttack");
            }

            if (swordModel != null) swordModel.SetActive(false);
            base.TriggerCampReset();
        }
    }
}
