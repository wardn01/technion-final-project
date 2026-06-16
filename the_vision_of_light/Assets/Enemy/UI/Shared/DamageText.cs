using UnityEngine;
using TMPro;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Floating damage number that rises, fades, and billboards toward the camera.
    /// </summary>
    public class DamageText : MonoBehaviour
    {
        #region Settings
        [Header("Settings")]
        public float moveSpeed = 2f;
        public float lifetime = 1.5f;

        private TextMeshPro textMesh;
        private Color textColor;
        private Camera targetCamera;
        #endregion

        #region Unity Lifecycle
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
        #endregion

        #region Public API
        /// <summary>Displays the damage value and schedules self-destruction.</summary>
        public void Setup(float damageAmount)
        {
            textMesh.text = damageAmount.ToString("0");
            Destroy(gameObject, lifetime);
        }
        #endregion
    }
}
