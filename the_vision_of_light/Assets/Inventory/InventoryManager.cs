using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Testing")]
    public ItemData testWeaponToGive; 

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

    private void Start()
    {
        if (testWeaponToGive != null)
        {
            AddItem(testWeaponToGive, 1);
        }
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null) return;

        if (inventory.ContainsKey(item))
            inventory[item] += amount;
        else
            inventory.Add(item, amount);

        if (inventory[item] <= 0)
        {
            inventory.Remove(item);
        }
    }

    public int GetItemAmount(ItemData item)
    {
        if (inventory.ContainsKey(item)) return inventory[item];
        return 0;
    }

    public Dictionary<ItemData, int> GetInventory() => inventory;
}