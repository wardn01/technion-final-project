using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopkeeperNPC : MonoBehaviour
{
    [Header("NPC Data")]
    public NPCData myData;

    public List<ItemData> itemsToSell => myData != null ? myData.itemsToSell : new List<ItemData>();

    [Header("Map Settings")]
    public Transform mapIconObject; 

    [Header("Overhead UI (World Space)")]
    public GameObject overheadUI; 
    public GameObject npcNameTextObj; 
    public TextMeshProUGUI overheadNameText; 
    public Image overheadIconImage; 
    public float iconVisibleDistance = 15f; 

    private bool isPlayerInRange = false;
    private Transform playerTransform; 

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (overheadUI != null) overheadUI.SetActive(false);

        if (myData != null)
        {
            if (overheadNameText != null) overheadNameText.text = myData.npcName;
            if (overheadIconImage != null && myData.npcIcon != null) overheadIconImage.sprite = myData.npcIcon;
            
            SetupMapIcon();
        }
    }

    void SetupMapIcon()
    {
        if (mapIconObject == null || myData.npcIcon == null) return;

        SpriteRenderer sr = mapIconObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = mapIconObject.gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = myData.npcIcon;
        sr.sortingOrder = 10;
        
        mapIconObject.gameObject.layer = LayerMask.NameToLayer("Minimap");
    }

    private void Update()
    {
        bool isShopOpen = ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf;
        bool isDialogueOpen = UIManager.Instance != null && UIManager.Instance.isDialogueOpen;
        bool isMenuOpen = isShopOpen || isDialogueOpen;

        if (playerTransform != null && overheadUI != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= iconVisibleDistance && !isMenuOpen)
            {
                overheadUI.SetActive(true); 
                if (npcNameTextObj != null) npcNameTextObj.SetActive(isPlayerInRange);
            }
            else
            {
                overheadUI.SetActive(false); 
            }
        }

        if (isPlayerInRange && myData != null)
        {
            if (!isMenuOpen)
            {
                if (ShopManager.Instance == null) return;

                ShopManager.Instance.ShowInteractPrompt(myData.npcName);

                if (Input.GetKeyDown(KeyCode.F) && Time.timeScale != 0f)
                {
                    ShopManager.Instance.HideInteractPrompt();
                    
                    FaceEachOtherInstantly();
                    
                    ShopManager.Instance.currentNPC = this;
                    ShopManager.Instance.currentShopkeeperAnim = GetComponent<Animator>();
                    
                    if (ShopManager.Instance.currentShopkeeperAnim != null) 
                    {
                        ShopManager.Instance.currentShopkeeperAnim.SetInteger("TalkIndex", Random.Range(0, 3));
                        ShopManager.Instance.currentShopkeeperAnim.SetTrigger("Talk");
                    }

                    DialogueTrigger questTrigger = GetComponent<DialogueTrigger>();
                    if (questTrigger != null)
                    {
                        questTrigger.TriggerDialogue();
                    }
                    else if (DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.StartDialogue(myData.npcName, myData.welcomeDialogue, true, this);
                    }
                }
            }
            else
            {
                if (ShopManager.Instance != null) ShopManager.Instance.HideInteractPrompt();
            }
        }
    }

    private void FaceEachOtherInstantly()
    {
        if (playerTransform == null) return;

        Vector3 playerDir = transform.position - playerTransform.position;
        playerDir.y = 0;
        if (playerDir != Vector3.zero) playerTransform.rotation = Quaternion.LookRotation(playerDir);
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

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.HideInteractPrompt();
                if (ShopManager.Instance.currentNPC == this)
                {
                    if (DialogueManager.Instance != null) DialogueManager.Instance.EndDialogue();
                    ShopManager.Instance.CloseShop();
                }
            }
        }
    }
}