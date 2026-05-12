using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [HideInInspector] public bool isDialogueOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
}