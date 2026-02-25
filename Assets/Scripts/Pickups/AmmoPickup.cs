using UnityEngine;

/// <summary>
/// Ammo pickup. Implements IInteractable so the player can press E to collect,
/// OR it can auto-collect on trigger overlap.
///
/// Setup:
///  1. Create a small cube/sphere with a collider set to "Is Trigger".
///  2. Attach this script and assign <weaponManager>.
///  3. Tag the GameObject "Pickup" (optional, for visual distinction).
/// </summary>
public class AmmoPickup : MonoBehaviour, IInteractable
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Ammo")]
    [SerializeField] private int   ammoAmount   = 30;
    [SerializeField] private bool  autoCollect  = true;   // collect on walk-over
    [SerializeField] private float respawnTime  = 0f;     // 0 = no respawn

    [Header("Visual")]
    [SerializeField] private float bobSpeed     = 1.5f;
    [SerializeField] private float bobHeight    = 0.15f;
    [SerializeField] private float rotateSpeed  = 60f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;

    // ── IInteractable ─────────────────────────────────────────────────────────
    public string InteractPrompt => $"Pick up Ammo (+{ammoAmount})";

    public void Interact(PlayerController player)
    {
        Collect(player.GetComponent<WeaponManager>());
    }

    // ── Private state ─────────────────────────────────────────────────────────
    private Vector3 _startPos;
    private bool    _collected;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        // Floating bob animation
        transform.position = _startPos +
            Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);

        // Slow rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoCollect || _collected) return;
        if (!other.CompareTag("Player")) return;

        var wm = other.GetComponent<WeaponManager>();
        Collect(wm);
    }

    // ── Collection ────────────────────────────────────────────────────────────
    private void Collect(WeaponManager wm)
    {
        if (_collected) return;
        _collected = true;

        wm?.AddAmmoToCurrent(ammoAmount);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (respawnTime > 0f)
        {
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), respawnTime);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Respawn()
    {
        _collected = false;
        gameObject.SetActive(true);
    }
}
