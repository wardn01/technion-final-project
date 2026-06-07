using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UI_InputManager : MonoBehaviour
{
    public static UI_InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
            UpdatePlayerInputLock();
            return;
        }

        HandleHotkeys();
        UpdatePlayerInputLock();
    }

    private void HandleHotkeys()
    {
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
        bool isMapOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.mapScreen != null && PauseMenuManager.Instance.mapScreen.activeSelf;
        bool isQuestOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.questScreen != null && PauseMenuManager.Instance.questScreen.activeSelf;

        if (inventoryKey)
        {
            if (isInvOpen)
            {
                PauseMenuManager.Instance?.Resume();
            }
            else if (!IsShopOrDialogueOpen() && !isCharOpen && !isMapOpen && !isQuestOpen)
            {
                if (PauseMenuManager.Instance != null)
                {
                    if (!PauseMenuManager.Instance.isPaused)
                    {
                        PauseMenuManager.Instance.Pause();
                        PauseMenuManager.Instance.openedFromHotkey = true;
                    }
                    PauseMenuManager.Instance.OpenInventory();
                }
            }
        }

        if (characterKey)
        {
            if (isCharOpen)
            {
                PauseMenuManager.Instance?.Resume();
            }
            else if (!IsShopOrDialogueOpen() && !isInvOpen && !isMapOpen && !isQuestOpen)
            {
                if (PauseMenuManager.Instance != null)
                {
                    if (!PauseMenuManager.Instance.isPaused)
                    {
                        PauseMenuManager.Instance.Pause();
                        PauseMenuManager.Instance.openedFromHotkey = true;
                    }
                    PauseMenuManager.Instance.OpenSetup();
                }
            }
        }

        if (mapKey)
        {
            if (isMapOpen)
            {
                PauseMenuManager.Instance?.Resume();
            }
            else if (!IsShopOrDialogueOpen() && !isInvOpen && !isCharOpen && !isQuestOpen)
            {
                if (PauseMenuManager.Instance != null)
                {
                    if (!PauseMenuManager.Instance.isPaused)
                    {
                        PauseMenuManager.Instance.Pause();
                        PauseMenuManager.Instance.openedFromHotkey = true;
                    }
                    PauseMenuManager.Instance.OpenMap();
                }
            }
        }

        if (questKey)
        {
            if (isQuestOpen)
            {
                PauseMenuManager.Instance?.Resume();
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
                QuestUIController.Instance?.RefreshQuestUI();
            }
        }
    }

    private void UpdatePlayerInputLock()
    {
        if (Player_InputManager.Instance != null)
        {
            bool isPauseMenuOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused;
            bool isInvOpen = InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf;
            bool isCharOpen = CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf;
            bool isMapOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.mapScreen != null && PauseMenuManager.Instance.mapScreen.activeSelf;
            bool isQuestOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.questScreen != null && PauseMenuManager.Instance.questScreen.activeSelf;

            Player_InputManager.Instance.isInputLocked = IsShopOrDialogueOpen() || isInvOpen || isCharOpen || isMapOpen || isQuestOpen || isPauseMenuOpen;
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
        if (ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf) { ShopManager.Instance.BackToDialogue(); return; }
        if (UIManager.Instance != null && UIManager.Instance.isDialogueOpen) { DialogueManager.Instance?.EndDialogue(); return; }

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused)
        {
            PauseMenuManager.Instance.HandleBackButton();
            return;
        }

        PauseMenuManager.Instance?.Pause();
    }
}