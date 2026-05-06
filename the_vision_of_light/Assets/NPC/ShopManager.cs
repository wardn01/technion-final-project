using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("UI References")]
    public GameObject shopPanel;
    public TextMeshProUGUI goldText;
    public Slider amountSlider;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI totalPriceText;
    public Transform itemsListContainer;

    [Header("Slot Highlight")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private Image currentSelectedSlotImage;

    [Header("Economy References")]
    public ItemData goldItemData; 
    
    private ItemData selectedItem;
    private int currentAmount = 1;

    [Header("Player Reference")]
    public MonoBehaviour playerMovementScript;
    public Animator playerAnimator; 
    public GameObject playerCameraObject;

    [HideInInspector] public Animator currentShopkeeperAnim;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        UpdateGoldUI();
        
        if (amountSlider != null) amountSlider.value = 1;

        if (currentSelectedSlotImage != null) currentSelectedSlotImage.color = normalColor;

        if (itemsListContainer != null && itemsListContainer.childCount > 0)
        {
            Button firstItemBtn = itemsListContainer.GetChild(0).GetComponentInChildren<Button>();
            if (firstItemBtn != null)
            {
                firstItemBtn.onClick.Invoke(); 
            }
        }
        else
        {
            UpdateTotalPrice();
        }

        if (playerAnimator != null)
        {
            playerAnimator.Play("Movement"); 
            playerAnimator.SetFloat("Speed", 0f); 
        }
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        if (playerCameraObject != null)
        {
            MonoBehaviour camInput = playerCameraObject.GetComponent("CinemachineInputAxisController") as MonoBehaviour;
            if (camInput != null) camInput.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);

        if (playerMovementScript != null) playerMovementScript.enabled = true;

        if (playerCameraObject != null)
        {
            MonoBehaviour camInput = playerCameraObject.GetComponent("CinemachineInputAxisController") as MonoBehaviour;
            if (camInput != null) camInput.enabled = true;
        }

        currentShopkeeperAnim = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SelectItem(ItemData item)
    {
        selectedItem = item;
        
        if (amountSlider != null) amountSlider.value = 1;
        UpdateTotalPrice();
    }

    public void HighlightSlot(Image clickedImage)
    {
        if (currentSelectedSlotImage != null)
        {
            currentSelectedSlotImage.color = normalColor;
        }
        currentSelectedSlotImage = clickedImage;
        if (currentSelectedSlotImage != null)
        {
            currentSelectedSlotImage.color = selectedColor;
        }
    }

    public void UpdateTotalPrice()
    {
        if (selectedItem == null || amountSlider == null) return;
        currentAmount = (int)amountSlider.value;
        if (amountText != null) amountText.text = currentAmount.ToString();
        int totalPrice = selectedItem.value * currentAmount;
        totalPriceText.text = totalPrice.ToString();
    }
    
    public void BuySelectedItem()
    {
        if (selectedItem == null || goldItemData == null) return;

        int totalCost = selectedItem.value * currentAmount;
        int currentGold = InventoryManager.Instance.GetItemAmount(goldItemData);

        if (currentGold >= totalCost)
        {
            InventoryManager.Instance.RemoveItem(goldItemData, totalCost);
            InventoryManager.Instance.AddItem(selectedItem, currentAmount);
            UpdateGoldUI();
            if (amountSlider != null) amountSlider.value = 1;

            if (currentShopkeeperAnim != null)
            {
                currentShopkeeperAnim.SetTrigger("ThankYou");
            }
        }
        else
        {
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowWarning("Not enough gold!");
            }
        }
    }

    public void UpdateGoldUI()
    {
        if (goldText != null && goldItemData != null)
        {
            int currentGold = InventoryManager.Instance.GetItemAmount(goldItemData);
            goldText.text = currentGold.ToString();
        }
    }
}