using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Game Data/Inventory/Consumable")]
public class ConsumableItemData : ItemData
{
    [Header("Heal Settings")]
    public float instantHeal = 200f;
    public float tickHealAmount = 100f;
    public float tickInterval = 5f;
    public int totalTicks = 4;

    [Header("Cooldown Settings")]
    public float cooldownTime = 40f;
}