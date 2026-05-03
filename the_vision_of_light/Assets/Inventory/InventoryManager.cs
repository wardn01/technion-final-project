using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Pickup Notifications")]
    public GameObject notificationPrefab;
    public Transform notificationParent;

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
        if (notificationPrefab != null && notificationParent != null)
        {
            GameObject notif = Instantiate(notificationPrefab, notificationParent);

            notif.transform.SetAsLastSibling(); 
            
            PickupNotification notifScript = notif.GetComponent<PickupNotification>();
            if (notifScript != null)
            {
                notifScript.Setup(item, amount);
            }
        }
    }

    public int GetItemAmount(ItemData item)
    {
        if (inventory.ContainsKey(item)) return inventory[item];
        return 0;
    }

    public void RemoveItem(ItemData item, int amount = 1)
    {
        if (inventory.ContainsKey(item))
        {
            inventory[item] -= amount;
            
            if (inventory[item] <= 0)
            {
                inventory.Remove(item);
            }
        }
    }

    public Dictionary<ItemData, int> GetInventory() => inventory;
}