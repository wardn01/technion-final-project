using System.Collections.Generic;
using UnityEngine;

namespace VisionOfLight.Player
{
    /// <summary>
    /// ScriptableObject holding player progression, stat allocation, weapon levels, build loadouts, and save/load helpers.
    /// </summary>
    [CreateAssetMenu(fileName = "MainPlayerData", menuName = "Game Data/Player Data")]
    public class PlayerData : ScriptableObject
    {
        #region Loadouts
        public int currentActiveLoadout = 0;

        [Header("Build Loadouts")]
        public BuildLoadout[] loadouts = new BuildLoadout[3] { new BuildLoadout(), new BuildLoadout(), new BuildLoadout() };
        #endregion

        #region Level & XP
        [Header("Level & XP System")]
        public int currentLevel = 1;
        public int currentXP = 0;
        public int xpToNextLevel = 100;
        public int maxLevelCap = 10;
        public int absoluteMaxLevel = 100;

        /// <summary>Adds XP and levels up while below <see cref="maxLevelCap"/>.</summary>
        public void AddXP(int amount)
        {
            if (currentLevel >= maxLevelCap || currentLevel >= absoluteMaxLevel)
            {
                currentXP = 0;
                return;
            }

            currentXP += amount;

            while (currentXP >= xpToNextLevel && currentLevel < maxLevelCap)
            {
                LevelUp();
            }

            if (currentLevel >= maxLevelCap)
            {
                currentLevel = maxLevelCap;
                currentXP = 0;
            }
        }

        private void LevelUp()
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            availableStatPoints++;
            xpToNextLevel += 100;
        }
        #endregion

        #region Ascension
        [Header("Ascension System")]
        public AscensionPhase[] ascensionPhases;
        public int currentAscensionIndex = 0;

        /// <summary>Returns true when the player meets level and item requirements for the next ascension.</summary>
        public bool CanAscend()
        {
            if (currentLevel < maxLevelCap) return false;
            if (currentAscensionIndex >= ascensionPhases.Length) return false;

            AscensionPhase phase = ascensionPhases[currentAscensionIndex];
            if (phase.requiredItems == null || phase.requiredItems.Length == 0) return true;

            foreach (var req in phase.requiredItems)
            {
                if (req.item == null) continue;
                if (InventoryManager.Instance.GetItemAmount(req.item) < req.amount)
                    return false;
            }

            return true;
        }

        /// <summary>Consumes ascension materials and raises <see cref="maxLevelCap"/> when eligible.</summary>
        public void TryAscend()
        {
            if (!CanAscend()) return;

            AscensionPhase phase = ascensionPhases[currentAscensionIndex];

            if (phase.requiredItems != null)
            {
                foreach (var req in phase.requiredItems)
                {
                    if (req.item != null)
                    {
                        InventoryManager.Instance.RemoveItem(req.item, req.amount);
                    }
                }
            }

            maxLevelCap = phase.newLevelCap;
            currentAscensionIndex++;
        }
        #endregion

        #region Stats
        [Header("Stat Points")]
        public int availableStatPoints = 1;
        public int investedHPPoints = 0;
        public int investedAtkPoints = 0;
        public int investedDefPoints = 0;
        public int investedStaminaPoints = 0;

        [Header("Base Stats (Level 1)")]
        public int baseAttack = 15;
        public int baseDefense = 10;
        public int baseMaxHealth = 150;
        public float baseMaxStamina = 100f;

        [Header("Stat Multipliers (Per Point)")]
        public int healthPerPoint = 20;
        public int attackPerPoint = 3;
        public int defensePerPoint = 2;
        public float staminaPerPoint = 10f;
        public float absoluteMaxStamina = 250f;

        public enum StatType { HP, Attack, Defense, Stamina }

