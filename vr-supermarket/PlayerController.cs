using UnityEngine;

/// <summary>
/// First-person player controller using Unity's CharacterController.
///
/// Controls:
///   W/A/S/D  — Move
///   Mouse    — Look around
///   Shift    — Sprint
///   Space    — Jump
///
/// IMPORTANT — Input System Setup:
///   Go to Edit → Project Settings → Player → Active Input Handling
///   and select "Both" so that Input.GetAxis() works alongside the
///   New Input System package that's installed in this project.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed      = 4.0f;
    public float sprintSpeed    = 7.0f;
    public float jumpHeight     = 1.2f;
    public float gravity        = -12f;

    [Header("Look")]
    [Tooltip("Drag the Camera child transform here.")]
    public Transform cameraTransform;
    public float mouseSensitivity   = 2.0f;
    [Range(40f, 89f)]
    public float maxVerticalAngle   = 80f;

    // ─── Private state ────────────────────────────────────────────────────────
    private CharacterController cc;
    private Vector3 playerVelocity;
    private float   verticalRotation = 0f;
    private bool    inputEnabled     = false;   // Locked until Welcome screen is dismissed

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        // Freeze player while any UI is open
        if (!inputEnabled) return;
        if (StoreManager.Instance != null && StoreManager.Instance.IsAnyPanelOpen) return;

        Move();
        Look();
        ApplyGravity();
    }

    // ─── Movement ─────────────────────────────────────────────────────────────

    private void Move()
    {
        bool grounded = cc.isGrounded;
        if (grounded && playerVelocity.y < 0f)
            playerVelocity.y = -2f;

        float h       = Input.GetAxis("Horizontal");
        float v       = Input.GetAxis("Vertical");
        bool  sprint  = Input.GetKey(KeyCode.LeftShift);
        float speed   = sprint ? sprintSpeed : walkSpeed;

        Vector3 dir = (transform.right * h + transform.forward * v).normalized;
        cc.Move(dir * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && grounded)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void ApplyGravity()
    {
        playerVelocity.y += gravity * Time.deltaTime;
        cc.Move(playerVelocity * Time.deltaTime);
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation  = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Called by StoreManager to lock / unlock player input and cursor.</summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !enabled;
    }

    public bool IsInputEnabled => inputEnabled;
}
