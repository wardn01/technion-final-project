using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Game Data/Inventory/Consumable")]
public class ConsumableItemData : ItemData
{
    [Header("Consumable Effects")]
    public float healAmount = 50f;

    private void OnEnable()
    {
        type = ItemType.Consumable;
    }
}