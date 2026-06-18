// Partial — weapon equip/unequip and spawn points.
using UnityEngine;

namespace VisionOfLight.Player
{
    public partial class PlayerCombat
    {
        #region Public API
        /// <summary>Equips a weapon, swaps animator override, spawns the model, and applies element spawn points.</summary>
        public void EquipWeapon(WeaponItemData newWeaponData)
        {
            if (newWeaponData == null) return;

            ClearActiveQSystems();

            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
            }

            activeWeaponData = newWeaponData;

            if (activeWeaponData.animatorOverride != null)
            {
                anim.runtimeAnimatorController = activeWeaponData.animatorOverride;
            }
            else
            {
                anim.runtimeAnimatorController = baseAnimatorController;
            }

            if (activeWeaponData.weaponModelPrefab != null && weaponHandPosition != null)
            {
                currentWeaponModel = Instantiate(activeWeaponData.weaponModelPrefab, weaponHandPosition);
                currentWeaponModel.SetActive(inCombatStance);
            }

            ApplySpawnPointsForWeapon(activeWeaponData);
        }

        /// <summary>Unequips the active weapon, clears Q systems, and resets combat animator state.</summary>
        public void UnequipCurrentWeapon()
        {
            ClearActiveQSystems();

            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
                currentWeaponModel = null;
            }

            activeWeaponData = null;

            if (anim != null)
            {
                AnimatorStateInfo combatState = anim.GetCurrentAnimatorStateInfo(1);

                if (!combatState.IsName("Empty"))
                {
                    anim.Play("Movement", 0);
                    anim.Play("Empty", 1);
                }

                anim.runtimeAnimatorController = baseAnimatorController;
                anim.SetBool("isAttacking", false);
                anim.SetBool("isRolling", false);
                anim.ResetTrigger("Attack");
                anim.ResetTrigger("Skill_E");
                anim.ResetTrigger("Skill_Q");
            }

            inCombatStance = false;
            isAttacking = false;
        }
        #endregion

        #region Spawn Points
        /// <summary>Caches E/Q spawn transforms for each weapon element under the player hierarchy.</summary>
        private void CacheElementSpawnPoints()
        {
            RegisterElementSpawnPoints(WeaponItemData.WeaponElement.Wind, "Wind_E_SpawnPoint", "Wind_Q_SpawnPoint");
            RegisterElementSpawnPoints(WeaponItemData.WeaponElement.Ice, "Ice_E_SpawnPoint", "Ice_Q_SpawnPoint");
            RegisterElementSpawnPoints(WeaponItemData.WeaponElement.Fire, "Fire_E_SpawnPoint", "Fire_Q_SpawnPoint");

            if (!spawnPointsByElement.ContainsKey(WeaponItemData.WeaponElement.Wind))
            {
                RegisterElementSpawnPoints(WeaponItemData.WeaponElement.Wind, "E_SpawnPoint", "Q_SpawnPoint");
            }
        }

        private void RegisterElementSpawnPoints(
            WeaponItemData.WeaponElement element,
            string ePointName,
            string qPointName)
        {
            Transform ePoint = FindPlayerSpawnPoint(ePointName);
            Transform qPoint = FindPlayerSpawnPoint(qPointName);

            if (ePoint == null && qPoint == null) return;

            spawnPointsByElement[element] = new ElementSpawnPoints { e = ePoint, q = qPoint };
        }

        private Transform FindPlayerSpawnPoint(string pointName)
        {
            Transform searchRoot = transform;
            while (searchRoot.parent != null && searchRoot.name != "Player")
            {
                searchRoot = searchRoot.parent;
            }

            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == pointName) return child;
            }

            return null;
        }

        private void ApplySpawnPointsForWeapon(WeaponItemData weapon)
        {
            if (weapon == null
                || !spawnPointsByElement.TryGetValue(weapon.weaponElement, out ElementSpawnPoints points))
            {
                eSpawnPoint = defaultESpawnPoint;
                qSpawnPoint = defaultQSpawnPoint;
                return;
            }

            eSpawnPoint = points.e != null ? points.e : defaultESpawnPoint;
            qSpawnPoint = points.q != null ? points.q : defaultQSpawnPoint;
        }
        #endregion

        #region Q System Cleanup
        private void ReleaseActiveQSystemReferences()
        {
            activeFireQSystem = null;
            activeIceQSystem = null;
        }

        private void ClearActiveQSystems()
        {
            ClearFireQOrbs();
            ClearIceQSystem();
        }

        private void ClearIceQSystem()
        {
            if (activeIceQSystem == null) return;

            activeIceQSystem.Cleanup();
            activeIceQSystem = null;
        }

        private void ClearFireQOrbs()
        {
            if (activeFireQSystem == null) return;

            activeFireQSystem.Cleanup();
            activeFireQSystem = null;
        }
        #endregion
    }
}
