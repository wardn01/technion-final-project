using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI warningText;
    public float displayTime = 1.5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (warningText != null) warningText.gameObject.SetActive(false);
    }

    public void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
            
            StopAllCoroutines();
            StartCoroutine(HideWarningAfterDelay());
        }
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayTime);
        
        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }
}