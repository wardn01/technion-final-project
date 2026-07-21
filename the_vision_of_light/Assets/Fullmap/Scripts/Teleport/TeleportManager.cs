using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the map UI interaction for selecting and executing teleportation to unlocked points.
/// Manages the loading screen visuals, including progress bar, percentage text, and animated loading status.
/// Also plays the same loading overlay when the player revives at a teleport.
/// </summary>
public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance { get; private set; }

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
    private Coroutine activeTravelCoroutine;

    /// <summary>True while a loading-screen travel (map teleport / revive / OOB) is running.</summary>
    public bool IsTraveling => activeTravelCoroutine != null;

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
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

            StartTravelCoroutine(TeleportSequence(selectedDestination));
        }
    }

    public void CancelSelection()
    {
        selectedDestination = null;
        if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(false);
        if (mapSelectionGlow != null) mapSelectionGlow.SetActive(false);
    }

    /// <summary>
    /// Same loading screen as map teleport — moves the player then invokes <paramref name="onComplete"/>.
    /// Used by death revive.
    /// </summary>
    public void TravelWithLoadingScreen(Vector3 targetPosition, Quaternion targetRotation, System.Action onComplete = null)
    {
        StartTravelCoroutine(TravelToPositionSequence(targetPosition, targetRotation, onComplete));
    }
    #endregion

    #region Teleport Execution
    private void StartTravelCoroutine(IEnumerator routine)
    {
        if (activeTravelCoroutine != null)
            StopCoroutine(activeTravelCoroutine);

        activeTravelCoroutine = StartCoroutine(routine);
    }

    private IEnumerator TeleportSequence(TeleportPoint destination)
    {
        Vector3 targetPosition = (destination.spawnLocation != null)
            ? destination.spawnLocation.position
            : destination.transform.position + new Vector3(2f, 1f, 0f);

        Quaternion targetRotation = destination.spawnLocation != null
            ? destination.spawnLocation.rotation
            : destination.transform.rotation;

        yield return TravelToPositionSequence(targetPosition, targetRotation, null);
        selectedDestination = null;
    }

    private IEnumerator TravelToPositionSequence(Vector3 targetPosition, Quaternion targetRotation, System.Action onComplete)
    {
        SharedInteractPromptUtility.ClearAllProximityPrompts();

        // Boss bar stays stuck otherwise — the boss only hides it on its own camp reset,
        // which cannot run while the game is paused during the teleport.
        if (VisionOfLight.Enemy.BossHealthBarUI.Instance != null)
            VisionOfLight.Enemy.BossHealthBarUI.Instance.HideBoss(null);

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.EndDialogue();
        else if (UIManager.Instance != null)
            UIManager.Instance.isDialogueOpen = false;

        BeginLoadingScreen();

        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.CloseAllSubScreens();
            Time.timeScale = 0f;
        }
        else if (fullMapScreen != null)
        {
            fullMapScreen.SetActive(false);
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 0f;
        }

        float elapsedTime = 0f;
        while (elapsedTime < loadingDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / loadingDuration);
            UpdateLoadingProgress(progress);
            yield return null;
        }

        if (!EnsurePlayerTransform())
        {
            FinishTeleportUi(resumeGameplay: true);
            onComplete?.Invoke();
            activeTravelCoroutine = null;
            yield break;
        }

        MovePlayerTo(targetPosition, targetRotation);

        yield return new WaitForEndOfFrame();

        if (postTeleportDelay > 0)
            yield return new WaitForSecondsRealtime(postTeleportDelay);

        FinishTeleportUi(resumeGameplay: true);
        onComplete?.Invoke();
        activeTravelCoroutine = null;
    }

    private void BeginLoadingScreen()
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);
        if (loadingFill != null) loadingFill.fillAmount = 0f;
        if (percentageText != null) percentageText.text = "0%";

        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        textAnimationCoroutine = StartCoroutine(AnimateLoadingText());
    }

    private void UpdateLoadingProgress(float progress)
    {
        if (loadingFill != null) loadingFill.fillAmount = progress;
        if (percentageText != null) percentageText.text = Mathf.RoundToInt(progress * 100) + "%";
    }

    private void MovePlayerTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        Rigidbody rb = player.GetComponent<Rigidbody>();
        UnityEngine.AI.NavMeshAgent agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (cc != null) cc.enabled = false;
        if (agent != null) agent.enabled = false;

        player.SetPositionAndRotation(targetPosition, targetRotation);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cc != null) cc.enabled = true;
        if (agent != null) agent.enabled = true;

        player.SendMessage("ResetVelocity", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("ResetFallDamage", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("CancelAttack", SendMessageOptions.DontRequireReceiver);
    }

    private bool EnsurePlayerTransform()
    {
        if (player != null)
            return true;

        player = SharedInteractPromptUtility.GetPlayerTransform();
        if (player != null)
            return true;

        Debug.LogError("[TeleportManager] Player reference is missing — teleport aborted.");
        return false;
    }

    private void FinishTeleportUi(bool resumeGameplay)
    {
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            textAnimationCoroutine = null;
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        if (!resumeGameplay)
            return;

        if (PauseMenuManager.Instance != null)
            PauseMenuManager.Instance.Resume();
        else
            Time.timeScale = 1f;
    }

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
