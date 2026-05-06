using UnityEngine;

public class ShopkeeperNPC : MonoBehaviour
{
    [Header("UI Prompt")]
    public GameObject shopPromptUI;

    private Animator anim;
    private bool isPlayerInRange = false;

    private void Start()
    {
        anim = GetComponent<Animator>();
        
        if (shopPromptUI != null)
        {
            shopPromptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerInRange)
        {
            bool isShopOpen = ShopManager.Instance.shopPanel.activeSelf;
            
            bool isGrounded = true;
            if (ShopManager.Instance.playerAnimator != null)
            {
                isGrounded = ShopManager.Instance.playerAnimator.GetBool("IsGrounded");
            }

            if (shopPromptUI != null)
            {
                shopPromptUI.SetActive(!isShopOpen && isGrounded);
            }

            if (!isShopOpen && isGrounded && Input.GetKeyDown(KeyCode.F))
            {
                if (Time.timeScale == 0f) return;

                if (anim != null) anim.SetTrigger("Talk");

                ShopManager.Instance.currentShopkeeperAnim = anim;

                ShopManager.Instance.OpenShop();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            
            if (shopPromptUI != null) shopPromptUI.SetActive(false);
            ShopManager.Instance.CloseShop(); 
        }
    }
}