using UnityEngine;
using UnityEngine.UI;

public class ShopkeeperNPC : MonoBehaviour
{
    [Header("Overhead UI (World Space)")]
    public GameObject overheadUI; 
    public GameObject npcNameText; 
    public float iconVisibleDistance = 15f; 

    [Header("Dialogue UI (Genshin Style)")]
    public GameObject dialoguePanel; 
    public Button shopButton; 
    public Button exitButton; 

    [Header("Old UI Prompt")]
    public GameObject shopPromptUI; 

    private Animator anim;
    private bool isPlayerInRange = false;
    private bool isDialogueOpen = false;
    private Transform mainCamera;
    private Transform playerTransform; 

    private void Start()
    {
        anim = GetComponent<Animator>();
        mainCamera = Camera.main.transform;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (overheadUI != null) overheadUI.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (shopPromptUI != null) shopPromptUI.SetActive(false);

        if (shopButton != null) shopButton.onClick.AddListener(OnShopButtonClicked);
        if (exitButton != null) exitButton.onClick.AddListener(CloseDialogue);
    }

    private void Update()
    {
        bool isShopOpen = ShopManager.Instance.shopPanel.activeSelf;
        bool isGrounded = true;
        if (ShopManager.Instance.playerAnimator != null)
        {
            isGrounded = ShopManager.Instance.playerAnimator.GetBool("IsGrounded");
        }

        bool isMenuOpen = isShopOpen || isDialogueOpen;

        if (playerTransform != null && overheadUI != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance <= iconVisibleDistance && !isMenuOpen)
            {
                overheadUI.SetActive(true); 
                
                if (npcNameText != null) npcNameText.SetActive(isPlayerInRange);
            }
            else
            {
                overheadUI.SetActive(false); 
            }
        }

        if (isPlayerInRange)
        {
            bool showPrompts = !isMenuOpen && isGrounded;
            if (shopPromptUI != null) shopPromptUI.SetActive(showPrompts);

            if (showPrompts && Input.GetKeyDown(KeyCode.F))
            {
                if (Time.timeScale == 0f) return;
                OpenDialogue();
            }
        }
    }

    private void OpenDialogue()
    {
        isDialogueOpen = true;
        if (UIManager.Instance != null) UIManager.Instance.isDialogueOpen = true;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (anim != null) anim.SetTrigger("Talk");
        ShopManager.Instance.currentShopkeeperAnim = anim;

        if (ShopManager.Instance.playerAnimator != null)
        {
            ShopManager.Instance.playerAnimator.Play("Movement");
            ShopManager.Instance.playerAnimator.SetFloat("Speed", 0f);
        }
        if (ShopManager.Instance.playerMovementScript != null) 
            ShopManager.Instance.playerMovementScript.enabled = false;
        
        if (ShopManager.Instance.playerCameraObject != null)
        {
            MonoBehaviour camInput = ShopManager.Instance.playerCameraObject.GetComponent("CinemachineInputAxisController") as MonoBehaviour;
            if (camInput != null) camInput.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseDialogue()
    {
        isDialogueOpen = false;
        if (UIManager.Instance != null) UIManager.Instance.isDialogueOpen = false;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (ShopManager.Instance.playerMovementScript != null) 
            ShopManager.Instance.playerMovementScript.enabled = true;

        if (ShopManager.Instance.playerCameraObject != null)
        {
            MonoBehaviour camInput = ShopManager.Instance.playerCameraObject.GetComponent("CinemachineInputAxisController") as MonoBehaviour;
            if (camInput != null) camInput.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReopenDialogue()
    {
        isDialogueOpen = true;
        if (UIManager.Instance != null) UIManager.Instance.isDialogueOpen = true;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
    }

    private void OnShopButtonClicked()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        isDialogueOpen = false; 

        ShopManager.Instance.currentNPC = this;
        ShopManager.Instance.OpenShop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (shopPromptUI != null) shopPromptUI.SetActive(false);
            
            CloseDialogue();
            ShopManager.Instance.CloseShop();
        }
    }
}