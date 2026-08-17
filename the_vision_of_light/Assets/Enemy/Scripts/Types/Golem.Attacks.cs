using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>Jump attack, stone throw, root motion, and animation events for <see cref="Golem"/>.</summary>
    public partial class Golem
    {
        #region Jump Attack

        private void BeginJumpAttack()
        {
            isJumpAttackActive = true;
            lastJumpAttackTime = Time.time;

            if (anim != null)
            {
                anim.ResetTrigger("JumpAttack");
                anim.SetTrigger("JumpAttack");
            }
        }

        private bool ShouldJumpAttack()
        {
            if (isInOpeningSequence || isThrowing)
                return false;

            if (BossStats == null)
                return false;

            if (distanceToTarget < jumpAttackMinDistance || distanceToTarget > jumpAttackMaxDistance)
                return false;

            if (Time.time < lastJumpAttackTime + jumpAttackCooldown)
                return false;

            return Random.value <= jumpAttackChance;
        }

        private bool IsInJumpAttackState()
        {
            if (anim == null) return false;

            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            return state.IsName("JumpAttack");
        }

        #endregion

        #region Throw Stone

        private bool TryGetThrowStats(out float throwDamagePercent, out float projectileSpeed)
        {
            if (stats is GolemStats golemStats)
            {
                throwDamagePercent = golemStats.ThrowDamage;
                projectileSpeed = golemStats.ProjectileSpeed;
                return true;
            }

            throwDamagePercent = 95f;
            projectileSpeed = 26f;
            return false;
        }

        private bool ShouldThrowStone()
        {
            if (isThrowing || isAttackingBase)
                return false;

            if (BossStats == null || distanceToTarget < throwMinDistance)
                return false;

            if (distanceToTarget > throwMaxDistance)
                return false;

            if (Time.time < lastAttackTime + GetAttackCooldown())
                return false;

            if (Random.value > throwChance)
            {
                lastAttackTime = Time.time;
                return false;
            }

            return true;
        }

        private void ForceEndThrow()
        {
            DisableAnimRootMotion();
            isThrowing = false;
            throwLaunchDirection = Vector3.zero;
            lastAttackTime = Time.time;

            if (anim != null)
                anim.ResetTrigger("Throw");

            ResetCombatStates();
        }

        private bool IsInThrowStoneState()
        {
            if (anim == null) return false;

            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            return state.IsName("ThrowStone");
        }

        private void LockThrowFacing()
        {
            Vector3 flatDirection = transform.forward;
            if (target != null)
            {
                flatDirection = target.position - transform.position;
                flatDirection.y = 0f;
            }

            if (flatDirection.sqrMagnitude < 0.01f)
                flatDirection = transform.forward;

            flatDirection.Normalize();
            throwLockedRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
            transform.rotation = throwLockedRotation;

            Vector3 origin = throwPoint != null ? throwPoint.position : transform.position + Vector3.up * 2f;
            if (target != null)
            {
                Vector3 aimPoint = target.position + Vector3.up * throwAimHeight;
                throwLaunchDirection = aimPoint - origin;

                float horizontalDistance = new Vector3(throwLaunchDirection.x, 0f, throwLaunchDirection.z).magnitude;
                throwLaunchDirection.y -= horizontalDistance * 0.01f;
            }
            else
            {
                throwLaunchDirection = flatDirection;
                throwLaunchDirection.y -= 0.04f;
            }

            if (throwLaunchDirection.sqrMagnitude > 0.01f)
                throwLaunchDirection.Normalize();
            else
                throwLaunchDirection = flatDirection;
        }

        private void IgnoreStoneCollisionWithGolem(GameObject stoneObj)
        {
            if (!TryGetComponent(out CapsuleCollider golemCollider))
                return;

            foreach (Collider stoneCollider in stoneObj.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(stoneCollider, golemCollider, true);
        }

        #endregion

        #region Root Motion

        private void EnableFullRootMotion()
        {
            if (animRootMotionMode == AnimRootMotionMode.Full || anim == null || agent == null)
                return;

            animRootMotionMode = AnimRootMotionMode.Full;
            anim.applyRootMotion = true;
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        private void EnableThrowRootMotion()
        {
            if (animRootMotionMode == AnimRootMotionMode.ThrowPositionOnly || anim == null || agent == null)
                return;

            animRootMotionMode = AnimRootMotionMode.ThrowPositionOnly;
            anim.applyRootMotion = true;
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        private void DisableAnimRootMotion()
        {
            if (animRootMotionMode == AnimRootMotionMode.None)
                return;

            animRootMotionMode = AnimRootMotionMode.None;

            if (anim != null)
                anim.applyRootMotion = false;

            RestoreAgentAfterRootMotion();
        }

        private void RestoreAgentAfterRootMotion()
        {
            if (agent == null) return;

            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = true;

            if (agent.isOnNavMesh)
                agent.Warp(transform.position);
        }

        private void OnAnimatorMove()
        {
            if (anim == null || animRootMotionMode == AnimRootMotionMode.None)
                return;

            switch (animRootMotionMode)
            {
                case AnimRootMotionMode.Full:
                    anim.ApplyBuiltinRootMotion();
                    break;
                case AnimRootMotionMode.ThrowPositionOnly:
                    transform.position += anim.deltaPosition;
                    transform.rotation = throwLockedRotation;
                    break;
            }

            if (agent != null && agent.isOnNavMesh)
                agent.Warp(transform.position);
        }

        #endregion

        #region Animation Events

        /// <summary>Animation event at end of StartFight — chains into JumpAttack.</summary>
        public void EndStartFight()
        {
            isInStartFightSlap = false;
            FaceTarget();
            BeginJumpAttack();
        }

        /// <summary>Animation event — light melee VFX (Attack_1 / Attack_2).</summary>
        public void PlayLightAttackVfx()
        {
            attackVfx?.PlayLightEffect();
        }

        /// <summary>Animation event — JumpAttack impact VFX.</summary>
        public void PlayHeavyAttackVfx()
        {
            attackVfx?.PlayHeavyEffect();
        }

        /// <summary>Animation event on melee / jump attack clips — damage only.</summary>
        public void AnimHit()
        {
            if (BossStats == null || isInStartFightSlap)
                return;

            float damageMultiplier = GetDamagePercent() / 100f;
            float range = BossStats.AttackRange + (isInOpeningSequence ? 1.5f : 0f);
            ExecuteMeleeAttack(damageMultiplier, range);
        }

        /// <summary>Animation event on ThrowStone clip — spawns <see cref="StoneProjectile"/>.</summary>
        public void ShootStone()
        {
            // Animation events can fire while blending into death — no posthumous throws.
            if (isDead || throwPoint == null || stonePrefab == null)
                return;

            TryGetThrowStats(out float throwDamagePercent, out float projectileSpeed);
            float damage = currentAttack * (throwDamagePercent / 100f);

            transform.rotation = throwLockedRotation;

            GameObject stoneObj = Instantiate(stonePrefab, throwPoint.position, throwLockedRotation);

            IgnoreStoneCollisionWithGolem(stoneObj);

            StoneProjectile projectile = null;
            if (stoneObj.TryGetComponent(out projectile))
            {
                projectile.SetDamage(damage);
                projectile.SetTarget(target);
                if (TryGetComponent(out EnemyAudioEmitter emitter))
                    projectile.BindAudio(emitter);
            }

            if (stoneObj.TryGetComponent(out Rigidbody rb))
            {
                Vector3 direction = throwLaunchDirection.sqrMagnitude > 0.01f
                    ? throwLaunchDirection
                    : throwLockedRotation * Vector3.forward;

                rb.linearVelocity = direction * projectileSpeed;
                rb.angularVelocity = Vector3.zero;
                projectile?.NotifyLaunched();
            }
        }

        /// <summary>Animation event — ends the attack clip and resumes movement.</summary>
        public override void EndAttack()
        {
            DisableAnimRootMotion();

            if (isThrowing)
            {
                isThrowing = false;
                throwLaunchDirection = Vector3.zero;
                lastAttackTime = Time.time;
            }

            if (anim != null)
            {
                anim.ResetTrigger("Throw");
                anim.ResetTrigger("JumpAttack");
            }

            if (isJumpAttackActive)
            {
                isJumpAttackActive = false;
                lastJumpAttackTime = Time.time;
                lastAttackTime = Time.time;
            }

            if (isInOpeningSequence)
            {
                isInOpeningSequence = false;
                openingSequenceDone = true;
            }

            ResetCombatStates();
        }

        /// <summary>Animation event — ends the hit reaction clip.</summary>
        public override void EndHit() => ResetCombatStates();

        #endregion
    }
}
