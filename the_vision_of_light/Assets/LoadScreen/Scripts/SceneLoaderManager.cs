using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the asynchronous loading of scenes and displays a loading screen with a smooth progress bar.
/// </summary>
public class SceneLoaderManager : MonoBehaviour
{
    #region Singleton
    public static SceneLoaderManager Instance { get; private set; }
    #endregion

    #region UI References
    [Header("Loading UI References")]
    public GameObject loadingScreenPanel;
    public Image fillImage; 
    public TMP_Text percentageText;
    public TMP_Text loadingText;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(false);
    }
    #endregion

    #region Scene Loading
    /// <summary>
    /// Activates the loading screen UI and begins loading the target scene asynchronously.
    /// </summary>
    public void LoadWorldScene(string sceneName)
    {
        loadingScreenPanel.SetActive(true);
        
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (percentageText != null) percentageText.text = "0%";

        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
        StartCoroutine(AnimateLoadingTextCoroutine());
    }
    #endregion

    #region Coroutines
    /// <summary>
    /// Handles the background loading operation and smoothly updates the visual progress bar.
    /// </summary>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        yield return new WaitForSeconds(0.1f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float visualProgress = 0f;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, Time.deltaTime * 1.5f);

            if (fillImage != null) fillImage.fillAmount = visualProgress;
            if (percentageText != null) percentageText.text = Mathf.RoundToInt(visualProgress * 100f) + "%";

            if (operation.progress >= 0.9f && visualProgress >= 1f)
            {
                yield return new WaitForSeconds(0.2f); 
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Animates the loading text with appending dots.
    /// </summary>
    private IEnumerator AnimateLoadingTextCoroutine()
    {
        int dotCount = 0;
        string baseText = "Loading";

        while (true)
        {
            dotCount++;
            if (dotCount > 3) dotCount = 0;

            string dots = new string('.', dotCount);
            
            if (loadingText != null) 
                loadingText.text = baseText + dots;

            yield return new WaitForSeconds(0.4f);
        }
    }
    #endregion
}