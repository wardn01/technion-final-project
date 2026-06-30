using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Cinemachine;
using VisionOfLight.Player;

[DefaultExecutionOrder(50)]
public class AwakeningManager : MonoBehaviour
{
    public static bool HasCompletedAwakening { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAwakeningState()
    {
        HasCompletedAwakening = false;
    }

    public static void ResetSessionState()
    {
        HasCompletedAwakening = false;
    }
    [Header("UI Elements")]
    public Image blackScreen;
    public GameObject mainCanvas; 

    [Header("Cinematic Elements")]
    public Animator cinematicAnimator;
    public GameObject cinematicDummy;
    public GameObject realPlayer;
    public CinemachineCamera sleepCamera;

    [Header("Timings")]
    public float sitUpDuration = 3.0f;
    public float standUpDuration = 2.0f;
    public float cameraBlendTime = 1.5f;

    [Header("Markers")]
    public Transform sitPoint;
    public Transform standPoint;

    public float playerSpawnYOffset = 0.1f;

    [Header("Intro")]
    [Tooltip("When assigned and playing the intro, awakening waits for IntroCutsceneManager.")]
    public IntroCutsceneManager introCutsceneManager;

    private bool awakeningStarted;

    private void Awake()
    {
        ResolveIntroReference();
    }

    private void Start()
    {
        if (ShouldSkipAwakening())
        {
            SkipToGameplay();
            return;
        }

        IntroCutsceneManager intro = ResolveIntroReference();

        if (intro != null && intro.HandlesAwakeningStart)
            return;

        StartAwakening();
    }

    private IntroCutsceneManager ResolveIntroReference()
    {
        if (introCutsceneManager == null)
        {
            introCutsceneManager = GetComponent<IntroCutsceneManager>()
                ?? GetComponentInParent<IntroCutsceneManager>()
                ?? FindAnyObjectByType<IntroCutsceneManager>();
        }

        return introCutsceneManager;
    }

    /// <summary>Called by <see cref="IntroCutsceneManager"/> after the placeholder intro finishes.</summary>
    public void StartAwakening()
    {
        if (awakeningStarted || ShouldSkipAwakening())
            return;

        awakeningStarted = true;
        StartCoroutine(AwakeningRoutine());
    }

    private bool ShouldSkipAwakening()
    {
        if (QuestManager.Instance == null)
            return false;

        return !QuestManager.Instance.IsAtFreshStoryStart();
    }

    private void SkipToGameplay()
    {
        bool restoredSavedPosition = PauseMenuManager.Instance != null
            && PauseMenuManager.Instance.TryRestorePlayerTransformFromSave();

        if (!restoredSavedPosition && realPlayer != null && standPoint != null)
        {
            Vector3 finalPos = standPoint.position;
            finalPos.y += playerSpawnYOffset;

            realPlayer.transform.position = finalPos;
            realPlayer.transform.rotation = standPoint.rotation;

            PlayerMovement movement = realPlayer.GetComponent<PlayerMovement>();
            if (movement != null)
                movement.ResetFallDamage();
        }

        if (blackScreen != null)
            blackScreen.gameObject.SetActive(false);

        if (cinematicDummy != null)
            cinematicDummy.SetActive(false);

        if (realPlayer != null)
            realPlayer.SetActive(true);

        EnsurePlayerHealthAfterSpawn();

        if (sleepCamera != null)
            sleepCamera.Priority = 0;

        if (ShopManager.Instance != null)
            ShopManager.Instance.SetPlayerFreeze(false);

        if (mainCanvas != null)
            mainCanvas.SetActive(true);

        HasCompletedAwakening = true;
        QuestPathSuppression.SetForcedInterior(false);

        if (WorldSaveManager.Instance == null || !WorldSaveManager.Instance.HasCompletedChapter01Awakening)
            MarkAwakeningComplete();
    }

    private void EnsurePlayerHealthAfterSpawn()
    {
        if (realPlayer == null)
            return;

        PlayerHealth health = realPlayer.GetComponent<PlayerHealth>();
        health?.EnsureFullHealthAtSpawn();
    }

    private void MarkAwakeningComplete()
    {
        HasCompletedAwakening = true;
        WorldSaveManager.Instance?.MarkChapter01AwakeningComplete();
    }

    private IEnumerator AwakeningRoutine()
    {
        if (IntroCutsceneManager.HasFinishedIntro)
            introCutsceneManager?.HideIntroPresentation();

        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            SetAlpha(1f);
        }

        if (cinematicDummy != null)
            cinematicDummy.SetActive(true);

        if (realPlayer != null)
            realPlayer.SetActive(false);

        if (sleepCamera != null)
            sleepCamera.Priority = 20;

        if (ShopManager.Instance != null)
            ShopManager.Instance.SetPlayerFreeze(true);

        if (mainCanvas != null)
            mainCanvas.SetActive(false);

        yield return CinematicRoutine();
    }

    private IEnumerator CinematicRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        yield return StartCoroutine(FadeAlpha(1f, 0.6f, 1.2f));
        yield return StartCoroutine(FadeAlpha(0.6f, 0.95f, 0.3f));

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(FadeAlpha(0.95f, 0.3f, 1.0f));
        yield return StartCoroutine(FadeAlpha(0.3f, 0.7f, 0.4f));

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(FadeAlpha(0.7f, 0f, 2.0f));

        if (cinematicAnimator != null)
            cinematicAnimator.SetTrigger("WakeUp");

        yield return new WaitForSeconds(sitUpDuration);

        if (blackScreen != null)
            blackScreen.gameObject.SetActive(true);

        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.2f));

        if (cinematicDummy != null && sitPoint != null)
        {
            cinematicDummy.transform.position = sitPoint.position;
            cinematicDummy.transform.rotation = sitPoint.rotation;
        }

        if (cinematicAnimator != null)
            cinematicAnimator.SetTrigger("StandUp");

        yield return StartCoroutine(FadeAlpha(1f, 0f, 0.3f));

        if (blackScreen != null)
            blackScreen.gameObject.SetActive(false);

        yield return new WaitForSeconds(standUpDuration);

        if (realPlayer != null && standPoint != null)
        {
            Vector3 finalPos = standPoint.position;
            finalPos.y += playerSpawnYOffset;

            realPlayer.transform.position = finalPos;
            realPlayer.transform.rotation = standPoint.rotation;

            realPlayer.SetActive(true);
            cinematicDummy.SetActive(false);

            PlayerMovement movement = realPlayer.GetComponent<PlayerMovement>();
            if (movement != null)
                movement.ResetFallDamage();
        }

        EnsurePlayerHealthAfterSpawn();
        QuestPathSuppression.SetForcedInterior(true);

        if (sleepCamera != null)
            sleepCamera.Priority = 0;

        yield return new WaitForSeconds(cameraBlendTime);

        if (ShopManager.Instance != null)
            ShopManager.Instance.SetPlayerFreeze(false);

        if (mainCanvas != null)
            mainCanvas.SetActive(true);

        PauseMenuManager.Instance?.SaveGameSilently();
        
        MarkAwakeningComplete();
    }

    private IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = blackScreen.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            blackScreen.color = c;

            yield return null;
        }

        c.a = endAlpha;
        blackScreen.color = c;
    }

    private void SetAlpha(float alpha)
    {
        Color c = blackScreen.color;
        c.a = alpha;
        blackScreen.color = c;
    }
}