using UnityEngine;

/// <summary>
/// Smooth FPS mouse look.
///
/// Attach to the Player root GameObject.
/// Assign the Camera (or its parent) to <cameraTransform>.
///
/// The script rotates:
///   • the Player body  horizontally  (yaw)   – left / right
///   • the Camera holder vertically   (pitch) – up / down
/// Keeping these separate prevents the body from tilting forward/backward.
/// </summary>
public class MouseLook : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 2f;
    [SerializeField] private float sensitivityY = 2f;

    [Header("Pitch Limits (degrees)")]
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch =  85f;

    [Header("Smoothing (0 = off)")]
    [SerializeField] [Range(0f, 20f)] private float smoothing = 5f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform; // the CameraHolder or Camera itself

    // ── Private state ─────────────────────────────────────────────────────────
    private float _pitch;           // accumulated vertical rotation
    private float _smoothPitch;
    private float _smoothYaw;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        // Lock and hide cursor for FPS feel
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (cameraTransform == null)
            Debug.LogError("[MouseLook] cameraTransform is not assigned!");
    }

    private void Update()
    {
        HandleCursorToggle();

        // Only rotate if cursor is locked
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivityX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY;

        if (smoothing > 0f)
        {
            _smoothYaw   = Mathf.Lerp(_smoothYaw,   mouseX, smoothing * Time.deltaTime);
            _smoothPitch = Mathf.Lerp(_smoothPitch, mouseY, smoothing * Time.deltaTime);
        }
        else
        {
            _smoothYaw   = mouseX;
            _smoothPitch = mouseY;
        }

        // Yaw: rotate the player body left/right
        transform.Rotate(Vector3.up, _smoothYaw);

        // Pitch: rotate camera up/down (inverted Y = subtract)
        _pitch -= _smoothPitch;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

        if (cameraTransform != null)
        {
            Vector3 localEuler = cameraTransform.localEulerAngles;
            localEuler.x = _pitch;
            cameraTransform.localEulerAngles = localEuler;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    /// Press Escape to unlock the cursor (e.g., for pause menus).
    private void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// Temporarily set sensitivity multiplier (e.g., during ADS).
    public void SetSensitivityMultiplier(float multiplier)
    {
        sensitivityX = 2f * multiplier;
        sensitivityY = 2f * multiplier;
    }
}
