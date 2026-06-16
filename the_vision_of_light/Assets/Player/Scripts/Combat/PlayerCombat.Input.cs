// Partial — attack input, buffering, and root motion.
using UnityEngine;
using UnityEngine.EventSystems;

namespace VisionOfLight.Player
{
    public partial class PlayerCombat
    {
        #region Interrupt & Input
        /// <summary>Returns true when the current combat animation can be cancelled by movement or a new action.</summary>
        public bool CanInterrupt()
        {
            if (anim == null) return true;

            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(1);

            if (anim.IsInTransition(1)) return false;

            if (state.IsName("Skill_E") || state.IsName("Skill_Q"))
            {
                return state.normalizedTime >= 0.95f;
            }
            else if (state.IsName("Rolling") || state.IsName("Attack_1") || state.IsName("Attack_2") || state.IsName("Attack_3"))
            {
                return state.normalizedTime >= 0.60f;
            }

            return true;
        }

        private void HandleInput()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.isInputLocked) return;

            bool grounded = movementScript != null && movementScript.isGrounded;

            if (!grounded) return;

            if (!CanInterrupt() || activeWeaponData == null) return;

            if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.AttackPressed)
            {
                ForceCancelRoll();
                RequestAttack();
            }

            WeaponState state = GetCurrentWeaponState();

            KeyCode burstKey = KeyCode.Q;
            KeyCode skillKey = KeyCode.E;

            if (KeybindManager.Instance != null && KeybindManager.Instance.keys.TryGetValue("Burst", out KeyCode burstKeyCode))
            {
                burstKey = burstKeyCode;
            }
            else if (PlayerPrefs.HasKey("Key_Burst"))
            {
                burstKey = (KeyCode)PlayerPrefs.GetInt("Key_Burst");
            }

            if (KeybindManager.Instance != null && KeybindManager.Instance.keys.TryGetValue("Skill", out KeyCode skillKeyCode))
            {
                skillKey = skillKeyCode;
            }
            else if (PlayerPrefs.HasKey("Key_Skill"))
            {
                skillKey = (KeyCode)PlayerPrefs.GetInt("Key_Skill");
            }

            if (Input.GetKeyDown(burstKey) && state.currentE_Count >= activeWeaponData.requiredE_For_Q)
            {
                ForceCancelRoll();
                anim.SetTrigger("Skill_Q");
                isAttacking = true;
                ShowWeapon();
                state.currentE_Count = 0;
                ClearBuffer();
            }

            if (Input.GetKeyDown(skillKey) && state.skillETimer <= 0f)
            {
                ForceCancelRoll();
                anim.SetTrigger("Skill_E");
                isAttacking = true;
                ShowWeapon();
                state.skillETimer = activeWeaponData.skillECooldown;
                state.currentE_Count++;
                ClearBuffer();
            }
        }
        #endregion

        #region Attack Buffer
        private void UpdateBuffer()
        {
            if (!isBufferActive) return;

            bufferTimer -= Time.deltaTime;

            if (bufferTimer <= 0f)
            {
                isBufferActive = false;
                return;
            }

            if (CanInterrupt())
            {
                ExecuteAttack();
            }
        }

        private void CheckAttackState()
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(1);

            if (stateInfo.IsName("Empty") || stateInfo.IsName("Combat_Movement") || stateInfo.IsName("Rolling"))
            {
                isAttacking = false;
            }
            else
            {
                isAttacking = true;
            }

            anim.SetBool("isAttacking", isAttacking);
        }

        private void ForceCancelRoll()
        {
            if (movementScript.isRolling)
            {
                movementScript.isRolling = false;
                anim.SetBool("isRolling", false);
            }
        }

        private void RequestAttack()
        {
            isBufferActive = true;
            bufferTimer = bufferDuration;

            if (!isAttacking)
            {
                ExecuteAttack();
            }
        }

        private void ExecuteAttack()
        {
            anim.SetTrigger("Attack");
            isAttacking = true;
            ShowWeapon();
            ClearBuffer();
        }

        private void ClearBuffer()
        {
            isBufferActive = false;
            bufferTimer = 0f;
        }
        #endregion

        #region Root Motion
        private void OnAnimatorMove()
        {
            if (anim == null || controller == null) return;
            if (!IsRootMotionAnimatorState()) return;

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(1);
            bool isRolling = stateInfo.IsName("Rolling");

            Vector3 finalMovement = anim.deltaPosition;
            finalMovement.y = -20f * Time.deltaTime;

            Transform moveRoot = controller.transform;
            if (!isRolling)
                moveRoot.rotation *= anim.deltaRotation;

            controller.Move(finalMovement);
        }

        private static bool IsCombatCameraFollowState(AnimatorStateInfo stateInfo)
        {
            return stateInfo.IsName("Attack_1") || stateInfo.IsName("Attack_2") || stateInfo.IsName("Attack_3")
                || stateInfo.IsName("Skill_E") || stateInfo.IsName("Skill_Q");
        }

        private bool IsRootMotionAnimatorState()
        {
            if (anim == null) return false;

            if (anim.IsInTransition(1))
            {
                AnimatorStateInfo nextState = anim.GetNextAnimatorStateInfo(1);
                if (IsRootMotionStateName(nextState)) return true;
            }

            return IsRootMotionStateName(anim.GetCurrentAnimatorStateInfo(1));
        }

        private static bool IsRootMotionStateName(AnimatorStateInfo stateInfo)
        {
            return stateInfo.IsName("Attack_1") || stateInfo.IsName("Attack_2") || stateInfo.IsName("Attack_3")
                || stateInfo.IsName("Skill_E") || stateInfo.IsName("Skill_Q") || stateInfo.IsName("Rolling");
        }
        #endregion
    }
}
