// Partial — skill E/Q execution and combat audio.
using UnityEngine;

namespace VisionOfLight.Player
{
    public partial class PlayerCombat
    {
        #region Skill E
        /// <summary>Animation event — spawns the E skill prefab and applies scaled damage.</summary>
        public void OnSkillE()
        {
            if (activeWeaponData != null && activeWeaponData.skillEPrefab != null && eSpawnPoint != null)
            {
                GameObject skill = Instantiate(activeWeaponData.skillEPrefab, eSpawnPoint.position, eSpawnPoint.rotation);

                float playerBaseAttack = playerData != null ? playerData.GetTotalAttack() : 0f;
                int weaponBoost = GetWeaponUpgradeBoost(activeWeaponData);
                float totalAttack = playerBaseAttack + activeWeaponData.weaponBaseAttack + weaponBoost;
                float finalDamage = totalAttack * (activeWeaponData.skillEDamage / 100f);

                WindSkillEDamage windScript = skill.GetComponent<WindSkillEDamage>();
                if (windScript != null)
                {
                    windScript.SetDamage(finalDamage);
                }

                IceSkillEDamage iceScript = skill.GetComponent<IceSkillEDamage>();
                if (iceScript != null)
                {
                    iceScript.SetDamage(finalDamage);
                }

                FireSkillEDamage fireScript = skill.GetComponent<FireSkillEDamage>();
                if (fireScript != null)
                {
                    fireScript.SetDamage(finalDamage);
                }
            }
        }
        #endregion

        #region Skill Q
        /// <summary>Animation event — executes element-specific Q skill logic and damage.</summary>
        public void OnSkillQ()
        {
            if (activeWeaponData == null) return;

            float playerBaseAttack = playerData != null ? playerData.GetTotalAttack() : 0f;
            int weaponBoost = GetWeaponUpgradeBoost(activeWeaponData);
            float totalAttack = playerBaseAttack + activeWeaponData.weaponBaseAttack + weaponBoost;
            float finalDamage = totalAttack * (activeWeaponData.skillQDamage / 100f);

            if (activeWeaponData.weaponElement == WeaponItemData.WeaponElement.Fire)
            {
                int boomPercent = activeWeaponData.skillQStrikeDamage > 0
                    ? activeWeaponData.skillQStrikeDamage
                    : activeWeaponData.skillQDamage;

                float boomDamage = CalculateSkillDamage(boomPercent);
                float orbDamage = CalculateSkillDamage(activeWeaponData.skillQDamage);
                HandleFireSkillQ(boomDamage, orbDamage);
                return;
            }

            if (activeWeaponData.weaponElement == WeaponItemData.WeaponElement.Ice)
            {
                HandleIceSkillQ(CalculateSkillDamage(activeWeaponData.skillQDamage));
                return;
            }

            if (activeWeaponData.skillQPrefab == null || qSpawnPoint == null) return;

            GameObject skill = Instantiate(
                activeWeaponData.skillQPrefab,
                qSpawnPoint.position,
                qSpawnPoint.rotation);

            WindSkillQDamage windScript = skill.GetComponent<WindSkillQDamage>();
            if (windScript != null)
                windScript.SetDamage(finalDamage);
        }

        private float CalculateSkillDamage(int damagePercent)
        {
            if (activeWeaponData == null) return 0f;

            float playerBaseAttack = playerData != null ? playerData.GetTotalAttack() : 0f;
            int weaponBoost = GetWeaponUpgradeBoost(activeWeaponData);
            float totalAttack = playerBaseAttack + activeWeaponData.weaponBaseAttack + weaponBoost;
            return totalAttack * (damagePercent / 100f);
        }