        /// <summary>Spends one available stat point on the chosen stat type.</summary>
        public void AllocateStatPoint(StatType stat)
        {
            if (availableStatPoints <= 0) return;

            switch (stat)
            {
                case StatType.HP: investedHPPoints++; break;
                case StatType.Attack: investedAtkPoints++; break;
                case StatType.Defense: investedDefPoints++; break;
                case StatType.Stamina:
                    if (GetTotalMaxStamina() + staminaPerPoint > absoluteMaxStamina) return;
                    investedStaminaPoints++; break;
            }
            availableStatPoints--;
        }

        /// <summary>Refunds one invested point from the chosen stat type.</summary>
        public void RemoveStatPoint(StatType stat)
        {
            switch (stat)
            {
                case StatType.HP: if (investedHPPoints > 0) { investedHPPoints--; availableStatPoints++; } break;
                case StatType.Attack: if (investedAtkPoints > 0) { investedAtkPoints--; availableStatPoints++; } break;
                case StatType.Defense: if (investedDefPoints > 0) { investedDefPoints--; availableStatPoints++; } break;
                case StatType.Stamina: if (investedStaminaPoints > 0) { investedStaminaPoints--; availableStatPoints++; } break;
            }
        }

        /// <summary>Refunds all invested stat points back to <see cref="availableStatPoints"/>.</summary>
        public void ResetStatPoints()
        {
            availableStatPoints += (investedHPPoints + investedAtkPoints + investedDefPoints + investedStaminaPoints);
            investedHPPoints = investedAtkPoints = investedDefPoints = investedStaminaPoints = 0;
        }

        public int GetTotalMaxHealth() => baseMaxHealth + (investedHPPoints * healthPerPoint);
        public int GetTotalAttack() => baseAttack + (investedAtkPoints * attackPerPoint);
        public int GetTotalDefense() => baseDefense + (investedDefPoints * defensePerPoint);
        public float GetTotalMaxStamina() => baseMaxStamina + (investedStaminaPoints * staminaPerPoint);
        #endregion

        #region Weapon Levels
        [Header("Weapon Levels")]
        public List<WeaponLevelEntry> weaponLevelEntries = new List<WeaponLevelEntry>();

        /// <summary>Returns the upgrade level for <paramref name="weaponName"/>, defaulting to 1.</summary>
        public int GetWeaponLevel(string weaponName)
        {
            foreach (WeaponLevelEntry entry in weaponLevelEntries)
            {
                if (entry.weaponName == weaponName)
                    return entry.level;
            }

            return 1;
        }

        /// <summary>Increments the level for <paramref name="weaponName"/> or adds a new entry at level 2.</summary>
        public void LevelUpWeapon(string weaponName)
        {
            for (int i = 0; i < weaponLevelEntries.Count; i++)
            {
                if (weaponLevelEntries[i].weaponName == weaponName)
                {
                    weaponLevelEntries[i].level++;
                    return;
                }
            }

            weaponLevelEntries.Add(new WeaponLevelEntry { weaponName = weaponName, level = 2 });
        }
        #endregion

        #region Save/Load
        /// <summary>Saves current stat allocation and hotbar into the given build slot.</summary>
        public void SaveBuild(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= loadouts.Length) return;

            loadouts[slotIndex].hpPoints = investedHPPoints;
            loadouts[slotIndex].atkPoints = investedAtkPoints;
            loadouts[slotIndex].defPoints = investedDefPoints;
            loadouts[slotIndex].stmPoints = investedStaminaPoints;

