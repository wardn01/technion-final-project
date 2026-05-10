using UnityEngine;

[System.Serializable]
public struct WeaponUpgradeLevel
{
    public int goldCost; 
    public ItemRequirement[] materials; 
    public int damageBoost; 
}

[CreateAssetMenu(fileName = "New Weapon", menuName = "Game Data/Inventory/Weapon")]
public class WeaponItemData : ItemData
{
    public enum WeaponElement { None, Wind, Ice }

    [Header("Weapon Element")]
    public WeaponElement weaponElement;

    [Header("Weapon Visuals")]
    public GameObject weaponModelPrefab;
    public AnimatorOverrideController animatorOverride;

    [Header("Combat Stats")]
    public int normalAttackDamage = 20;
    public int skillEDamage = 40;
    public int skillQDamage = 100;

    [Header("VFX & Skills")]
    public GameObject skillEPrefab;
    public GameObject skillQPrefab;

    [Header("Skill Settings")]
    public float skillECooldown = 8f;
    public int requiredE_For_Q = 2;

    [Header("Upgrade System")]
    public WeaponUpgradeLevel[] upgradeLevels;

    private void OnEnable()
    {
        type = ItemType.Weapon;
    }
}