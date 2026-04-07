using UnityEngine;
using UnityEngine.EventSystems;

public class HoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cursor Settings")]
    public Texture2D hoverCursorTexture;

    public Vector2 hotSpot = Vector2.zero; 

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverCursorTexture != null)
        {
            Cursor.SetCursor(hoverCursorTexture, hotSpot, CursorMode.Auto);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetCursor();
    }

    private void OnDisable()
    {
        ResetCursor();
    }

    private void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}