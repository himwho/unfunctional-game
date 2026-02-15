using UnityEngine;

/// <summary>
/// Basic first-person player controller. Temp stick-figure pawn.
/// Uses a CharacterController for movement and the InputManager for input.
/// Attach to a capsule GameObject with a CharacterController component.
/// The Main Camera should be a child of this object, positioned at head height.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 7f;
    public float gravity = -20f;

    [Header("Look")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 80f;
    public Transform cameraTransform;

    [Header("State")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private Vector3 velocity;

    private CharacterController controller;
    private float cameraPitch = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }
    }

    private void Start()
    {
        // Lock cursor for FPS control
        if (InputManager.Instance != null)
        {
            InputManager.Instance.LockCursor();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            return;

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        if (InputManager.Instance != null)
        {
            mouseX = InputManager.Instance.MouseX;
            mouseY = InputManager.Instance.MouseY;
        }
        else
        {
            mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
        }

        // Horizontal rotation - rotate the whole player
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation - pitch the camera only
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        if (cameraTransform != null)
        {
            cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        float h = 0f;
        float v = 0f;

        if (InputManager.Instance != null)
        {
            h = InputManager.Instance.Horizontal;
            v = InputManager.Instance.Vertical;
        }
        else
        {
            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");
        }

        bool running = Input.GetKey(KeyCode.LeftShift);
        float speed = running ? runSpeed : walkSpeed;

        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * speed * Time.deltaTime);

        // Jump
        bool jumpInput = InputManager.Instance != null ? InputManager.Instance.JumpPressed : Input.GetButtonDown("Jump");
        if (jumpInput && isGrounded)
        {
            velocity.y = jumpForce;
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Teleport the player to a world position.
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
        velocity = Vector3.zero;
    }
}
