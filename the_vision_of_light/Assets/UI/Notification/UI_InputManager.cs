using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;

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

        bool inventoryKey = false;
        bool characterKey = false;
        bool mapKey = false;

        if (KeybindManager.Instance != null)
        {
            inventoryKey = Input.GetKeyDown(KeybindManager.Instance.keys["OpenInventory"]) || Input.GetKeyDown(KeyCode.Tab);
            characterKey = Input.GetKeyDown(KeybindManager.Instance.keys["OpenCharacterScreen"]);
            mapKey = Input.GetKeyDown(KeybindManager.Instance.keys["OpenMap"]);
        }
        else
        {
            inventoryKey = Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
            characterKey = Input.GetKeyDown(KeyCode.C);
            mapKey = Input.GetKeyDown(KeyCode.M);
        }

        if (inventoryKey)
        {
            if (!IsShopOrDialogueOpen() && CharacterMenuController.Instance != null && !CharacterMenuController.Instance.attributesScreen.activeSelf) 
            {
                if (InventoryUIManager.Instance != null) InventoryUIManager.Instance.ToggleInventory(); 
            }
        }

        if (characterKey)
        {
            if (!IsShopOrDialogueOpen() && InventoryUIManager.Instance != null && !InventoryUIManager.Instance.inventoryWindow.activeSelf)
            {
                if (CharacterMenuController.Instance != null) CharacterMenuController.Instance.ToggleMenu();
            }
        }

        if (mapKey)
        {
            bool isInvOpen = InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf;
            bool isCharOpen = CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf;
            
            if (!IsShopOrDialogueOpen() && !isInvOpen && !isCharOpen)
            {
                FindObjectOfType<FullMapController>()?.ToggleMap();
            }
        }

        UpdatePlayerInputLock();
    }

    private void UpdatePlayerInputLock()
    {
        if (Player_InputManager.Instance != null)
        {
            bool isPauseMenuOpen = InGameHandler.Instance != null && InGameHandler.Instance.isPaused;

            bool isAnyScreenOpen = IsShopOrDialogueOpen() || 
                                   (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf) ||
                                   (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf) ||
                                   (FindObjectOfType<FullMapController>() != null && FindObjectOfType<FullMapController>().fullMapScreen.activeSelf) ||
                                   isPauseMenuOpen;
                                   
            Player_InputManager.Instance.isInputLocked = isAnyScreenOpen;
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
        if (InGameHandler.Instance != null && InGameHandler.Instance.isPaused)
        {
            InGameHandler.Instance.Resume();
            return;
        }

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

        if (InGameHandler.Instance != null)
        {
            InGameHandler.Instance.Pause();
        }
    }
}