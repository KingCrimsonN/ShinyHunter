using UnityEngine;

/// <summary>
/// Basic first-person movement: WASD move, mouse look, gravity, and a
/// sine-wave camera bob while walking/sprinting.
/// GDD explicitly rules out jumping/dashing, so those are omitted
/// (jumpHeight left as a hook if you change your mind later).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera transform, should be a child of this object.")]
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [Tooltip("Bob cycles per second at walk speed. Scales up automatically when sprinting.")]
    [SerializeField] private float bobFrequency = 8f;
    [SerializeField] private float bobVerticalAmount = 0.05f;
    [SerializeField] private float bobHorizontalAmount = 0.03f;
    [Tooltip("How quickly the camera eases toward/away from the bob offset. Higher = snappier.")]
    [SerializeField] private float bobSmoothSpeed = 10f;

    private CharacterController controller;
    private float pitch;
    private Vector3 velocity;

    private Vector3 cameraBasePosition;
    private float bobTimer;
    private bool isMoving;
    private float speedRatio = 1f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPivot != null)
            cameraBasePosition = cameraPivot.localPosition;
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        HandleHeadBob();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * h + transform.forward * v);
        move = Vector3.ClampMagnitude(move, 1f);

        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        isMoving = controller.isGrounded && move.sqrMagnitude > 0.0001f;
        speedRatio = speed / walkSpeed;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f; // small stick-to-ground force

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = move * speed + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraPivot == null) return;

        Vector3 targetOffset;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency * speedRatio;

            float verticalOffset = Mathf.Sin(bobTimer) * bobVerticalAmount;
            float horizontalOffset = Mathf.Cos(bobTimer * 0.5f) * bobHorizontalAmount;
            targetOffset = new Vector3(horizontalOffset, verticalOffset, 0f);
        }
        else
        {
            // Reset the cycle so the next step starts from a neutral position
            // instead of wherever it happened to leave off.
            bobTimer = 0f;
            targetOffset = Vector3.zero;
        }

        Vector3 targetPosition = cameraBasePosition + targetOffset;
        cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, targetPosition, Time.deltaTime * bobSmoothSpeed);
    }
}