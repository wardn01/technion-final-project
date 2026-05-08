using UnityEngine;

public class Goblin : NormalEnemy
{
    [Header("Goblin Visuals")]
    [SerializeField] private GameObject swordModel; 

    [Header("Ranged Attack Settings")]
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
            {
                swordModel.SetActive(false);
            }
                
            return; 
        }

        UpdateBlendTree(); 

        if (isHitBase || isAttackingBase)
        {
            StopAgent();
            if (isAttackingBase) FaceTarget(); 
            return;
        }

        distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (MeleeStats != null && distanceToTarget <= MeleeStats.NormalAttackRange)
        {
            HandleAttack(true); 
        }
        else if (RangedStats != null && distanceToTarget <= RangedStats.MeleeChargeRange)
        {
            if (swordModel != null && !swordModel.activeSelf) swordModel.SetActive(true); 
            ChaseBehavior(); 
        }
        else if (RangedStats != null && distanceToTarget <= RangedStats.RangedAttackRange)
        {
            HandleAttack(false); 
        }
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
        StopAgent();
        FaceTarget();

        float currentCooldown = isMelee ? MeleeStats.NormalAttackCooldown : (RangedStats != null ? RangedStats.RangedAttackCooldown : 2f);

        if (Time.time >= lastAttackTime + currentCooldown)
        {
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
    }

    public void ShootBone()
    {
        if (bonePrefab != null && throwPoint != null)
        {
            GameObject boneObj = Instantiate(bonePrefab, throwPoint.position, throwPoint.rotation);
            
            BoneProjectile boneScript = boneObj.GetComponent<BoneProjectile>();
            if (boneScript != null) 
            {
                float bonePercentage = RangedStats != null ? RangedStats.RangedDamage : MeleeStats.NormalDamage;
                float damageMultiplier = bonePercentage / 100f;
                float finalBoneDamage = currentAttack * damageMultiplier;
                
                boneScript.SetDamage(finalBoneDamage);
            }

            Rigidbody rb = boneObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 aimTarget = target.position + Vector3.up * 2f; 
                Vector3 direction = (aimTarget - throwPoint.position).normalized;

                float currentDistance = Vector3.Distance(transform.position, target.position);
                direction.y += (currentDistance * 0.015f); 

                float speed = RangedStats != null ? RangedStats.ProjectileSpeed : 15f;
                rb.AddForce(direction * speed, ForceMode.Impulse);
            }
        }
    }

    public void AnimHit()
    {
        if (MeleeStats != null)
        {
            float damageMultiplier = MeleeStats.NormalDamage / 100f;
            ExecuteMeleeAttack(damageMultiplier, MeleeStats.NormalAttackRange);
        }
    }

    protected override void PerformAttack() { }
}