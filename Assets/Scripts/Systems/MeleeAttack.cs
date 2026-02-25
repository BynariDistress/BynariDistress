using System.Collections;
using UnityEngine;

/// <summary>
/// Melee attack on Q key press.
///
/// Performs a short-range sphere-cast (or overlap sphere) to detect enemies
/// and applies damage. Optionally triggers an Animator "Melee" trigger for
/// knife / fist animations.
///
/// Attach to the Player GameObject.
/// </summary>
public class MeleeAttack : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Attack")]
    [SerializeField] private float meleeDamage    = 40f;
    [SerializeField] private float meleeRange     = 1.8f;
    [SerializeField] private float meleeRadius    = 0.6f;  // hit-sphere size
    [SerializeField] private float attackCooldown = 0.5f;  // seconds between attacks
    [SerializeField] private LayerMask hitLayers  = ~0;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;   // small spark VFX

    [Header("Audio")]
    [SerializeField] private AudioClip swingSound;
    [SerializeField] private AudioClip hitSound;

    [Header("Animation")]
    [SerializeField] private Animator weaponAnimator;   // assign knife/fist animator

    // ── Private state ─────────────────────────────────────────────────────────
    private float       _nextAttackTime;
    private Camera      _cam;
    private AudioSource _audio;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _cam   = Camera.main;
        _audio = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= _nextAttackTime)
            PerformMelee();
    }

    // ── Attack ────────────────────────────────────────────────────────────────
    private void PerformMelee()
    {
        _nextAttackTime = Time.time + attackCooldown;

        PlaySound(swingSound);
        TriggerAnimation("Melee");

        // Short delay to match animation wind-up before checking hits
        StartCoroutine(HitCheck(0.15f));
    }

    private IEnumerator HitCheck(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_cam == null) yield break;

        Vector3 origin = _cam.transform.position + _cam.transform.forward * (meleeRange * 0.5f);

        Collider[] hits = Physics.OverlapSphere(origin, meleeRadius, hitLayers);
        bool hitSomething = false;

        foreach (Collider col in hits)
        {
            // Don't hit ourselves
            if (col.transform.IsChildOf(transform) || col.transform == transform)
                continue;

            // Damage enemies
            if (col.TryGetComponent<EnemyHealth>(out var enemy))
            {
                enemy.TakeDamage(meleeDamage);
                hitSomething = true;
            }

            // Spawn hit VFX at closest point
            if (hitEffectPrefab != null)
            {
                Vector3 hitPos = col.ClosestPoint(origin);
                GameObject fx  = Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                Destroy(fx, 1f);
            }
        }

        if (hitSomething)
            PlaySound(hitSound);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void TriggerAnimation(string triggerName)
    {
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger(triggerName);
    }

    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;
        Gizmos.color = Color.yellow;
        Vector3 origin = Camera.main.transform.position +
                         Camera.main.transform.forward * (meleeRange * 0.5f);
        Gizmos.DrawWireSphere(origin, meleeRadius);
    }
}
