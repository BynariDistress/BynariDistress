using System.Collections;
using UnityEngine;

/// <summary>
/// Grenade physics object.
///
/// Attach to the grenade prefab.
/// The prefab needs:
///   • Rigidbody
///   • Collider (sphere or capsule)
///   • (optional) AudioSource
///   • (optional) ParticleSystem children: sparksPrefab, explosionPrefab
///
/// Workflow:
///   GrenadeThrow.cs spawns the prefab, adds a throw impulse, and this script
///   handles the countdown and explosion.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Explosion")]
    [SerializeField] private float fuseDuration    = 3f;
    [SerializeField] private float blastRadius     = 6f;
    [SerializeField] private float blastDamage     = 80f;
    [SerializeField] private float blastForce      = 700f;   // physics push
    [SerializeField] private LayerMask damageableLayers = ~0;

    [Header("VFX / SFX")]
    [SerializeField] private GameObject explosionPrefab;  // particle system prefab
    [SerializeField] private AudioClip  fuseTickSound;
    [SerializeField] private AudioClip  explosionSound;

    // ── Private state ─────────────────────────────────────────────────────────
    private bool        _hasExploded;
    private AudioSource _audio;
    private Rigidbody   _rb;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _rb    = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        StartCoroutine(FuseCountdown());
    }

    // ── Fuse ──────────────────────────────────────────────────────────────────
    private IEnumerator FuseCountdown()
    {
        if (_audio != null && fuseTickSound != null)
            _audio.PlayOneShot(fuseTickSound);

        yield return new WaitForSeconds(fuseDuration);
        Explode();
    }

    // ── Explosion ─────────────────────────────────────────────────────────────
    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // VFX
        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, transform.position,
                                         Quaternion.identity);
            Destroy(fx, 3f);
        }

        // SFX
        if (_audio != null && explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        // Damage & force
        Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius,
                                                 damageableLayers);

        // Track which IDestructible roots we've already notified this frame so
        // multiple child colliders on the same wall don't stack damage.
        var damagedDestructibles = new System.Collections.Generic.HashSet<IDestructible>();

        foreach (Collider col in hits)
        {
            // Apply physics force to free rigidbodies (already-released debris etc.)
            if (col.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddExplosionForce(blastForce, transform.position, blastRadius,
                                     1f, ForceMode.Impulse);
            }

            // Damage enemies
            if (col.TryGetComponent<EnemyHealth>(out var enemy))
            {
                float dist    = Vector3.Distance(transform.position, col.bounds.center);
                float falloff = 1f - Mathf.Clamp01(dist / blastRadius); // linear falloff
                enemy.TakeDamage(blastDamage * falloff);
            }

            // Damage player (friendly fire)
            if (col.TryGetComponent<PlayerHealth>(out var player))
            {
                float dist    = Vector3.Distance(transform.position, col.bounds.center);
                float falloff = 1f - Mathf.Clamp01(dist / blastRadius);
                player.TakeDamage(blastDamage * falloff);
            }

            // ── Destructible environment ───────────────────────────────────
            var destructible = col.GetComponentInParent<IDestructible>()
                            ?? col.GetComponent<IDestructible>();
            if (destructible != null && damagedDestructibles.Add(destructible))
            {
                // ApplyExplosionDamage handles distance falloff internally
                destructible.ApplyExplosionDamage(blastDamage, transform.position,
                                                  blastRadius, blastForce);
            }
        }

        Destroy(gameObject);
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawSphere(transform.position, blastRadius);
    }
}
