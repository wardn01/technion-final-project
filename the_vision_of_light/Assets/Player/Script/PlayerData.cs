using UnityEngine;

[System.Serializable]
public class ItemRequirement
{
    public ItemData item;
    public int amount;
}

[System.Serializable]
public class AscensionPhase
{
    public int newLevelCap;      
    public ItemRequirement[] requiredItems;
}

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance; 

    [Header("Level & XP System")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    public int maxLevelCap = 10; 
    public int absoluteMaxLevel = 100; 

    [Header("Ascension System")]
    public AscensionPhase[] ascensionPhases; 
    public int currentAscensionIndex = 0;    

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

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddXP(int amount)
    {
        if (currentLevel >= absoluteMaxLevel) return;

        currentXP += amount;

        if (currentLevel >= maxLevelCap && currentXP >= xpToNextLevel)
        {
            currentXP = xpToNextLevel; 
            return;
        }

        while (currentXP >= xpToNextLevel && currentLevel < maxLevelCap)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel; 
        currentLevel++;
        availableStatPoints++;

        xpToNextLevel += 100; 

        Debug.Log("Leveled Up! New Level: " + currentLevel + " | Next Goal: " + xpToNextLevel);
    }

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
        
        while (currentXP >= xpToNextLevel && currentLevel < maxLevelCap)
        {
            LevelUp();
        }
    }

    public enum StatType { HP, Attack, Defense, Stamina } 

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

    public void ResetStatPoints()
    {
        availableStatPoints += (investedHPPoints + investedAtkPoints + investedDefPoints + investedStaminaPoints);
        investedHPPoints = investedAtkPoints = investedDefPoints = investedStaminaPoints = 0;
    }

    public int GetTotalMaxHealth() => baseMaxHealth + (investedHPPoints * healthPerPoint);
    public int GetTotalAttack() => baseAttack + (investedAtkPoints * attackPerPoint);
    public int GetTotalDefense() => baseDefense + (investedDefPoints * defensePerPoint); 
    public float GetTotalMaxStamina() => baseMaxStamina + (investedStaminaPoints * staminaPerPoint);
}