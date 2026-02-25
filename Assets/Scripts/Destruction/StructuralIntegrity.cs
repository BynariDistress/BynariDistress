using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight joint-based structural support system.
///
/// Each destructible section that relies on physical support can reference
/// its supporting sections. When those supports are damaged past a threshold,
/// the dependent section starts receiving "gravity damage" – simulating the
/// structure pulling itself apart.
///
/// This is an alternative to DestructibleBuilding.cs (event-based).
/// Both can coexist: use DestructibleBuilding for scripted events and
/// StructuralIntegrity for continuous physics-like degradation.
///
/// ── Example ──────────────────────────────────────────────────────────────────
///   Wall_Top.StructuralIntegrity  →  supports[] = { Wall_Bottom }
///   If Wall_Bottom health drops below 30%, Wall_Top begins accumulating
///   gravity damage over time until it collapses.
/// </summary>
public class StructuralIntegrity : MonoBehaviour
{
    // ── Data ──────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class Support
    {
        public DestructibleHealth supportSection;
        [Tooltip("Below this health % the support is considered compromised.")]
        [Range(0f, 1f)] public float failureThreshold = 0.3f;
        [Tooltip("Weight of this support (1=single point of failure, 0.5=two supports needed).")]
        [Range(0f, 1f)] public float weight           = 1f;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("This section's supports")]
    [SerializeField] private Support[] supports;

    [Header("Damage-over-time when unsupported")]
    [SerializeField] private float gravityDamagePerSecond = 30f;
    [SerializeField] private float gravityDamageDelay     = 1.5f;  // s before crumbling starts

    [Header("Minimum integrity to sustain (0=fully unsupported collapses)")]
    [Range(0f, 1f)]
    [SerializeField] private float minSupportNeeded = 0.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private DestructibleHealth _myHealth;
    private float              _unsupportedTimer;
    private bool               _crumbling;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _myHealth = GetComponent<DestructibleHealth>();
        if (_myHealth == null)
            Debug.LogError("[StructuralIntegrity] Requires DestructibleHealth on same GO.");
    }

    private void Update()
    {
        if (_myHealth == null || _myHealth.IsDestroyed) return;

        float integrity = CalculateIntegrity();

        if (integrity < minSupportNeeded)
        {
            _unsupportedTimer += Time.deltaTime;

            if (_unsupportedTimer >= gravityDamageDelay && !_crumbling)
                _crumbling = true;

            if (_crumbling)
                ApplyGravityDamage();
        }
        else
        {
            // Supports are OK – reset timer
            _unsupportedTimer = 0f;
            _crumbling        = false;
        }
    }

    // ── Integrity calculation ─────────────────────────────────────────────────
    /// Returns 0..1 representing how well-supported this section currently is.
    private float CalculateIntegrity()
    {
        if (supports == null || supports.Length == 0)
            return 1f;   // no supports listed = self-standing

        float total = 0f;
        foreach (var s in supports)
        {
            if (s.supportSection == null) continue;
            if (s.supportSection.IsDestroyed) continue;

            float healthPct = s.supportSection.HealthPercent;
            float support   = healthPct >= s.failureThreshold ? s.weight : 0f;
            total += support;
        }

        return Mathf.Clamp01(total);
    }

    // ── Gravity damage ────────────────────────────────────────────────────────
    private void ApplyGravityDamage()
    {
        float damage = gravityDamagePerSecond * Time.deltaTime;
        _myHealth.ApplyDamage(damage, transform.position, Vector3.down, 0f);
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (supports == null) return;
        Gizmos.color = Color.blue;
        foreach (var s in supports)
        {
            if (s.supportSection == null) continue;
            Gizmos.DrawLine(transform.position, s.supportSection.transform.position);
        }
    }
}
