using UnityEngine;

public class Player_InputManager : MonoBehaviour
{
    public static Player_InputManager Instance { get; private set; }

    public bool isInputLocked = false;

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool AttackPressed { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (isInputLocked)
        {
            Horizontal = 0f;
            Vertical = 0f;
            JumpPressed = false;
            AttackPressed = false;
            return;
        }

        float h = 0f;
        float v = 0f;
        bool jump = false;
        bool attack = false;

        if (KeybindManager.Instance != null)
        {
            var keys = KeybindManager.Instance.keys;
            if (keys.TryGetValue("MoveRight", out KeyCode moveRight) && Input.GetKey(moveRight)) h += 1f;
            if (keys.TryGetValue("MoveLeft", out KeyCode moveLeft) && Input.GetKey(moveLeft)) h -= 1f;
            if (keys.TryGetValue("MoveForward", out KeyCode moveForward) && Input.GetKey(moveForward)) v += 1f;
            if (keys.TryGetValue("MoveBackward", out KeyCode moveBackward) && Input.GetKey(moveBackward)) v -= 1f;

            if (keys.TryGetValue("Jump", out KeyCode jumpKey))
                jump = Input.GetKeyDown(jumpKey);
            if (keys.TryGetValue("NormalAttack", out KeyCode attackKey))
                attack = Input.GetKeyDown(attackKey);
        }
        else
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
            jump = Input.GetKeyDown(KeyCode.Space);
            attack = Input.GetMouseButtonDown(0);
        }

        Horizontal = h;
        Vertical = v;
        JumpPressed = jump;
        AttackPressed = attack;

        HandleQuickSlots();
    }

    private void HandleQuickSlots()
    {
        if (QuickSlotManager.Instance == null) return;

        if (KeybindManager.Instance != null)
        {
            var keys = KeybindManager.Instance.keys;
            if (keys.TryGetValue("Slot1", out KeyCode slot1) && Input.GetKeyDown(slot1)) QuickSlotManager.Instance.ExecuteSlotAction(0);
            if (keys.TryGetValue("Slot2", out KeyCode slot2) && Input.GetKeyDown(slot2)) QuickSlotManager.Instance.ExecuteSlotAction(1);
            if (keys.TryGetValue("Slot3", out KeyCode slot3) && Input.GetKeyDown(slot3)) QuickSlotManager.Instance.ExecuteSlotAction(2);
            if (keys.TryGetValue("Slot4", out KeyCode slot4) && Input.GetKeyDown(slot4)) QuickSlotManager.Instance.ExecuteSlotAction(3);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) QuickSlotManager.Instance.ExecuteSlotAction(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) QuickSlotManager.Instance.ExecuteSlotAction(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) QuickSlotManager.Instance.ExecuteSlotAction(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) QuickSlotManager.Instance.ExecuteSlotAction(3);
        }
    }
}