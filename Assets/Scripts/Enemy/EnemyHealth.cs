using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Health component for enemies.
///
/// Attach to the root GameObject of an enemy prefab.
/// Works with WeaponBase raycast hits, Grenade blast damage,
/// and MeleeAttack sphere-casts.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private float maxHealth   = 100f;
    [SerializeField] private float startHealth = 100f;

    [Header("Death")]
    [SerializeField] private float destroyDelay = 3f;    // seconds before GO is removed
    [SerializeField] private bool  ragdollOnDeath = false; // enable if you have ragdoll

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;  // (current, max)
    public UnityEvent               OnDeath;

    // ── Private state ─────────────────────────────────────────────────────────
    private float        _currentHealth;
    private bool         _isDead;
    private AudioSource  _audio;
    private EnemyAI      _ai;

    public float CurrentHealth => _currentHealth;
    public bool  IsDead        => _isDead;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _currentHealth = Mathf.Clamp(startHealth, 0f, maxHealth);
        _audio         = GetComponent<AudioSource>();
        _ai            = GetComponent<EnemyAI>();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (_isDead || amount <= 0f) return;

        _currentHealth = Mathf.Max(_currentHealth - amount, 0f);
        PlaySound(hitSound);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        // Alert the AI that it has been hit (switch to chase state)
        _ai?.OnHit();

        if (_currentHealth <= 0f)
            Die();
    }

    // ── Death ─────────────────────────────────────────────────────────────────
    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        PlaySound(deathSound);
        OnDeath?.Invoke();

        // Disable AI
        if (_ai != null) _ai.enabled = false;

        // Disable colliders so the body doesn't block movement
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Optional: activate ragdoll (requires pre-built ragdoll in prefab)
        if (ragdollOnDeath)
            EnableRagdoll();

        // Trigger death animation if available
        if (TryGetComponent<Animator>(out var anim))
        {
            anim.SetTrigger("Die");
            anim.SetBool("IsDead", true);
        }

        Destroy(gameObject, destroyDelay);
    }

    private void EnableRagdoll()
    {
        // Re-enable all child Rigidbodies for ragdoll physics
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = false;

        // Disable the Animator so it doesn't fight the ragdoll
        if (TryGetComponent<Animator>(out var anim))
            anim.enabled = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }
}
