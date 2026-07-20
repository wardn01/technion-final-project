using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the map UI interaction for selecting and executing teleportation to unlocked points.
/// Manages the loading screen visuals, including progress bar, percentage text, and animated loading status.
/// </summary>
public class TeleportManager : MonoBehaviour
{
    #region Settings & References
    [Header("Teleportation Settings")]
    [Tooltip("Time in seconds to simulate the loading process.")]
    public float loadingDuration = 1.5f;
    
    [Tooltip("Extra delay after repositioning the player before hiding the loading screen. Helps smooth out visual hitches.")]
    public float postTeleportDelay = 0.2f;

    [Header("Player Reference")]
    public Transform player;

    [Header("UI Elements")]
    public GameObject fullMapScreen;
    public GameObject loadingScreen;
    public Image loadingFill;
    public TMP_Text percentageText;
    public TMP_Text loadingText;

    [Header("Confirmation UI")]
    public GameObject teleportConfirmPanel;
    public GameObject mapSelectionGlow;
    public Vector2 selectionOffset = new Vector2(0f, -30f);
    #endregion

    private TeleportPoint selectedDestination;
    private Coroutine textAnimationCoroutine;

    #region Unity Lifecycle
    private void Start()
    {
        // Initialize UI states to ensure a clean start
        if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(false);
        if (mapSelectionGlow != null) mapSelectionGlow.SetActive(false);
        if (loadingScreen != null) loadingScreen.SetActive(false);
    }
    #endregion

    #region UI Interactions
    public void SelectTeleportPoint(TeleportPoint destination)
    {
        if (destination.isUnlocked)
        {
            selectedDestination = destination;
            if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(true);

            if (mapSelectionGlow != null)
            {
                mapSelectionGlow.SetActive(true);
                GameObject clickedButton = EventSystem.current.currentSelectedGameObject;

                if (clickedButton != null)
                {
                    mapSelectionGlow.transform.SetParent(clickedButton.transform.parent, false);
                    mapSelectionGlow.transform.position = clickedButton.transform.position;
                    mapSelectionGlow.transform.SetAsFirstSibling();

                    RectTransform glowRect = mapSelectionGlow.GetComponent<RectTransform>();
                    if (glowRect != null) glowRect.anchoredPosition += selectionOffset;
                }
            }
        }
    }

    public void ConfirmTeleport()
    {
        if (selectedDestination != null)
        {
            if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(false);
            if (mapSelectionGlow != null) mapSelectionGlow.SetActive(false);

            StartCoroutine(TeleportSequence(selectedDestination));
        }
    }

    public void CancelSelection()
    {
        selectedDestination = null;
        if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(false);
        if (mapSelectionGlow != null) mapSelectionGlow.SetActive(false);
    }
    #endregion

    #region Teleport Execution
    /// <summary>
    /// Executes the teleportation, animates the loading screen, and safely relocates the player.
    /// Includes a post-teleport buffer to ensure visual smoothness.
    /// </summary>
    private IEnumerator TeleportSequence(TeleportPoint destination)
    {
        // 1. Activate loading UI — clear world interact prompts (teleport skips OnTriggerExit).
        SharedInteractPromptUtility.ClearAllProximityPrompts();

        if (loadingScreen != null) loadingScreen.SetActive(true);
        if (loadingFill != null) loadingFill.fillAmount = 0f;
        if (percentageText != null) percentageText.text = "0%";
        textAnimationCoroutine = StartCoroutine(AnimateLoadingText());

        if (PauseMenuManager.Instance != null) PauseMenuManager.Instance.Resume();
        else if (fullMapScreen != null) fullMapScreen.SetActive(false);

        // 2. Animate loading progress
        float elapsedTime = 0f;
        while (elapsedTime < loadingDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / loadingDuration);
            
            if (loadingFill != null) loadingFill.fillAmount = progress;
            if (percentageText != null) percentageText.text = Mathf.RoundToInt(progress * 100) + "%";
            
            yield return null;
        }

        // 3. Move player
        Vector3 targetPosition = (destination.spawnLocation != null) 
            ? destination.spawnLocation.position 
            : destination.transform.position + new Vector3(2f, 1f, 0f);

        CharacterController cc = player.GetComponent<CharacterController>();
        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (cc != null)
        {
            cc.enabled = false;
            player.position = targetPosition;
            cc.enabled = true;
        }
        else
        {
            player.position = targetPosition;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.SendMessage("ResetVelocity", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("ResetFallDamage", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("CancelAttack", SendMessageOptions.DontRequireReceiver);

        // 4. Wait for physics to stabilize
        yield return new WaitForEndOfFrame(); 

        // 5. Post-Teleport Delay (The "Magic Buffer" for smoothness)
        if (postTeleportDelay > 0)
        {
            yield return new WaitForSecondsRealtime(postTeleportDelay);
        }

        // 6. Finalize
        if (UIManager.Instance != null) UIManager.Instance.isDialogueOpen = false;
        if (textAnimationCoroutine != null) StopCoroutine(textAnimationCoroutine);
        if (loadingScreen != null) loadingScreen.SetActive(false);

        selectedDestination = null;
    }

    /// <summary>
    /// Animates the "Loading..." text with dots to indicate activity.
    /// </summary>
    private IEnumerator AnimateLoadingText()
    {
        int dotCount = 0;
        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            if (loadingText != null) loadingText.text = "Loading" + new string('.', dotCount);
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
    #endregion
}