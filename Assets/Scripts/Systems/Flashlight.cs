using UnityEngine;

/// <summary>
/// Toggles a spotlight attached to the player/weapon on F key press.
///
/// Setup:
///  1. Add a child GameObject to the camera (or weapon model) called "Flashlight".
///  2. Add a Spot Light component to it (range ~15, angle ~35, intensity ~3).
///  3. Assign that Light component to <flashlight> in the inspector.
///     OR leave it unassigned – the script will find the first Light child
///     automatically.
/// </summary>
public class Flashlight : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Light")]
    [SerializeField] private Light flashlight;
    [SerializeField] private bool  startOn    = false;

    [Header("Battery (optional – 0 = infinite)")]
    [SerializeField] private float batteryLife    = 0f;    // seconds, 0 = infinite
    [SerializeField] private float rechargeRate   = 0f;    // seconds per second when off

    [Header("Audio")]
    [SerializeField] private AudioClip toggleOnSound;
    [SerializeField] private AudioClip toggleOffSound;

    // ── Private state ─────────────────────────────────────────────────────────
    private float       _battery;        // remaining battery (if used)
    private AudioSource _audio;

    public bool IsOn => flashlight != null && flashlight.enabled;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _audio = GetComponent<AudioSource>();

        // Auto-find light in children if not assigned
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>();

        if (flashlight == null)
            Debug.LogWarning("[Flashlight] No Light component found. " +
                             "Add a child Light to the player/camera.");

        _battery = batteryLife > 0f ? batteryLife : float.MaxValue;

        if (flashlight != null)
            flashlight.enabled = startOn;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            Toggle();

        HandleBattery();
    }

    // ── Toggle ────────────────────────────────────────────────────────────────
    private void Toggle()
    {
        if (flashlight == null) return;

        bool turnOn = !flashlight.enabled;

        // Don't turn on if battery is dead
        if (turnOn && _battery <= 0f)
        {
            Debug.Log("[Flashlight] Battery dead.");
            return;
        }

        flashlight.enabled = turnOn;
        PlaySound(turnOn ? toggleOnSound : toggleOffSound);
    }

    // ── Battery ───────────────────────────────────────────────────────────────
    private void HandleBattery()
    {
        if (batteryLife <= 0f) return;  // infinite battery

        if (IsOn)
        {
            _battery -= Time.deltaTime;
            if (_battery <= 0f)
            {
                _battery = 0f;
                if (flashlight != null) flashlight.enabled = false;
                Debug.Log("[Flashlight] Battery depleted.");
            }
        }
        else if (rechargeRate > 0f)
        {
            _battery = Mathf.Min(_battery + rechargeRate * Time.deltaTime, batteryLife);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }

    /// Normalised battery level 0-1 for UI display.
    public float BatteryPercent =>
        batteryLife > 0f ? _battery / batteryLife : 1f;
}
