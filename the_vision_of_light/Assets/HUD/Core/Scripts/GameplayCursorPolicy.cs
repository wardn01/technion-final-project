using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using VisionOfLight.Player;

/// <summary>
/// Single authority for gameplay cursor lock and Input System UI map toggling.
/// Runs after other scripts so nothing can re-show the cursor afterward.
/// </summary>
[DefaultExecutionOrder(32000)]
public class GameplayCursorPolicy : MonoBehaviour
{
    public static GameplayCursorPolicy Instance { get; private set; }

    private InputSystemUIInputModule uiInputModule;
    private InputActionMap uiActionMap;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(this);
            return;
        }

        uiInputModule = FindAnyObjectByType<InputSystemUIInputModule>();

        if (uiInputModule != null && uiInputModule.actionsAsset != null)
            uiActionMap = uiInputModule.actionsAsset.FindActionMap("UI", throwIfNotFound: false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            Apply();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "World")
            return;

        ResetMenuStateForWorldEntry();
        Apply();
    }

    /// <summary>True when menus, dialogue, shop, or death screen need a free cursor.</summary>
    public static bool RequiresFreeCursor()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused)
            return true;

        if (PauseMenuManager.Instance != null
            && PauseMenuManager.Instance.settingsMenuUI != null
            && PauseMenuManager.Instance.settingsMenuUI.activeSelf)
            return true;

        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf)
            return true;

        if (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf)
            return true;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.mapScreen != null && PauseMenuManager.Instance.mapScreen.activeSelf)
            return true;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.questScreen != null && PauseMenuManager.Instance.questScreen.activeSelf)
            return true;

        if (ShopManager.Instance != null && ShopManager.Instance.shopPanel != null && ShopManager.Instance.shopPanel.activeSelf)
            return true;

        if (UIManager.Instance != null && UIManager.Instance.isDialogueOpen)
            return true;

        PlayerHealth health = PlayerRegistry.Instance?.Health;
        if (health != null && health.isDead)
            return true;

        return false;
    }

    public void Apply()
    {
        bool freeCursor = RequiresFreeCursor();

        if (VisionOfLight.Player.PlayerInputManager.Instance != null)
            VisionOfLight.Player.PlayerInputManager.Instance.isInputLocked = freeCursor;

        SetUiInputActive(freeCursor);

        if (freeCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetUiInputActive(bool enableUi)
    {
        if (uiActionMap != null)
        {
            if (enableUi)
            {
                if (!uiActionMap.enabled)
                    uiActionMap.Enable();
            }
            else if (uiActionMap.enabled)
            {
                uiActionMap.Disable();
            }
        }

        if (uiInputModule != null && uiInputModule.enabled != enableUi)
            uiInputModule.enabled = enableUi;
    }

    public static void RequestApply()
    {
        if (Instance != null)
            Instance.Apply();
    }

    private static void ResetMenuStateForWorldEntry()
    {
        if (PauseMenuManager.Instance == null)
            return;

        if (PauseMenuManager.Instance.isPaused
            || (PauseMenuManager.Instance.pauseMainPanel != null && PauseMenuManager.Instance.pauseMainPanel.activeSelf))
        {
            PauseMenuManager.Instance.Resume();
        }

        if (UIManager.Instance != null)
            UIManager.Instance.isDialogueOpen = false;

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.isDialogueOpen = false;
    }
}
