using UnityEngine;

public class MerchantInteract : MonoBehaviour
{
    [Header("UI Prompt")]
    public GameObject shopPromptUI; 

    private bool isPlayerInRange = false;

    private void Start()
    {
        if (shopPromptUI != null)
        {
            shopPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        if (isPlayerInRange)
        {
            bool isShopOpen = ShopManager.Instance.shopPanel.activeSelf;

            if (shopPromptUI != null)
            {
                shopPromptUI.SetActive(!isShopOpen);
            }

            if (!isShopOpen && Input.GetKeyDown(KeyCode.F))
            {
                if (Time.timeScale == 0f) return; 

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