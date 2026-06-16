using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Global HUD for wave challenges: countdown timer and center result message.
/// Place once on the main Canvas; stones call it through <see cref="Instance"/>.
/// </summary>
public class ChallengeTimerUI : MonoBehaviour
{
    public static ChallengeTimerUI Instance { get; private set; }

    #region Inspector
    [Header("Timer (top of screen)")]
    [Tooltip("Root panel for the countdown (hidden when no challenge is running).")]
    public GameObject timerPanel;

    [Tooltip("TMP label that shows MM:SS.")]
    public TextMeshProUGUI timerText;

    [Tooltip("Text color when less than this many seconds remain.")]
    public float urgentThresholdSeconds = 10f;

    public Color normalTimerColor = Color.white;
    public Color urgentTimerColor = new Color(1f, 0.35f, 0.35f);

    [Header("Result (center of screen)")]
    [Tooltip("Root panel for success / fail messages.")]
    public GameObject resultPanel;

    public TextMeshProUGUI resultText;

    [Tooltip("How long the center message stays visible.")]
    public float resultDisplaySeconds = 2.5f;

    public Color successColor = new Color(1f, 0.92f, 0.55f);
    public Color failColor = new Color(1f, 0.45f, 0.45f);
    #endregion

    #region Runtime State
    private float remainingSeconds;
    private bool isRunning;
    private Coroutine resultRoutine;
    private Action onExpired;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HideAllImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isRunning)
            return;

        remainingSeconds -= Time.deltaTime;
        if (remainingSeconds <= 0f)
        {
            remainingSeconds = 0f;
            isRunning = false;
            UpdateTimerDisplay();
            HideTimer();

            Action callback = onExpired;
            onExpired = null;
            callback?.Invoke();
            return;
        }

        UpdateTimerDisplay();
    }
    #endregion

    #region Public API
    /// <summary>Starts the countdown and invokes <paramref name="expiredCallback"/> when it hits zero.</summary>
    public void Begin(float durationSeconds, Action expiredCallback)
    {
        if (durationSeconds <= 0f)
            return;

        StopResultRoutine();
        HideResultImmediate();

        remainingSeconds = durationSeconds;
        onExpired = expiredCallback;
        isRunning = true;

        if (timerPanel != null)
            timerPanel.SetActive(true);

        UpdateTimerDisplay();
    }

    /// <summary>Stops the countdown without showing a result.</summary>
    public void StopTimer()
    {
        isRunning = false;
        onExpired = null;
        remainingSeconds = 0f;
        HideTimer();
    }

    /// <summary>Shows a short center-screen message (e.g. Challenge Complete).</summary>
    public void ShowResult(string message, bool success)
    {
        if (resultText == null)
            return;

        StopResultRoutine();
        HideTimer();

        resultText.text = message;
        resultText.color = success ? successColor : failColor;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        resultText.gameObject.SetActive(true);
        resultRoutine = StartCoroutine(HideResultAfterDelay());
    }

    /// <summary>Hides timer and result immediately.</summary>
    public void HideAll()
    {
        StopTimer();
        StopResultRoutine();
        HideResultImmediate();
    }
    #endregion

    #region Display Helpers
    private void UpdateTimerDisplay()
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(remainingSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
        timerText.color = remainingSeconds <= urgentThresholdSeconds ? urgentTimerColor : normalTimerColor;
    }

    private void HideTimer()
    {
        if (timerPanel != null)
            timerPanel.SetActive(false);
    }

    private IEnumerator HideResultAfterDelay()
    {
        yield return new WaitForSeconds(resultDisplaySeconds);
        HideResultImmediate();
        resultRoutine = null;
    }

    private void HideResultImmediate()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (resultText != null)
            resultText.gameObject.SetActive(false);
    }

    private void HideAllImmediate()
    {
        isRunning = false;
        onExpired = null;
        HideTimer();
        HideResultImmediate();
    }

    private void StopResultRoutine()
    {
        if (resultRoutine == null)
            return;

        StopCoroutine(resultRoutine);
        resultRoutine = null;
    }
    #endregion
}
