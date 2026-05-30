using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Cinemachine;

public class AwakeningManager : MonoBehaviour
{
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

    private void Start()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.mainQuestState > 0)
        {
            if (blackScreen != null)
                blackScreen.gameObject.SetActive(false);

            if (cinematicDummy != null)
                cinematicDummy.SetActive(false);

            if (realPlayer != null)
                realPlayer.SetActive(true);

            if (sleepCamera != null)
                sleepCamera.Priority = 0;

            if (ShopManager.Instance != null)
                ShopManager.Instance.SetPlayerFreeze(false);

            if (mainCanvas != null)
                mainCanvas.SetActive(true); 

            return;
        }

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

        StartCoroutine(CinematicRoutine());
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
        }

        if (sleepCamera != null)
            sleepCamera.Priority = 0;

        yield return new WaitForSeconds(cameraBlendTime);

        if (ShopManager.Instance != null)
            ShopManager.Instance.SetPlayerFreeze(false);

        if (mainCanvas != null)
            mainCanvas.SetActive(true); 
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