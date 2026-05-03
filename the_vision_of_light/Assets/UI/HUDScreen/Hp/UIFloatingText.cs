using UnityEngine;
using TMPro;

public class UIFloatingText : MonoBehaviour
{
    public float moveSpeedX = 50f;
    public float moveSpeedY = 10f;
    public float fadeSpeed = 1.5f;
    public float lifeTime = 2f;

    private TextMeshProUGUI textMesh;
    private Color textColor;

    public void Setup(string text, Color color)
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.color = color;
        textColor = color;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += new Vector3(moveSpeedX, moveSpeedY, 0) * Time.deltaTime;

        textColor.a -= fadeSpeed * Time.deltaTime;
        textMesh.color = textColor;
    }
}