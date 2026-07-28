using UnityEngine;
using VisionOfLight.Player;

/// <summary>
/// Global HUD input router: menu hotkeys (inventory, character, map, quests),
/// Escape handling, and player input lock while UI is open.
/// Cursor lock is enforced by <see cref="GameplayCursorPolicy"/>.
/// Lives on the UIManager object in the scene.
/// </summary>
public class UI_InputManager : MonoBehaviour
{
    public static UI_InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (GetComponent<GameplayCursorPolicy>() == null)
            gameObject.AddComponent<GameplayCursorPolicy>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        // No menu input while a teleport loading screen covers the view — Escape
        // here would resume gameplay (timeScale = 1) underneath the loading screen.
        if (TeleportManager.Instance != null && TeleportManager.Instance.IsTraveling)
            return;

        // While the player is rebinding a key, KeybindManager.OnGUI owns the input —
        // the pressed key must not also fire menu hotkeys or close the settings screen.
        if (KeybindManager.Instance != null && !string.IsNullOrEmpty(KeybindManager.Instance.GetActionToRebind()))
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
            return;
        }

        HandleHotkeys();
    }

    /// <summary>
    /// Opens or closes pause-menu sub-screens via KeybindManager (Tab/I, C, M, J).
    /// Pressing the same hotkey again resumes the game.
    /// </summary>
    private void HandleHotkeys()
    {
        if (IsPlayerDead())
            return;

        // Settings screen has its own back-button flow — I/C/M/J must not open
        // other screens on top of it (they never deactivate settingsMenuUI).
        if (PauseMenuManager.Instance != null
            && PauseMenuManager.Instance.settingsMenuUI != null
            && PauseMenuManager.Instance.settingsMenuUI.activeSelf)
            return;

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

    private bool IsShopOrDialogueOpen()
    {
        bool isShop = ShopManager.Instance != null
                      && ShopManager.Instance.shopPanel != null
                      && ShopManager.Instance.shopPanel.activeSelf;
        bool isDialogue = UIManager.Instance != null && UIManager.Instance.isDialogueOpen;
        return isShop || isDialogue;
    }

    private static bool IsPlayerDead()
    {
        if (PlayerRegistry.Instance != null && PlayerRegistry.Instance.Health != null)
            return PlayerRegistry.Instance.Health.isDead;

        GameObject player = SharedInteractPromptUtility.GetPlayerGameObject();
        if (player == null)
            return false;

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        return health != null && health.isDead;
    }

    /// <summary>
    /// Escape priority: close shop → end dialogue → pause back → open pause menu.
    /// </summary>
    private void HandleEscapeKey()
    {
        if (IsPlayerDead())
            return;

        if (ShopManager.Instance != null
            && ShopManager.Instance.shopPanel != null
            && ShopManager.Instance.shopPanel.activeSelf)
        {
            ShopManager.Instance.BackToDialogue();
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.isDialogueOpen)
        {
            DialogueManager.Instance?.EndDialogue();
            return;
        }

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused)
        {
            PauseMenuManager.Instance.HandleBackButton();
            return;
        }

        PauseMenuManager.Instance?.Pause();
    }
}
