using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance;

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

        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");
        JumpPressed = Input.GetKeyDown(KeyCode.Space);
        AttackPressed = Input.GetMouseButtonDown(0);

        HandleQuickSlots();
    }

    private void HandleQuickSlots()
    {
        if (QuickSlotManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) QuickSlotManager.Instance.ExecuteSlotAction(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) QuickSlotManager.Instance.ExecuteSlotAction(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) QuickSlotManager.Instance.ExecuteSlotAction(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) QuickSlotManager.Instance.ExecuteSlotAction(3);
    }
}