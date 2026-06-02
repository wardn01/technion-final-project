using UnityEngine;

public class MinimapIconUpright : MonoBehaviour
{
    private Transform minimapCamera;

    void Start()
    {
        GameObject cam = GameObject.Find("MinimapCamera");
        if (cam != null)
        {
            minimapCamera = cam.transform;
        }
    }

    void LateUpdate()
    {
        if (minimapCamera != null)
        {
            transform.rotation = Quaternion.Euler(90f, minimapCamera.eulerAngles.y, 0f);
        }
    }
}