using UnityEngine;

namespace VisionOfLight.Player
{
    /// <summary>Item and quantity required for an ascension phase.</summary>
    [System.Serializable]
    public class ItemRequirement
    {
        public ItemData item;
        public int amount;
    }

    /// <summary>Level cap increase and material costs for one ascension tier.</summary>
    [System.Serializable]
    public class AscensionPhase
    {
        public int newLevelCap;
        public ItemRequirement[] requiredItems;
    }

    /// <summary>Saved stat allocation and hotbar layout for a build slot.</summary>
    [System.Serializable]
    public class BuildLoadout
    {
        public bool isSaved;
        public int hpPoints;
        public int atkPoints;
        public int defPoints;
        public int stmPoints;
        public ItemData[] hotbarSlots = new ItemData[4];
        public string[] savedHotbarItemNames = new string[4];
    }

    /// <summary>Tracks upgrade level for a single weapon by name.</summary>
    [System.Serializable]
    public class WeaponLevelEntry
    {
        public string weaponName;
        public int level = 1;
    }
}
