// Partial — combat stance timer and weapon visibility.
using UnityEngine;

namespace VisionOfLight.Player
{
    public partial class PlayerCombat
    {
        #region Combat Stance
        private void HandleCombatTimer()
        {
            if (inCombatStance && !isAttacking)
            {
                combatTimer -= Time.deltaTime;

                if (combatTimer <= 0f)
                {
                    ExitCombatStance();
                }
            }
        }

        /// <summary>Leaves combat stance, hides the weapon, and plays sheath or empty-layer animation.</summary>
        public void ExitCombatStance(bool isRunning = false)
        {
            inCombatStance = false;
            HideWeapon();

            if (!isRunning)
            {
                anim.SetTrigger("SheathWeapon");
            }
            else
            {
                anim.Play("Empty", 1, 0f);
            }
        }

        /// <summary>Shows the weapon model and resets the combat stance idle timer.</summary>
        public void ShowWeapon()
        {
            if (currentWeaponModel != null)
            {
                currentWeaponModel.SetActive(true);
            }

            inCombatStance = true;
            combatTimer = combatStanceDuration;
        }

        /// <summary>Hides the weapon model without leaving combat stance.</summary>
        public void HideWeapon()
        {
            if (currentWeaponModel != null)
            {
                currentWeaponModel.SetActive(false);
            }
        }
        #endregion
    }
}
