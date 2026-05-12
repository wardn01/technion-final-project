using UnityEngine;

public class UI_InputManager : MonoBehaviour
{
    public static UI_InputManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
            UpdatePlayerInputLock();
            return; 
        }

        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (!IsShopOrDialogueOpen() && !CharacterMenuController.Instance.attributesScreen.activeSelf) 
            {
                InventoryUIManager.Instance.ToggleInventory(); 
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!IsShopOrDialogueOpen() && !InventoryUIManager.Instance.inventoryWindow.activeSelf)
            {
                CharacterMenuController.Instance.ToggleMenu();
            }
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (!IsShopOrDialogueOpen() && !InventoryUIManager.Instance.inventoryWindow.activeSelf && !CharacterMenuController.Instance.attributesScreen.activeSelf)
            {
                FindObjectOfType<FullMapController>()?.ToggleMap();
            }
        }

        UpdatePlayerInputLock();
    }

    private void UpdatePlayerInputLock()
    {
        if (PlayerInputManager.Instance != null)
        {
            bool isAnyScreenOpen = IsShopOrDialogueOpen() || 
                                   (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf) ||
                                   (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf) ||
                                   (FindObjectOfType<FullMapController>() != null && FindObjectOfType<FullMapController>().fullMapScreen.activeSelf);
                                   
            PlayerInputManager.Instance.isInputLocked = isAnyScreenOpen;
        }
    }

    private bool IsShopOrDialogueOpen()
    {
        bool isShop = ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf;
        bool isDialogue = UIManager.Instance != null && UIManager.Instance.isDialogueOpen;
        return isShop || isDialogue;
    }

    private void HandleEscapeKey()
    {
        if (ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf)
        {
            ShopManager.Instance.BackToDialogue();
            return; 
        }

        if (UIManager.Instance != null && UIManager.Instance.isDialogueOpen)
        {
            if (ShopManager.Instance != null) ShopManager.Instance.CloseDialogue();
            return;
        }

        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf)
        {
            InventoryUIManager.Instance.ToggleInventory();
            return;
        }

        if (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf)
        {
            CharacterMenuController.Instance.ToggleMenu();
            return;
        }

        FullMapController map = FindObjectOfType<FullMapController>();
        if (map != null && map.fullMapScreen.activeSelf)
        {
            map.ToggleMap();
            return;
        }
    }
}