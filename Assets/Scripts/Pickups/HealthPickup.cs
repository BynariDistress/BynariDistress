using UnityEngine;

/// <summary>
/// Health pickup – restores player HP on walk-over or E-key interaction.
/// </summary>
public class HealthPickup : MonoBehaviour, IInteractable
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private float healAmount   = 25f;
    [SerializeField] private bool  autoCollect  = true;
    [SerializeField] private float respawnTime  = 0f;

    [Header("Visual")]
    [SerializeField] private float bobSpeed    = 1.5f;
    [SerializeField] private float bobHeight   = 0.15f;
    [SerializeField] private float rotateSpeed = 60f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;

    // ── IInteractable ─────────────────────────────────────────────────────────
    public string InteractPrompt => $"Pick up Health (+{healAmount} HP)";

    public void Interact(PlayerController player)
    {
        Collect(player.GetComponent<PlayerHealth>());
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
        transform.position = _startPos +
            Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoCollect || _collected) return;
        if (!other.CompareTag("Player")) return;

        Collect(other.GetComponent<PlayerHealth>());
    }

    // ── Collection ────────────────────────────────────────────────────────────
    private void Collect(PlayerHealth ph)
    {
        if (_collected || ph == null) return;

        // Don't collect if already full health (optional – comment out if not desired)
        if (Mathf.Approximately(ph.CurrentHealth, ph.MaxHealth)) return;

        _collected = true;
        ph.Heal(healAmount);

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
