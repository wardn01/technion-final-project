using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 2f;
    public float lifetime = 1.5f;

    private TextMeshPro textMesh;
    private Color textColor;
    private Camera targetCamera;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textColor = textMesh.color;
    }

    private void Start()
    {
        targetCamera = Camera.main;

        if (targetCamera == null)
        {
            targetCamera = FindAnyObjectByType<Camera>();
        }
    }

    public void Setup(float damageAmount)
    {
        textMesh.text = damageAmount.ToString("0");
        Destroy(gameObject, lifetime);
    }

    private void LateUpdate()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (targetCamera != null)
        {
            transform.rotation = targetCamera.transform.rotation;
        }

        textColor.a -= (1f / lifetime) * Time.deltaTime;
        textMesh.color = textColor;
    }
}