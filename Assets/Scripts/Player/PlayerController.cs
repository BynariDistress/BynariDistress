using UnityEngine;

/// <summary>
/// Core FPS player controller.
/// Requires a CharacterController component on the same GameObject.
///
/// Inspector setup:
///   walkSpeed    – normal movement speed  (default 5)
///   sprintSpeed  – speed while Left Shift  (default 9)
///   crouchSpeed  – speed while crouching   (default 2.5)
///   jumpHeight   – how high the player jumps (default 1.5)
///   gravity      – downward acceleration    (default -20)
///   standHeight  – CharacterController height when standing (default 2)
///   crouchHeight – CharacterController height when crouching (default 1)
///   cameraStandY – local Y of camera when standing  (default 0.8)
///   crouchCamY   – local Y of camera when crouching (default 0.2)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float walkSpeed   = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity    = -20f;

    [Header("Crouch")]
    [SerializeField] private float standHeight  = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchCamY   = 0.2f;
    [SerializeField] private float standCamY    = 0.8f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("References")]
    [SerializeField] private Transform cameraHolder; // child object that holds the Camera

    // ── Private state ─────────────────────────────────────────────────────────
    private CharacterController _cc;
    private Vector3 _velocity;          // stores vertical velocity (gravity / jump)
    private bool    _isCrouching;
    private float   _targetCamY;
    private float   _targetHeight;

    // Public read-only accessors used by other scripts (e.g. animations, UI)
    public bool  IsSprinting  { get; private set; }
    public bool  IsCrouching  => _isCrouching;
    public bool  IsGrounded   => _cc.isGrounded;
    public float CurrentSpeed { get; private set; }

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        // Validate camera holder reference
        if (cameraHolder == null)
        {
            Debug.LogError("[PlayerController] cameraHolder is not assigned! " +
                           "Create a child empty GameObject called 'CameraHolder' " +
                           "and drag it here.");
        }

        _targetCamY   = standCamY;
        _targetHeight = standHeight;
    }

    private void Update()
    {
        HandleGrounding();
        HandleCrouch();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        AnimateCrouchTransition();
    }

    // ── Grounding ─────────────────────────────────────────────────────────────
    /// Reset downward velocity when the player is on the ground so gravity
    /// does not accumulate into a large negative number.
    private void HandleGrounding()
    {
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;  // small constant keeps the controller "stuck" to ground
    }

    // ── Movement ──────────────────────────────────────────────────────────────
    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");   // A / D
        float v = Input.GetAxis("Vertical");     // W / S

        // Sprint only when moving forward, not crouching
        IsSprinting = Input.GetKey(KeyCode.LeftShift) && v > 0.1f && !_isCrouching;

        float speed = _isCrouching ? crouchSpeed
                    : IsSprinting  ? sprintSpeed
                    :                walkSpeed;

        CurrentSpeed = speed;

        // Move in the direction the player is facing
        Vector3 move = transform.right * h + transform.forward * v;
        _cc.Move(move * (speed * Time.deltaTime));
    }

    // ── Jump ──────────────────────────────────────────────────────────────────
    private void HandleJump()
    {
        // Cannot jump while crouching (optional design choice – remove if desired)
        if (Input.GetButtonDown("Jump") && _cc.isGrounded && !_isCrouching)
        {
            // v² = 2gh  →  v = sqrt(2 * |gravity| * jumpHeight)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // ── Gravity ───────────────────────────────────────────────────────────────
    private void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // ── Crouch ────────────────────────────────────────────────────────────────
    private void HandleCrouch()
    {
        // Toggle on C press
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Before standing up, check there is room overhead
            if (_isCrouching && !CanStandUp())
                return;

            _isCrouching  = !_isCrouching;
            _targetHeight = _isCrouching ? crouchHeight : standHeight;
            _targetCamY   = _isCrouching ? crouchCamY   : standCamY;
        }
    }

    /// Smooth transition of CharacterController height and camera Y position.
    private void AnimateCrouchTransition()
    {
        // Lerp controller height
        if (!Mathf.Approximately(_cc.height, _targetHeight))
        {
            float newHeight = Mathf.Lerp(_cc.height, _targetHeight,
                                         crouchTransitionSpeed * Time.deltaTime);
            float heightDelta = newHeight - _cc.height;

            _cc.height = newHeight;
            // Shift center so the feet stay on the ground
            _cc.center = new Vector3(0f, _cc.height / 2f, 0f);

            // Move the whole capsule up/down by half the height change
            transform.position += new Vector3(0f, heightDelta / 2f, 0f);
        }

        // Lerp camera holder Y
        if (cameraHolder != null)
        {
            Vector3 localPos = cameraHolder.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, _targetCamY,
                                    crouchTransitionSpeed * Time.deltaTime);
            cameraHolder.localPosition = localPos;
        }
    }

    /// Spherecast upward to detect whether standing up is safe.
    private bool CanStandUp()
    {
        // Cast a small sphere upward from mid-body to check for ceiling collision
        float checkDistance = standHeight - crouchHeight;
        Vector3 origin = transform.position + Vector3.up * (crouchHeight * 0.5f);
        return !Physics.SphereCast(origin, _cc.radius * 0.9f, Vector3.up,
                                   out _, checkDistance, ~LayerMask.GetMask("Player"));
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// Called by external scripts (e.g. damage effects) to push the player.
    public void AddImpulse(Vector3 force)
    {
        _velocity += force;
    }
}
