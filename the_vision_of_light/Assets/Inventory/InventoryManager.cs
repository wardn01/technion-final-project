using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public Dictionary<ItemData, int> GetInventory() => inventory;

    private Dictionary<ItemData, int> inventory = new Dictionary<ItemData, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null) return;

        if (inventory.ContainsKey(item))
        {
            inventory[item] += amount;
        }
        else
        {
            inventory.Add(item, amount);
        }

        Debug.Log($"🎒 [Inventory] Picked up {amount}x {item.itemName}. Total: {inventory[item]}");
    }

    public int GetItemAmount(ItemData item)
    {
        if (inventory.ContainsKey(item))
        {
            return inventory[item];
        }
        return 0;
    }
}