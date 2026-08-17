using UnityEngine;

namespace VisionOfLight.Player
{
    /// <summary>Item and quantity required for an ascension phase.</summary>
    [System.Serializable]
    public class ItemRequirement
    {
        #region Serialized Fields
        public ItemData item;
        public int amount;

        /// <summary>
        /// Survives <see cref="JsonUtility"/> (unlike <see cref="item"/>) so requirements
        /// can be re-linked after save/load.
        /// </summary>
        public string itemName;
        #endregion
    }

    /// <summary>Level cap increase and material costs for one ascension tier.</summary>
    [System.Serializable]
    public class AscensionPhase
    {
        #region Serialized Fields
        public int newLevelCap;
        public ItemRequirement[] requiredItems;
        #endregion
    }

    /// <summary>Saved stat allocation and hotbar layout for a build slot.</summary>
    [System.Serializable]
    public class BuildLoadout
    {
        #region Serialized Fields
        public bool isSaved;
        public int hpPoints;
        public int atkPoints;
        public int defPoints;
        public int stmPoints;
        public ItemData[] hotbarSlots = new ItemData[4];
        public string[] savedHotbarItemNames = new string[4];
        #endregion
    }

    /// <summary>Tracks upgrade level for a single weapon by name.</summary>
    [System.Serializable]
    public class WeaponLevelEntry
    {
        #region Serialized Fields
        public string weaponName;
        public int level = 1;
        #endregion
    }
}
