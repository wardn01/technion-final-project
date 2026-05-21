using UnityEngine;
using TMPro;

public class DoorTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform targetLocation;

    [Header("UI Settings")]
    public TextMeshProUGUI promptTextUI;
    public string promptText = "Enter House [F]";

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
            if (promptTextUI != null)
            {
                bool shouldShow = isPlayerNear && !isMenuOpen && Time.timeScale != 0f;
                
                if (promptTextUI.gameObject.activeSelf != shouldShow)
                    promptTextUI.gameObject.SetActive(shouldShow);

                if (shouldShow)
                    promptTextUI.text = promptText;
            }

            if (isPlayerNear && playerObj != null && !isMenuOpen)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    TeleportPlayer();
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
        
        if (promptTextUI != null) promptTextUI.gameObject.SetActive(false);
        
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
                if (promptTextUI != null) promptTextUI.gameObject.SetActive(false);
                activeDoor = null;
            }
            playerObj = null;
        }
    }
}