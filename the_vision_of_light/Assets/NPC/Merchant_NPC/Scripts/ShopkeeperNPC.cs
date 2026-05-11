using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopkeeperNPC : MonoBehaviour
{
    [Header("NPC Identity")]
    public string npcName = "Merchant"; 
    public Sprite npcIcon; 
    public List<ItemData> itemsToSell; 
    
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

        if (overheadNameText != null) overheadNameText.text = npcName;
        if (overheadIconImage != null && npcIcon != null) overheadIconImage.sprite = npcIcon;
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

        if (isPlayerInRange)
        {
            bool isGrounded = ShopManager.Instance.playerAnimator != null && ShopManager.Instance.playerAnimator.GetBool("IsGrounded");
            bool showPrompts = !isMenuOpen && isGrounded;

            if (showPrompts)
            {
                ShopManager.Instance.ShowInteractPrompt(npcName);

                if (Input.GetKeyDown(KeyCode.F) && Time.timeScale != 0f)
                {
                    ShopManager.Instance.OpenDialogue(this);
                    ShopManager.Instance.HideInteractPrompt();
                }
            }
            else
            {
                ShopManager.Instance.HideInteractPrompt();
            }
        }
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
            ShopManager.Instance.HideInteractPrompt();
            
            if (ShopManager.Instance.currentNPC == this)
            {
                ShopManager.Instance.CloseDialogue();
                ShopManager.Instance.CloseShop();
            }
        }
    }
}