        private void HandleIceSkillQ(float ballDamage)
        {
            if (activeIceQSystem == null)
                activeIceQSystem = GetComponentInChildren<IceSwordQSystem>();

            if (activeIceQSystem != null && activeIceQSystem.IsActive)
            {
                activeIceQSystem.Activate(ballDamage, enemyLayer);
                return;
            }

            activeIceQSystem = null;

            if (activeWeaponData.skillQPrefab == null) return;

            GameObject skill = Instantiate(activeWeaponData.skillQPrefab, transform.position, Quaternion.identity);
            IceSwordQSystem iceSystem = skill.GetComponent<IceSwordQSystem>();
            if (iceSystem == null) return;

            activeIceQSystem = iceSystem;
            iceSystem.Initialize(transform, ballDamage, enemyLayer);
        }

        private void HandleFireSkillQ(float boomDamage, float orbDamage)
        {
            SpawnFireSkillQBoom(boomDamage);

            if (activeFireQSystem == null)
                activeFireQSystem = GetComponentInChildren<FireSwordQOrbitSystem>();

            if (activeFireQSystem != null && activeFireQSystem.IsActive)
            {
                activeFireQSystem.RefreshOrbs(orbDamage);
                return;
            }

            activeFireQSystem = null;

            if (activeWeaponData.skillQPrefab == null) return;

            GameObject skill = Instantiate(activeWeaponData.skillQPrefab, transform.position, Quaternion.identity);
            FireSwordQOrbitSystem fireOrbit = skill.GetComponent<FireSwordQOrbitSystem>();
            if (fireOrbit == null) return;

            activeFireQSystem = fireOrbit;
            fireOrbit.Initialize(transform, orbDamage, enemyLayer);
        }

        private void SpawnFireSkillQBoom(float finalDamage)
        {
            if (activeWeaponData.skillQStrikePrefab == null || qSpawnPoint == null) return;

            GameObject boom = Instantiate(
                activeWeaponData.skillQStrikePrefab,
                qSpawnPoint.position,
                qSpawnPoint.rotation);

            FireSkillQBoom boomScript = boom.GetComponent<FireSkillQBoom>();
            if (boomScript != null)
                boomScript.SetDamage(finalDamage);
        }
        #endregion

        #region Combat Audio
        /// <summary>Animation event — plays the equipped weapon's normal attack sound.</summary>
        public void PlayNormalAttackSound()
        {
            if (currentWeaponModel != null)
            {
                currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayNormalAttackSound();
            }
        }

        /// <summary>Animation event — plays the E skill audio from the active weapon prefab.</summary>
        public void PlaySkillESound()
        {
            if (activeWeaponData == null) return;

            Vector3 position = eSpawnPoint != null ? eSpawnPoint.position : transform.position;
            WeaponSkillAudio.PlayFromPrefab(activeWeaponData.skillEPrefab, position);
        }

        /// <summary>Animation event — plays the Q skill audio, with a fallback for ice cast VFX.</summary>
        public void PlaySkillQSound()
        {
            if (activeWeaponData == null) return;

            Vector3 position = qSpawnPoint != null ? qSpawnPoint.position : transform.position;
            if (WeaponSkillAudio.PlayFromPrefab(activeWeaponData.skillQPrefab, position))
                return;

            PlayIceQCastSound(activeWeaponData.skillQPrefab, position);
        }

        private static void PlayIceQCastSound(GameObject qPrefab, Vector3 position)
        {
            IceSwordQSystem iceQ = qPrefab != null ? qPrefab.GetComponent<IceSwordQSystem>() : null;
            if (iceQ == null || iceQ.circleVfxPrefab == null) return;

            IceCircleQZone circle = iceQ.circleVfxPrefab.GetComponent<IceCircleQZone>();
            if (circle == null || circle.circleOpenClip == null) return;

            AudioSource.PlayClipAtPoint(circle.circleOpenClip, position, circle.circleOpenVolume);
        }

        /// <summary>Animation event — plays the equipped weapon's roll sound.</summary>
        public void PlayRollSound()
        {
            if (currentWeaponModel != null)
            {
                currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayRollSound();
            }
        }

        /// <summary>Animation event — plays the equipped weapon's combat walk sound.</summary>
        public void PlayCombatWalkSound()
        {
            if (currentWeaponModel != null)
            {
                currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayCombatWalkSound();
            }
        }
        #endregion
    }
}
