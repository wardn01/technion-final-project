using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform player;
    public Transform playerCamera;
    public float height = 500f;

    void LateUpdate()
    {
        if (player != null && playerCamera != null)
        {
            transform.position = new Vector3(player.position.x, player.position.y + height, player.position.z);

            transform.rotation = Quaternion.Euler(90f, playerCamera.eulerAngles.y, 0f);
        }
    }
}