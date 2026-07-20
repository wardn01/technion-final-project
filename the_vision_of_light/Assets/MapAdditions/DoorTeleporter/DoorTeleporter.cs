using UnityEngine;
using TMPro;

/// <summary>
/// Door teleport that uses the shared scene InteractPrompt UI (same as chests / challenge stone).
/// Must re-enable the parent InteractPrompt and the F-key badge — other systems may leave them off.
/// </summary>
public class DoorTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform targetLocation;

    [Header("UI Settings")]
    [Tooltip("Usually Interact_F under InteractPrompt.")]
    public GameObject promptContainer;

    [Tooltip("Optional. Parent of Interact_F (InteractPrompt). Auto-resolved from promptContainer.")]
    public GameObject promptRoot;

    [Tooltip("Optional. F key badge (Interact_F/btn). Auto-resolved when empty.")]
    public GameObject interactKeyPrompt;

    public TextMeshProUGUI promptTextUI;
    public string promptText = "Enter House";

    [Header("Quest Path")]
    [Tooltip("Hide the ground quest guide after teleporting here (e.g. house interior).")]
    public bool hideQuestPathAtDestination;

    [Tooltip("Show the ground quest guide after teleporting here (e.g. leaving the house).")]
    public bool showQuestPathAtDestination;

    private bool isPlayerNear;
    private GameObject playerObj;
    private static DoorTeleporter activeDoor;

    private void Start()
    {
        ResolveSharedInteractUi();
    }

    private void Update()
    {
        if (activeDoor != this)
            return;

        RefreshPlayerNearByDistance();

        bool isMenuOpen =
            (ShopManager.Instance != null && ShopManager.Instance.shopPanel != null &&
             ShopManager.Instance.shopPanel.activeSelf) ||
            (UIManager.Instance != null && UIManager.Instance.isDialogueOpen) ||
            (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused);

        bool shouldShow = isPlayerNear && !isMenuOpen && Time.timeScale != 0f;

        if (shouldShow)
        {
            ShowInteractPrompt();

            if (playerObj != null && Input.GetKeyDown(ShopManager.GetInteractKey()))
                TeleportPlayer();
        }
        else
        {
            HideInteractPrompt();
        }
    }

    /// <summary>Call when the player warps away without OnTriggerExit (map teleport).</summary>
    public void ClearPlayerProximity()
    {
        isPlayerNear = false;
        playerObj = null;

        if (activeDoor == this)
            activeDoor = null;

        HideInteractPrompt();
    }

    private void RefreshPlayerNearByDistance()
    {
        if (!isPlayerNear)
            return;

        Transform player = playerObj != null ? playerObj.transform : null;
        if (player == null)
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null)
                player = tagged.transform;
        }

        if (SharedInteractPromptUtility.IsPlayerBeyondRange(
                transform.position, player, SharedInteractPromptUtility.DefaultLeaveDistance))
            ClearPlayerProximity();
    }

    private void ShowInteractPrompt()
    {
        ResolveSharedInteractUi();

        // Wave / chests may leave InteractPrompt (parent) disabled — child alone will not render.
        if (promptRoot != null && !promptRoot.activeSelf)
            promptRoot.SetActive(true);

        if (promptContainer != null && !promptContainer.activeSelf)
            promptContainer.SetActive(true);

        // Challenge stone may leave the F badge disabled after a trial.
        if (interactKeyPrompt != null && !interactKeyPrompt.activeSelf)
            interactKeyPrompt.SetActive(true);

        if (promptTextUI == null)
            return;

        if (!promptTextUI.gameObject.activeSelf)
            promptTextUI.gameObject.SetActive(true);

        promptTextUI.text = string.IsNullOrEmpty(promptText) ? "Enter" : promptText;
    }

    private void HideInteractPrompt()
    {
        if (promptContainer != null && promptContainer.activeSelf)
            promptContainer.SetActive(false);
    }

    private void ResolveSharedInteractUi()
    {
        if (promptRoot == null && promptContainer != null && promptContainer.transform.parent != null)
            promptRoot = promptContainer.transform.parent.gameObject;

        if (interactKeyPrompt == null && promptContainer != null)
        {
            Transform btn = promptContainer.transform.Find("btn");
            if (btn != null)
                interactKeyPrompt = btn.gameObject;
        }

        if (promptTextUI == null && promptContainer != null)
            promptTextUI = promptContainer.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void TeleportPlayer()
    {
        if (playerObj == null || targetLocation == null)
            return;

        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = playerObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        playerObj.transform.position = targetLocation.position;

        if (cc != null) cc.enabled = true;
        if (agent != null) agent.enabled = true;

        if (hideQuestPathAtDestination)
            QuestPathSuppression.SetForcedInterior(true);
        else if (showQuestPathAtDestination)
            QuestPathSuppression.SetForcedInterior(false);

        ClearPlayerProximity();
        SharedInteractPromptUtility.ClearAllProximityPrompts();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        activeDoor = this;
        isPlayerNear = true;
        playerObj = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ClearPlayerProximity();
    }
}
