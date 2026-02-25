using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the player's health, damage intake, healing, and death.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private float maxHealth    = 100f;
    [SerializeField] private float startHealth  = 100f;

    [Header("Regeneration (optional)")]
    [SerializeField] private bool  regenEnabled = false;
    [SerializeField] private float regenDelay   = 5f;    // seconds after last damage
    [SerializeField] private float regenRate    = 5f;    // HP per second

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;  // (current, max)
    public UnityEvent               OnDeath;

    // ── Private state ─────────────────────────────────────────────────────────
    private float        _currentHealth;
    private bool         _isDead;
    private float        _regenTimer;
    private AudioSource  _audio;

    // Read-only properties for UI and other scripts
    public float CurrentHealth  => _currentHealth;
    public float MaxHealth      => maxHealth;
    public float HealthPercent  => _currentHealth / maxHealth;
    public bool  IsDead         => _isDead;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _currentHealth = Mathf.Clamp(startHealth, 0f, maxHealth);
    }

    private void Update()
    {
        HandleRegen();
    }

    // ── Regen ─────────────────────────────────────────────────────────────────
    private void HandleRegen()
    {
        if (!regenEnabled || _isDead || Mathf.Approximately(_currentHealth, maxHealth))
            return;

        _regenTimer -= Time.deltaTime;
        if (_regenTimer <= 0f)
            Heal(regenRate * Time.deltaTime);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// Apply damage to the player. <paramref name="amount"/> should be positive.
    public void TakeDamage(float amount)
    {
        if (_isDead || amount <= 0f) return;

        _currentHealth = Mathf.Max(_currentHealth - amount, 0f);
        _regenTimer    = regenDelay;

        PlaySound(hitSound);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0f)
            Die();
    }

    /// Restore health. Clamped to maxHealth.
    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f) return;

        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    // ── Death ─────────────────────────────────────────────────────────────────
    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        PlaySound(deathSound);
        OnDeath?.Invoke();

        Debug.Log("[PlayerHealth] Player died.");

        // Disable movement & look so the scene doesn't keep running
        if (TryGetComponent<PlayerController>(out var ctrl))
            ctrl.enabled = false;
        if (TryGetComponent<MouseLook>(out var look))
            look.enabled = false;

        // TODO: Show death / game-over UI via UIManager.Instance.ShowDeathScreen()
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }
}
