using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Center-screen banner when a story quest is completed — same flow as
/// <see cref="VisionOfLight.Enemy.ChallengeTimerUI.ShowResult"/>.
/// Auto-created at runtime on Canvas when <see cref="ResultQuestPanel"/> exists in the scene.
/// </summary>
public class QuestResultUI : MonoBehaviour
{
    private static QuestResultUI instance;

    public static QuestResultUI Instance => instance;

    [Header("Panel")]
    [Tooltip("Auto-found: ResultQuestPanel under Canvas.")]
    public GameObject resultPanel;

    public TextMeshProUGUI resultText;

    [Tooltip("How long the message stays visible.")]
    public float resultDisplaySeconds = 2.5f;

    public Color successColor = new Color(1f, 0.92f, 0.55f);

    [Header("Audio")]
    public AudioClip successClip;
    [Range(0f, 1f)] public float successVolume = 1f;

    private Coroutine hideRoutine;
    private AudioSource audioSource;
    private bool isInitialized;

    /// <summary>
    /// Returns an existing component or creates one on the main Canvas.
    /// </summary>
    public static QuestResultUI EnsureExists()
    {
        if (instance != null)
            return instance;

        foreach (QuestResultUI ui in Resources.FindObjectsOfTypeAll<QuestResultUI>())
        {
            if (ui == null || !ui.gameObject.scene.IsValid())
                continue;

            instance = ui;
            return instance;
        }

        GameObject panel = FindSceneObjectByName("ResultQuestPanel");
        if (panel == null)
            return null;

        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return null;

        QuestResultUI onCanvas = canvas.GetComponent<QuestResultUI>();
        if (onCanvas == null)
            onCanvas = canvas.gameObject.AddComponent<QuestResultUI>();

        instance = onCanvas;
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void ShowQuestComplete(QuestData quest)
    {
        ShowResult(BuildMessage(quest));
    }

    public void ShowResult(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        EnsureInitialized();
        StopHideRoutine();

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = message;
            resultText.color = successColor;
            resultText.gameObject.SetActive(true);
        }

        PlaySuccessSound();
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    public void HideImmediate()
    {
        StopHideRoutine();

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        ResolveReferences();
        DisablePanelRaycasts();
        HideImmediate();
    }

    private static string BuildMessage(QuestData quest)
    {
        if (quest == null)
            return "Quest Complete";

        return string.IsNullOrWhiteSpace(quest.completionMessage)
            ? "Quest Complete"
            : quest.completionMessage.Trim();
    }

    private void ResolveReferences()
    {
        if (resultPanel == null)
            resultPanel = FindSceneObjectByName("ResultQuestPanel");

        if (resultText != null || resultPanel == null)
            return;

        foreach (TextMeshProUGUI label in resultPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.gameObject.name == "ResultText")
            {
                resultText = label;
                return;
            }
        }

        resultText = resultPanel.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name != objectName || !go.scene.IsValid())
                continue;

            if ((go.hideFlags & HideFlags.HideInHierarchy) != 0)
                continue;

            return go;
        }

        return null;
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(resultDisplaySeconds);
        HideImmediate();
        hideRoutine = null;
    }

    private void StopHideRoutine()
    {
        if (hideRoutine == null)
            return;

        StopCoroutine(hideRoutine);
        hideRoutine = null;
    }

    private void DisablePanelRaycasts()
    {
        if (resultPanel == null)
            return;

        foreach (Graphic graphic in resultPanel.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    private void PlaySuccessSound()
    {
        if (successClip == null || successVolume <= 0f)
            return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        audioSource.PlayOneShot(successClip, successVolume);
    }
}
