// Partial — normal attack damage and hit detection.
using UnityEngine;
using VisionOfLight.Enemy;

namespace VisionOfLight.Player
{
    public partial class PlayerCombat
    {
        #region Damage Calculation
        private int GetWeaponUpgradeBoost(WeaponItemData weapon)
        {
            if (playerData == null || weapon == null) return 0;

            int currentLvl = playerData.GetWeaponLevel(weapon.itemName);
            return weapon.GetTotalBoostUntil(currentLvl - 1);
        }
        #endregion

        #region Melee Attack
        /// <summary>Animation event — deals normal attack damage to enemies within range and angle.</summary>
        public void DealNormalDamage()
        {
            if (activeWeaponData == null) return;

            float playerBaseAttack = playerData != null ? playerData.GetTotalAttack() : 0f;
            int weaponBoost = GetWeaponUpgradeBoost(activeWeaponData);
            float totalAttack = playerBaseAttack + activeWeaponData.weaponBaseAttack + weaponBoost;
            float finalDamage = totalAttack * (activeWeaponData.normalAttackDamage / 100f);

            Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

            foreach (Collider enemy in hitEnemies)
            {
                EnemyBase enemyBase = enemy.GetComponent<EnemyBase>() ?? enemy.GetComponentInParent<EnemyBase>();

                if (enemyBase != null && !enemyBase.IsDead)
                {
                    Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                    directionToEnemy.y = 0;

                    float angle = Vector3.Angle(transform.forward, directionToEnemy);

                    if (angle <= attackAngle)
                    {
                        enemyBase.TakeDamage(finalDamage, true, activeWeaponData.weaponElement);
                    }
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Vector3 rightDir = Quaternion.Euler(0, attackAngle, 0) * transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngle, 0) * transform.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, rightDir * attackRange);
            Gizmos.DrawRay(transform.position, leftDir * attackRange);
        }
        #endregion
    }
}
