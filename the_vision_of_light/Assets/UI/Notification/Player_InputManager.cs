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
        bool jump = false;
        bool attack = false;

        if (KeybindManager.Instance != null)
        {
            if (Input.GetKey(KeybindManager.Instance.keys["MoveRight"])) h += 1f;
            if (Input.GetKey(KeybindManager.Instance.keys["MoveLeft"])) h -= 1f;
            if (Input.GetKey(KeybindManager.Instance.keys["MoveForward"])) v += 1f;
            if (Input.GetKey(KeybindManager.Instance.keys["MoveBackward"])) v -= 1f;

            jump = Input.GetKeyDown(KeybindManager.Instance.keys["Jump"]);
            attack = Input.GetKeyDown(KeybindManager.Instance.keys["NormalAttack"]);
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
            if (Input.GetKeyDown(KeybindManager.Instance.keys["Slot1"])) QuickSlotManager.Instance.ExecuteSlotAction(0);
            if (Input.GetKeyDown(KeybindManager.Instance.keys["Slot2"])) QuickSlotManager.Instance.ExecuteSlotAction(1);
            if (Input.GetKeyDown(KeybindManager.Instance.keys["Slot3"])) QuickSlotManager.Instance.ExecuteSlotAction(2);
            if (Input.GetKeyDown(KeybindManager.Instance.keys["Slot4"])) QuickSlotManager.Instance.ExecuteSlotAction(3);
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