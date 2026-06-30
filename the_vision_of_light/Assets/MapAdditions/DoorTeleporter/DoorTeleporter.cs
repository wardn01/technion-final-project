using UnityEngine;
using TMPro;

public class DoorTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform targetLocation;

    [Header("UI Settings")]
    public GameObject promptContainer; 
    public TextMeshProUGUI promptTextUI;
    public string promptText = "Enter House";

    [Header("Quest Path")]
    [Tooltip("Hide the ground quest guide after teleporting here (e.g. house interior).")]
    public bool hideQuestPathAtDestination;

    [Tooltip("Show the ground quest guide after teleporting here (e.g. leaving the house).")]
    public bool showQuestPathAtDestination;

    private bool isPlayerNear = false;
    private GameObject playerObj;
    
    private static DoorTeleporter activeDoor = null;

    private void Start()
    {
    }

    private void Update()
    {
        bool isMenuOpen = (ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf) || 
                          (UIManager.Instance != null && UIManager.Instance.isDialogueOpen);

        if (activeDoor == this)
        {
            bool shouldShow = isPlayerNear && !isMenuOpen && Time.timeScale != 0f;

            if (shouldShow)
            {
                if (promptContainer != null && !promptContainer.activeSelf)
                {
                    promptContainer.SetActive(true);
                }

                if (promptTextUI != null)
                {
                    if (!promptTextUI.gameObject.activeSelf) 
                    {
                        promptTextUI.gameObject.SetActive(true);
                    }
                    promptTextUI.text = promptText;
                }

                if (playerObj != null && Input.GetKeyDown(KeyCode.F))
                {
                    TeleportPlayer();
                }
            }
            else
            {
                if (promptContainer != null && promptContainer.activeSelf)
                {
                    promptContainer.SetActive(false);
                }
            }
        }
    }

    private void TeleportPlayer()
    {
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = playerObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        
        playerObj.transform.position = targetLocation.position;

        if (cc != null) cc.enabled = true;
        if (agent != null) agent.enabled = true;

        if (hideQuestPathAtDestination)
            QuestPathSuppression.SetForcedInterior(true);
        else if (showQuestPathAtDestination)
            QuestPathSuppression.SetForcedInterior(false);
        
        if (promptContainer != null) promptContainer.SetActive(false);
        
        isPlayerNear = false;
        playerObj = null;
        activeDoor = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            activeDoor = this;
            isPlayerNear = true;
            playerObj = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            
            if (activeDoor == this)
            {
                if (promptContainer != null) promptContainer.SetActive(false);
                activeDoor = null;
            }
            playerObj = null;
        }
    }
}