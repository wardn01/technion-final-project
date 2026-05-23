using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject shopPanel;
    public TextMeshProUGUI goldText;
    public Slider amountSlider;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI totalPriceText;
    public Transform itemsListContainer;

    [Header("Dynamic Shop Generation")]
    public GameObject shopSlotPrefab; 
    
    private List<GameObject> pooledSlots = new List<GameObject>(); 

    [Header("Dialogue UI (Shared)")]
    public GameObject shopPromptUI; 
    public TextMeshProUGUI promptNameText;

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
    [HideInInspector] public ShopkeeperNPC currentNPC;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (itemsListContainer != null)
        {
            foreach (Transform child in itemsListContainer)
            {
                pooledSlots.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        if (shopPromptUI != null) shopPromptUI.SetActive(false);
    }

    public void ShowInteractPrompt(string npcName)
    {
        if (shopPromptUI != null) shopPromptUI.SetActive(true);
        if (promptNameText != null) promptNameText.text = npcName;
    }

    public void HideInteractPrompt()
    {
        if (shopPromptUI != null) shopPromptUI.SetActive(false);
    }

    public void OpenShop(List<ItemData> itemsToSell)
    {
        shopPanel.SetActive(true);
        UpdateGoldUI();
        
        if (amountSlider != null) amountSlider.value = 1;
        if (currentSelectedSlotImage != null) currentSelectedSlotImage.color = normalColor;

        for (int i = 0; i < pooledSlots.Count; i++)
        {
            pooledSlots[i].SetActive(false);
        }

        for (int i = 0; i < itemsToSell.Count; i++)
        {
            ItemData item = itemsToSell[i];
            GameObject slot;

            if (i < pooledSlots.Count)
            {
                slot = pooledSlots[i];
            }
            else
            {
                slot = Instantiate(shopSlotPrefab, itemsListContainer);
                pooledSlots.Add(slot);
            }

            slot.SetActive(true);

            Transform iconTr = slot.transform.Find("Icon");
            if (iconTr != null) iconTr.GetComponent<Image>().sprite = item.itemIcon;

            Transform nameTr = slot.transform.Find("Name");
            if (nameTr != null) nameTr.GetComponent<TextMeshProUGUI>().text = item.itemName;

            Transform priceTr = slot.transform.Find("Price");
            if (priceTr != null) priceTr.GetComponent<TextMeshProUGUI>().text = item.value.ToString();

            Button btn = slot.GetComponent<Button>();
            Image bgImage = slot.GetComponent<Image>();

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => 
            {
                SelectItem(item);
                HighlightSlot(bgImage);
            });
        }

        if (itemsListContainer.GetComponent<LayoutGroup>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsListContainer.GetComponent<RectTransform>());
        }

        Canvas.ForceUpdateCanvases();

        if (itemsToSell.Count > 0)
        {
            SelectItem(itemsToSell[0]);
            if (pooledSlots.Count > 0 && pooledSlots[0].GetComponent<Image>() != null) 
                HighlightSlot(pooledSlots[0].GetComponent<Image>());
                
            UpdateTotalPrice();
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        SetPlayerFreeze(false);
        currentShopkeeperAnim = null;
        currentNPC = null;
    }

    public void BackToDialogue()
    {
        shopPanel.SetActive(false); 
        if (currentNPC != null && DialogueManager.Instance != null) 
        {
            DialogueManager.Instance.ShowShopOptions();
        }
        else 
        {
            CloseShop(); 
        }
    }

    public void SetPlayerFreeze(bool freeze)
    {
        if (freeze)
        {
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
        else
        {
            if (playerMovementScript != null) playerMovementScript.enabled = true;
            if (playerCameraObject != null)
            {
                MonoBehaviour camInput = playerCameraObject.GetComponent("CinemachineInputAxisController") as MonoBehaviour;
                if (camInput != null) camInput.enabled = true;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SelectItem(ItemData item)
    {
        selectedItem = item;
        if (amountSlider != null) amountSlider.value = 1;
        UpdateTotalPrice();
    }

    public void HighlightSlot(Image clickedImage)
    {
        if (currentSelectedSlotImage != null) currentSelectedSlotImage.color = normalColor;
        currentSelectedSlotImage = clickedImage;
        if (currentSelectedSlotImage != null) currentSelectedSlotImage.color = selectedColor;
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