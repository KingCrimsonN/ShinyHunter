using UnityEngine;

/// <summary>
/// Basic first-person movement: WASD move, mouse look, gravity, and
/// independent sine-wave bob for the camera and the hands.
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

    [Header("Camera Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [Tooltip("Bob cycles per second at walk speed. Scales up automatically when sprinting.")]
    [SerializeField] private float bobFrequency = 8f;
    [SerializeField] private float bobVerticalAmount = 0.05f;
    [SerializeField] private float bobHorizontalAmount = 0.03f;
    [Tooltip("How quickly the camera eases toward/away from the bob offset. Higher = snappier.")]
    [SerializeField] private float bobSmoothSpeed = 10f;

    [Header("Hand Bob")]
    [Tooltip("Independent from Camera Bob above - tune separately so hands can feel punchier/looser than the camera instead of always being a fixed fraction of it.")]
    [SerializeField] private bool enableHandBob = true;
    [SerializeField] private Transform hands;
    [SerializeField] private float handBobFrequency = 8f;
    [SerializeField] private float handBobVerticalAmount = 0.02f;
    [SerializeField] private float handBobHorizontalAmount = 0.015f;
    [SerializeField] private float handBobSmoothSpeed = 10f;

    private CharacterController controller;
    private float pitch;
    private Vector3 velocity;

    private Vector3 cameraBasePosition;
    private Vector3 handsBasePosition;
    private float bobTimer;
    private float handBobTimer;
    private bool isMoving;
    private float speedRatio = 1f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPivot != null)
            cameraBasePosition = cameraPivot.localPosition;

        if (hands != null)
            handsBasePosition = hands.localPosition;
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        HandleBob();
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

    private void HandleBob()
    {
        if (enableHeadBob && cameraPivot != null)
        {
            Vector3 offset = ComputeBobOffset(ref bobTimer, bobFrequency, bobVerticalAmount, bobHorizontalAmount);
            Vector3 targetPosition = cameraBasePosition + offset;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, targetPosition, Time.deltaTime * bobSmoothSpeed);
        }

        if (enableHandBob && hands != null)
        {
            Vector3 offset = ComputeBobOffset(ref handBobTimer, handBobFrequency, handBobVerticalAmount, handBobHorizontalAmount);
            Vector3 targetPosition = handsBasePosition + offset;
            hands.localPosition = Vector3.Lerp(hands.localPosition, targetPosition, Time.deltaTime * handBobSmoothSpeed);
        }
    }

    /// <summary>
    /// Shared sine/cosine bob math, parameterized so camera and hands can
    /// run fully independent cycles (different frequency/amplitude, and
    /// their own timer so they aren't forced to stay in phase with each other).
    /// </summary>
    private Vector3 ComputeBobOffset(ref float timer, float frequency, float verticalAmount, float horizontalAmount)
    {
        if (isMoving)
        {
            timer += Time.deltaTime * frequency * speedRatio;

            float verticalOffset = Mathf.Sin(timer) * verticalAmount;
            float horizontalOffset = Mathf.Cos(timer * 0.5f) * horizontalAmount;
            return new Vector3(horizontalOffset, verticalOffset, 0f);
        }

        // Reset the cycle so the next step starts from a neutral position
        // instead of wherever it happened to leave off.
        timer = 0f;
        return Vector3.zero;
    }
}