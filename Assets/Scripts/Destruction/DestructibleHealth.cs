using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Core health and damage-stage component for all destructible environment objects.
///
/// ── How It Works ─────────────────────────────────────────────────────────────
///  1. Object starts Intact with full health.
///  2. Each hit calls ApplyDamage / ApplyExplosionDamage (IDestructible).
///  3. When health crosses a stage threshold the object:
///       a) Swaps to a cracked/damaged material on its renderer.
///       b) Tells FractureController to release chunks near the hit point.
///  4. At Destroyed the FractureController collapses everything.
///
/// ── Prefab Setup ─────────────────────────────────────────────────────────────
///  • Add this component (and FractureController) to a wall / crate / building.
///  • Assign stage materials (intact, damaged, critical, structural).
///  • The object automatically tags itself so bullets can find it.
///
/// ── Integration ──────────────────────────────────────────────────────────────
///  Weapons call:
///    hit.collider.GetComponentInParent<IDestructible>()?.ApplyDamage(...)
///  Grenade calls:
///    col.GetComponentInParent<IDestructible>()?.ApplyExplosionDamage(...)
/// </summary>
[RequireComponent(typeof(FractureController))]
public class DestructibleHealth : MonoBehaviour, IDestructible
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private float maxHealth   = 500f;
    [SerializeField] private float startHealth = 500f;

    [Header("Stage Thresholds (% of max health)")]
    [SerializeField] [Range(0f, 1f)] private float damagedAt    = 0.75f;
    [SerializeField] [Range(0f, 1f)] private float criticalAt   = 0.50f;
    [SerializeField] [Range(0f, 1f)] private float structuralAt = 0.25f;

    [Header("Stage Materials (URP Lit)")]
    [Tooltip("Applied to ALL MeshRenderers on this object at each stage.")]
    [SerializeField] private Material matIntact;
    [SerializeField] private Material matDamaged;
    [SerializeField] private Material matCritical;
    [SerializeField] private Material matStructural;

    [Header("Chunk Release Radii per Stage")]
    [Tooltip("Radius around hit point within which chunks are released per stage.")]
    [SerializeField] private float chunkRadiusDamaged    = 0.6f;
    [SerializeField] private float chunkRadiusCritical   = 1.2f;
    [SerializeField] private float chunkRadiusStructural = 2.0f;

    [Header("Armour / Resistance")]
    [Tooltip("Bullets do reduced damage to heavy materials (e.g. concrete).")]
    [SerializeField] private float bulletResistance    = 1.0f;   // 1 = no resistance
    [SerializeField] private float explosionResistance = 1.0f;

    [Header("Events")]
    public UnityEvent<DestructionStage> OnStageChanged;
    public UnityEvent                   OnDestroyed;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip collapseSound;

    // ── Private state ─────────────────────────────────────────────────────────
    private float            _currentHealth;
    private DestructionStage _stage = DestructionStage.Intact;
    private FractureController _fracture;
    private Renderer[]       _renderers;
    private AudioSource      _audio;

    public float            CurrentHealth => _currentHealth;
    public float            MaxHealth     => maxHealth;
    public DestructionStage Stage         => _stage;
    public bool             IsDestroyed   => _stage == DestructionStage.Destroyed;
    public float            HealthPercent => _currentHealth / maxHealth;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _fracture      = GetComponent<FractureController>();
        _renderers     = GetComponentsInChildren<Renderer>();
        _audio         = GetComponent<AudioSource>();
        _currentHealth = Mathf.Clamp(startHealth, 0f, maxHealth);

        ApplyMaterialForStage(DestructionStage.Intact);
    }

    // ── IDestructible ─────────────────────────────────────────────────────────
    public void ApplyDamage(float damage, Vector3 hitPoint, Vector3 hitNormal,
                            float impactForce = 100f)
    {
        if (IsDestroyed) return;

        float adjusted = damage * bulletResistance;
        TakeDamage(adjusted, hitPoint, impactForce);
        PlaySound(hitSound);
    }

    public void ApplyExplosionDamage(float maxDamage, Vector3 explosionCenter,
                                     float radius, float force)
    {
        if (IsDestroyed) return;

        // Falloff: full damage at center, zero at edge
        float dist    = Vector3.Distance(transform.position, explosionCenter);
        float falloff = 1f - Mathf.Clamp01(dist / radius);
        float adjusted = maxDamage * falloff * explosionResistance;

        if (adjusted <= 0f) return;

        TakeDamage(adjusted, explosionCenter, force);
    }

    // ── Damage processing ─────────────────────────────────────────────────────
    private void TakeDamage(float amount, Vector3 sourcePos, float force)
    {
        _currentHealth = Mathf.Max(_currentHealth - amount, 0f);
        CheckStageTransition(sourcePos, force);

        // Notify debug health bar (if present)
        GetComponent<DestructibleHealthBar>()?.Refresh();
    }

    private void CheckStageTransition(Vector3 sourcePos, float force)
    {
        DestructionStage newStage = CalculateStage();
        if (newStage <= _stage) return;   // already at or past this stage

        // Advance through all crossed stages (e.g. massive explosion skips stages)
        while (_stage < newStage)
        {
            _stage++;
            ApplyStage(_stage, sourcePos, force);
        }
    }

    private DestructionStage CalculateStage()
    {
        float pct = HealthPercent;
        if (pct <= 0f)          return DestructionStage.Destroyed;
        if (pct <= structuralAt) return DestructionStage.Structural;
        if (pct <= criticalAt)   return DestructionStage.Critical;
        if (pct <= damagedAt)    return DestructionStage.Damaged;
        return DestructionStage.Intact;
    }

    private void ApplyStage(DestructionStage stage, Vector3 sourcePos, float force)
    {
        ApplyMaterialForStage(stage);
        OnStageChanged?.Invoke(stage);

        // Notify DestructionManager for global tracking
        DestructionManager.Instance?.OnStructureDamaged(this, stage);

        switch (stage)
        {
            case DestructionStage.Damaged:
                _fracture.ReleaseChunksNear(sourcePos, chunkRadiusDamaged, force * 0.3f);
                break;

            case DestructionStage.Critical:
                _fracture.ReleaseChunksNear(sourcePos, chunkRadiusCritical, force * 0.6f);
                break;

            case DestructionStage.Structural:
                _fracture.ReleaseChunksNear(sourcePos, chunkRadiusStructural, force * 0.8f);
                break;

            case DestructionStage.Destroyed:
                _fracture.CollapseAll(sourcePos, force);
                PlaySound(collapseSound);
                OnDestroyed?.Invoke();
                break;
        }
    }

    // ── Materials ─────────────────────────────────────────────────────────────
    private void ApplyMaterialForStage(DestructionStage stage)
    {
        Material mat = stage switch
        {
            DestructionStage.Damaged    => matDamaged,
            DestructionStage.Critical   => matCritical,
            DestructionStage.Structural => matStructural,
            _                           => matIntact,
        };

        if (mat == null) return;

        foreach (var r in _renderers)
        {
            // Only update the whole-object renderers, not chunk renderers
            // Chunks manage their own materials via DebrisObject
            if (r.GetComponent<DebrisObject>() != null) continue;
            r.sharedMaterial = mat;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Visualise chunk-release radii at each stage
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chunkRadiusDamaged);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chunkRadiusCritical);
        Gizmos.color = new Color(1f, 0.3f, 0f);
        Gizmos.DrawWireSphere(transform.position, chunkRadiusStructural);
    }
}
