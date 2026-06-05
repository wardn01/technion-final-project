using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class UI_InputManager : MonoBehaviour
{
    public static UI_InputManager Instance { get; private set; }

    private FullMapController fullMapController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        fullMapController = FindAnyObjectByType<FullMapController>();
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
        bool questKey = false;

        if (KeybindManager.Instance != null)
        {
            var keys = KeybindManager.Instance.keys;
            
            if (keys.TryGetValue("OpenInventory", out KeyCode openInventoryKey))
                inventoryKey = Input.GetKeyDown(openInventoryKey) || Input.GetKeyDown(KeyCode.Tab);
            else
                inventoryKey = Input.GetKeyDown(KeyCode.Tab);

            if (keys.TryGetValue("OpenCharacterScreen", out KeyCode openCharacterKey))
                characterKey = Input.GetKeyDown(openCharacterKey);

            if (keys.TryGetValue("OpenMap", out KeyCode openMapKey))
                mapKey = Input.GetKeyDown(openMapKey);

            if (keys.TryGetValue("OpenQuests", out KeyCode openQuestKey))
                questKey = Input.GetKeyDown(openQuestKey);
        }
        else
        {
            inventoryKey = Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
            characterKey = Input.GetKeyDown(KeyCode.C);
            mapKey = Input.GetKeyDown(KeyCode.M);
            questKey = Input.GetKeyDown(KeyCode.J);
        }

        bool isInvOpen = InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf;
        bool isCharOpen = CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf;
        bool isMapOpen = fullMapController != null && fullMapController.fullMapScreen.activeSelf;
        bool isQuestOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.questScreen != null && PauseMenuManager.Instance.questScreen.activeSelf;

        if (inventoryKey)
        {
            if (!IsShopOrDialogueOpen() && !isCharOpen && !isMapOpen && !isQuestOpen) 
            {
                if (InventoryUIManager.Instance != null) InventoryUIManager.Instance.ToggleInventory(); 
            }
        }

        if (characterKey)
        {
            if (!IsShopOrDialogueOpen() && !isInvOpen && !isMapOpen && !isQuestOpen)
            {
                if (CharacterMenuController.Instance != null) CharacterMenuController.Instance.ToggleMenu();
            }
        }

        if (mapKey)
        {
            if (!IsShopOrDialogueOpen() && !isInvOpen && !isCharOpen && !isQuestOpen)
            {
                if (fullMapController == null)
                    fullMapController = FindAnyObjectByType<FullMapController>();

                fullMapController?.ToggleMap();
            }
        }

        if (questKey)
        {
            if (isQuestOpen)
            {
                if (PauseMenuManager.Instance != null)
                    PauseMenuManager.Instance.Resume();
            }
            else if (!IsShopOrDialogueOpen() && !isInvOpen && !isCharOpen && !isMapOpen)
            {
                if (PauseMenuManager.Instance != null)
                {
                    if (!PauseMenuManager.Instance.isPaused)
                    {
                        PauseMenuManager.Instance.Pause();
                        PauseMenuManager.Instance.openedFromHotkey = true; 
                    }
                    PauseMenuManager.Instance.OpenQuests();
                }

                if (QuestUIController.Instance != null)
                    QuestUIController.Instance.RefreshQuestUI();
            }
        }
    }

    private void UpdatePlayerInputLock()
    {
        if (Player_InputManager.Instance != null)
        {
            bool isPauseMenuOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused;
            if (fullMapController == null)
                fullMapController = FindAnyObjectByType<FullMapController>();

            bool isQuestOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.questScreen != null && PauseMenuManager.Instance.questScreen.activeSelf;

            bool isAnyScreenOpen = IsShopOrDialogueOpen() ||
                                   (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf) ||
                                   (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf) ||
                                   (fullMapController != null && fullMapController.fullMapScreen.activeSelf) ||
                                   isQuestOpen ||
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
        if (ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf)
        {
            ShopManager.Instance.BackToDialogue();
            return; 
        }

        if (UIManager.Instance != null && UIManager.Instance.isDialogueOpen)
        {
            if (DialogueManager.Instance != null) DialogueManager.Instance.EndDialogue();
            return;
        }

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused)
        {
            PauseMenuManager.Instance.HandleBackButton();
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

        if (fullMapController == null)
            fullMapController = FindAnyObjectByType<FullMapController>();

        if (fullMapController != null && fullMapController.fullMapScreen != null && fullMapController.fullMapScreen.activeSelf)
        {
            fullMapController.ToggleMap();
            return;
        }

        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.Pause();
        }
    }
}