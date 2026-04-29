using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Game Data/Inventory/Weapon")]
public class WeaponItemData : ItemData
{
    [Header("Weapon Visuals")]
    public GameObject weaponModelPrefab;
    public AnimatorOverrideController animatorOverride;

    [Header("Combat Stats")]
    public float normalAttackDamage = 20f;
    public float skillEDamage = 40f;
    public float skillQDamage = 100f;

    [Header("VFX & Skills")]
    public GameObject skillEPrefab;
    public GameObject skillQPrefab;

    [Header("Skill Settings")]
    public float skillECooldown = 8f;
    public int requiredE_For_Q = 2;

    private void OnEnable()
    {
        type = ItemType.Weapon;
    }
}