            for (int i = 0; i < 4; i++)
            {
                if (QuickSlotManager.Instance != null)
                    loadouts[slotIndex].hotbarSlots[i] = QuickSlotManager.Instance.slots[i];
            }
            loadouts[slotIndex].isSaved = true;
        }

        /// <summary>Loads stat allocation and hotbar from the given build slot when enough points are available.</summary>
        public void LoadBuild(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= loadouts.Length) return;

            currentActiveLoadout = slotIndex;

            int totalPoints = availableStatPoints + investedHPPoints + investedAtkPoints + investedDefPoints + investedStaminaPoints;
            int requiredPoints = loadouts[slotIndex].hpPoints + loadouts[slotIndex].atkPoints + loadouts[slotIndex].defPoints + loadouts[slotIndex].stmPoints;

            if (requiredPoints <= totalPoints)
            {
                investedHPPoints = loadouts[slotIndex].hpPoints;
                investedAtkPoints = loadouts[slotIndex].atkPoints;
                investedDefPoints = loadouts[slotIndex].defPoints;
                investedStaminaPoints = loadouts[slotIndex].stmPoints;
                availableStatPoints = totalPoints - requiredPoints;

                if (QuickSlotManager.Instance != null)
                {
                    PlayerCombat pc = PlayerRegistry.Instance?.Combat;
                    if (pc != null) pc.UnequipCurrentWeapon();

                    for (int i = 0; i < 4; i++)
                    {
                        QuickSlotManager.Instance.slots[i] = loadouts[slotIndex].hotbarSlots[i];
                    }
                    QuickSlotManager.Instance.ResetSelection();
                    QuickSlotManager.Instance.UpdateUI();
                }
            }
        }

        /// <summary>Syncs active loadout and serializes hotbar item names before writing save data.</summary>
        public void PrepareForSave()
        {
            if (QuickSlotManager.Instance != null)
            {
                SaveBuild(currentActiveLoadout);
            }

            for (int j = 0; j < loadouts.Length; j++)
            {
                if (loadouts[j].savedHotbarItemNames == null || loadouts[j].savedHotbarItemNames.Length != 4)
                    loadouts[j].savedHotbarItemNames = new string[4];

                for (int i = 0; i < 4; i++)
                {
                    if (loadouts[j].hotbarSlots[i] != null)
                        loadouts[j].savedHotbarItemNames[i] = loadouts[j].hotbarSlots[i].name;
                    else
                        loadouts[j].savedHotbarItemNames[i] = "";
                }
            }
        }

        /// <summary>Rehydrates hotbar slots from saved item names after loading.</summary>
        public void RestoreAfterLoad()
        {
            ItemData[] allItems = Resources.LoadAll<ItemData>("");
            for (int j = 0; j < loadouts.Length; j++)
            {
                if (loadouts[j].savedHotbarItemNames == null) continue;

                for (int i = 0; i < 4; i++)
                {
                    string itemName = loadouts[j].savedHotbarItemNames[i];
                    loadouts[j].hotbarSlots[i] = null;

                    if (!string.IsNullOrEmpty(itemName))
                    {
                        foreach (ItemData itemAsset in allItems)
                        {
                            if (itemAsset.name == itemName)
                            {
                                loadouts[j].hotbarSlots[i] = itemAsset;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Resets all progression, stats, weapon levels, and loadouts to default values.</summary>
        public void ResetToDefault()
        {
            currentActiveLoadout = 0;
            currentLevel = 1;
            currentXP = 0;
            xpToNextLevel = 100;
            maxLevelCap = 10;
            currentAscensionIndex = 0;

            availableStatPoints = 1;
            investedHPPoints = 0;
            investedAtkPoints = 0;
            investedDefPoints = 0;
            investedStaminaPoints = 0;

            weaponLevelEntries.Clear();

            for (int i = 0; i < loadouts.Length; i++)
            {
                loadouts[i].isSaved = false;
                loadouts[i].hpPoints = 0;
                loadouts[i].atkPoints = 0;
                loadouts[i].defPoints = 0;
                loadouts[i].stmPoints = 0;

                if (loadouts[i].hotbarSlots == null || loadouts[i].hotbarSlots.Length != 4)
                    loadouts[i].hotbarSlots = new ItemData[4];

                if (loadouts[i].savedHotbarItemNames == null || loadouts[i].savedHotbarItemNames.Length != 4)
                    loadouts[i].savedHotbarItemNames = new string[4];

                for (int j = 0; j < 4; j++)
                {
                    loadouts[i].hotbarSlots[j] = null;
                    loadouts[i].savedHotbarItemNames[j] = "";
                }
            }
        }
        #endregion
    }
}
