using UnityEngine;

public class Player_InputManager : MonoBehaviour
{
    public static Player_InputManager Instance;

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

        if (Input.GetKey(KeybindManager.Instance.keys["MoveRight"])) h += 1f;
        if (Input.GetKey(KeybindManager.Instance.keys["MoveLeft"])) h -= 1f;

        if (Input.GetKey(KeybindManager.Instance.keys["MoveForward"])) v += 1f;
        if (Input.GetKey(KeybindManager.Instance.keys["MoveBackward"])) v -= 1f;

        Horizontal = h;
        Vertical = v;

        JumpPressed = Input.GetKeyDown(KeybindManager.Instance.keys["Jump"]);
        AttackPressed = Input.GetKeyDown(KeybindManager.Instance.keys["NormalAttack"]);

        HandleQuickSlots();
    }

    private void HandleQuickSlots()
    {
        if (QuickSlotManager.Instance == null) return;

        if (Input.GetKeyDown(KeybindManager.Instance.keys["Slot1"])) QuickSlotManager.Instance.ExecuteSlotAction(0);
        if (Input.GetKeyDown(KeybindManager.Instance.keys["Slot2"])) QuickSlotManager.Instance.ExecuteSlotAction(1);
        if (Input.GetKeyDown(KeybindManager.Instance.keys["Slot3"])) QuickSlotManager.Instance.ExecuteSlotAction(2);
        if (Input.GetKeyDown(KeybindManager.Instance.keys["Slot4"])) QuickSlotManager.Instance.ExecuteSlotAction(3);
    }
}