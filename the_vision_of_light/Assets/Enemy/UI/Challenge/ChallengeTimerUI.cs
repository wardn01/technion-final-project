using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Global HUD for wave challenges: countdown timer and center result message.
    /// Place once on the main Canvas; stones call it through <see cref="Instance"/>.
    /// </summary>
    public class ChallengeTimerUI : MonoBehaviour
    {
        public static ChallengeTimerUI Instance { get; private set; }

        #region Inspector
        [Header("Root")]
        [Tooltip("Optional parent object (e.g. WaveChallenge). Hidden while no timer or result is shown.")]
        public GameObject rootContainer;

        [Header("Timer (top of screen)")]
        [Tooltip("Root panel for the countdown (hidden when no challenge is running).")]
        public GameObject timerPanel;

        [Tooltip("TMP label that shows MM:SS.")]
        public TextMeshProUGUI timerText;

        [Tooltip("Text color when less than this many seconds remain.")]
        public float urgentThresholdSeconds = 10f;

        public Color normalTimerColor = Color.white;
        public Color urgentTimerColor = new Color(1f, 0.35f, 0.35f);

        [Tooltip("Stopwatch icon next to the timer text.")]
        public Image timerIcon;

        public Color normalIconColor = Color.white;
        public Color urgentIconColor = new Color(1f, 0.35f, 0.35f);

        [Tooltip("Peak scale multiplier while pulsing in the last seconds.")]
        public float urgentPulseScale = 1.2f;

        [Tooltip("Full pulse cycles per second during the urgent phase.")]
        public float urgentPulseSpeed = 1f;

        [Header("Result (center of screen)")]
        [Tooltip("Root panel for success / fail messages.")]
        public GameObject resultPanel;

        public TextMeshProUGUI resultText;

        [Tooltip("How long the center message stays visible.")]
        public float resultDisplaySeconds = 2.5f;

        public Color successColor = new Color(1f, 0.92f, 0.55f);
        public Color failColor = new Color(1f, 0.45f, 0.45f);

        [Header("Audio")]
        [Tooltip("Plays once per second while the displayed countdown is within the urgent threshold (Genshin-style).")]
        public AudioClip urgentTickClip;

        [Range(0f, 1f)] public float urgentTickVolume = 1f;

        public AudioClip successClip;
        [Range(0f, 1f)] public float successVolume = 1f;

        public AudioClip failClip;
        [Range(0f, 1f)] public float failVolume = 1f;
        #endregion

        #region Runtime State
        private float remainingSeconds;
        private bool isRunning;
        private int lastUrgentTickSecond = -1;
        private Coroutine resultRoutine;
        private Action onExpired;
        private AudioSource audioSource;
        private RectTransform timerIconRect;
        private RectTransform timerTextRect;
        private Vector3 iconBaseScale = Vector3.one;
        private Vector3 textBaseScale = Vector3.one;
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
            EnsureAudioSource();
            CacheTimerTransforms();
            ResolveRootContainer();
            DisablePanelRaycasts();
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
            ApplyUrgentTimerVisuals();
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
            lastUrgentTickSecond = -1;

            if (timerPanel != null)
                timerPanel.SetActive(true);

            ResetTimerVisuals();
            UpdateTimerDisplay();
            RefreshRootVisibility();
        }

        /// <summary>Stops the countdown without showing a result.</summary>
        public void StopTimer()
        {
            isRunning = false;
            onExpired = null;
            remainingSeconds = 0f;
            lastUrgentTickSecond = -1;
            ResetTimerVisuals();
            HideTimer();
        }

        /// <summary>Shows a short center-screen message (e.g. Challenge Complete).</summary>
        public void ShowResult(string message, bool success)
        {
            StopResultRoutine();
            HideTimer();

            if (resultText != null)
            {
                resultText.text = message;
                resultText.color = success ? successColor : failColor;
                resultText.gameObject.SetActive(true);
            }

            if (resultPanel != null)
                resultPanel.SetActive(true);

            PlayResultSound(success);
            resultRoutine = StartCoroutine(HideResultAfterDelay());
            RefreshRootVisibility();
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

            TryPlayUrgentTick(totalSeconds);
        }

        private void ApplyUrgentTimerVisuals()
        {
            bool isUrgent = isRunning
                            && remainingSeconds <= urgentThresholdSeconds
                            && remainingSeconds > 0f;

            if (!isUrgent)
            {
                if (timerIcon != null)
                    timerIcon.color = normalIconColor;

                SetRectScale(timerIconRect, iconBaseScale);
                SetRectScale(timerTextRect, textBaseScale);
                return;
            }

            if (timerIcon != null)
                timerIcon.color = urgentIconColor;

            Vector3 pulseScale = iconBaseScale * GetUrgentPulseMultiplier();
            SetRectScale(timerIconRect, pulseScale);

            Vector3 textPulseScale = textBaseScale * GetUrgentPulseMultiplier();
            SetRectScale(timerTextRect, textPulseScale);
        }

        private float GetUrgentPulseMultiplier()
        {
            float pulse = (Mathf.Sin(Time.time * urgentPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            return Mathf.Lerp(1f, urgentPulseScale, pulse);
        }

        private static void SetRectScale(RectTransform rect, Vector3 scale)
        {
            if (rect != null)
                rect.localScale = scale;
        }

        private void CacheTimerTransforms()
        {
            if (timerIcon != null)
            {
                timerIconRect = timerIcon.rectTransform;
                iconBaseScale = timerIconRect.localScale;
            }

            if (timerText != null)
            {
                timerTextRect = timerText.rectTransform;
                textBaseScale = timerTextRect.localScale;
            }
        }

        private void ResetTimerVisuals()
        {
            if (timerIcon != null)
                timerIcon.color = normalIconColor;

            SetRectScale(timerIconRect, iconBaseScale);
            SetRectScale(timerTextRect, textBaseScale);
        }

        private void TryPlayUrgentTick(int displayedSeconds)
        {
            if (!isRunning || urgentTickClip == null)
                return;

            if (displayedSeconds > urgentThresholdSeconds || displayedSeconds <= 0)
                return;

            if (displayedSeconds == lastUrgentTickSecond)
                return;

            lastUrgentTickSecond = displayedSeconds;
            PlayOneShot(urgentTickClip, urgentTickVolume);
        }

        private void PlayResultSound(bool success)
        {
            AudioClip clip = success ? successClip : failClip;
            float volume = success ? successVolume : failVolume;
            PlayOneShot(clip, volume);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || volume <= 0f)
                return;

            EnsureAudioSource();
            if (audioSource != null)
                audioSource.PlayOneShot(clip, volume);
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
                return;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void HideTimer()
        {
            ResetTimerVisuals();

            if (timerPanel != null)
                timerPanel.SetActive(false);

            RefreshRootVisibility();
        }

        private IEnumerator HideResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDisplaySeconds);
            HideResultImmediate();
            resultRoutine = null;
            RefreshRootVisibility();
        }

        private void HideResultImmediate()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);

            if (resultText != null)
                resultText.gameObject.SetActive(false);

            RefreshRootVisibility();
        }

        private void HideAllImmediate()
        {
            isRunning = false;
            onExpired = null;
            lastUrgentTickSecond = -1;
            ResetTimerVisuals();
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

        private void ResolveRootContainer()
        {
            if (rootContainer != null || timerPanel == null)
                return;

            Transform parent = timerPanel.transform.parent;
            if (parent != null)
                rootContainer = parent.gameObject;
        }

        /// <summary>Challenge HUD is display-only — it must never steal gameplay mouse clicks.</summary>
        private void DisablePanelRaycasts()
        {
            SetPanelBlocksRaycasts(timerPanel, false);
            SetPanelBlocksRaycasts(resultPanel, false);
        }

        private static void SetPanelBlocksRaycasts(GameObject panel, bool block)
        {
            if (panel == null)
                return;

            foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = block;
        }

        private void RefreshRootVisibility()
        {
            if (rootContainer == null)
                return;

            bool shouldShow = (timerPanel != null && timerPanel.activeSelf)
                              || (resultPanel != null && resultPanel.activeSelf);

            if (rootContainer.activeSelf != shouldShow)
                rootContainer.SetActive(shouldShow);
        }
        #endregion
    }
}
