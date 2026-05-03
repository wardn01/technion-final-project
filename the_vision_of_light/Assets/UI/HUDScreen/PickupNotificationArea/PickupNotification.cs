using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PickupNotification : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemText;
    public CanvasGroup canvasGroup;
    
    public float showTime = 2f;
    public float fadeTime = 1f;

    private float timer;

    public void Setup(ItemData item, int amount)
    {
        itemIcon.sprite = item.itemIcon;
        itemText.text = $"{item.itemName} x{amount}";
        timer = showTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        
        if (timer <= 0)
        {
            canvasGroup.alpha -= (1f / fadeTime) * Time.deltaTime;
            
            if (canvasGroup.alpha <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}