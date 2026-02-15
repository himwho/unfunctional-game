using UnityEngine;

/// <summary>
/// Centralized input management singleton. 
/// Handles input state, cursor locking, and provides a clean API 
/// for other systems to query input without directly using Input class.
/// Lives in the GLOBAL scene.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2.0f;
    public bool invertMouseY = false;

    [Header("State")]
    [SerializeField] private bool inputEnabled = true;
    [SerializeField] private bool cursorLocked = false;

    // Cached input values each frame
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public float MouseX { get; private set; }
    public float MouseY { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool PausePressed { get; private set; }
    public bool ClickPressed { get; private set; }
    public bool ClickHeld { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            ClearInputs();
            return;
        }

        // Movement
        Horizontal = Input.GetAxis("Horizontal");
        Vertical = Input.GetAxis("Vertical");

        // Mouse look
        MouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        MouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertMouseY ? -1f : 1f);

        // Actions
        JumpPressed = Input.GetButtonDown("Jump");
        InteractPressed = Input.GetKeyDown(KeyCode.E);
        PausePressed = Input.GetKeyDown(KeyCode.Escape);
        ClickPressed = Input.GetMouseButtonDown(0);
        ClickHeld = Input.GetMouseButton(0);

        // Handle pause toggle
        if (PausePressed && GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    public void EnableInput()
    {
        inputEnabled = true;
    }

    public void DisableInput()
    {
        inputEnabled = false;
        ClearInputs();
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    public bool IsCursorLocked()
    {
        return cursorLocked;
    }

    private void ClearInputs()
    {
        Horizontal = 0f;
        Vertical = 0f;
        MouseX = 0f;
        MouseY = 0f;
        JumpPressed = false;
        InteractPressed = false;
        PausePressed = false;
        ClickPressed = false;
        ClickHeld = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
