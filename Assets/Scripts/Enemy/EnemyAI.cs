using System.Collections;
using UnityEngine;
using UnityEngine.AI;   // NavMesh – install via Window > AI > Navigation

/// <summary>
/// Simple three-state enemy AI: Patrol → Chase → Attack.
///
/// Requires:
///  • NavMeshAgent component on the same GameObject
///  • A baked NavMesh in the scene (Window > AI > Navigation > Bake)
///  • EnemyHealth component on the same GameObject
///
/// Inspector setup:
///  • Drag patrol waypoints into <patrolPoints> (optional).
///  • Adjust detection/attack ranges and timings.
///
/// States:
///  Patrol  – walk between waypoints (or idle if none set)
///  Chase   – run toward the player
///  Attack  – stop and shoot/swing when within attackRange
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    // ── State machine ─────────────────────────────────────────────────────────
    private enum AIState { Patrol, Chase, Attack }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Detection")]
    [SerializeField] private float sightRange    = 15f;
    [SerializeField] private float attackRange   = 3f;
    [SerializeField] private float sightAngle    = 110f;  // FOV half-angle (total = 2×)
    [SerializeField] private LayerMask sightMask = ~0;    // layers blocking LOS

    [Header("Speed")]
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float chaseSpeed  = 5f;

    [Header("Attack")]
    [SerializeField] private float attackDamage    = 15f;
    [SerializeField] private float attackCooldown  = 1.5f;
    [SerializeField] private float attackAnimDelay = 0.3f; // hit lands after this delay

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float       patrolWaitTime = 2f;   // pause at each waypoint

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip alertSound;

    // ── Private state ─────────────────────────────────────────────────────────
    private NavMeshAgent _agent;
    private Animator     _anim;
    private AudioSource  _audio;
    private Transform    _player;
    private PlayerHealth _playerHealth;
    private AIState      _state          = AIState.Patrol;
    private int          _patrolIndex;
    private float        _nextAttackTime;
    private bool         _isWaiting;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim  = GetComponent<Animator>();
        _audio = GetComponent<AudioSource>();

        // Find player by tag
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            _player       = playerGO.transform;
            _playerHealth = playerGO.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogWarning("[EnemyAI] No GameObject tagged 'Player' found.");
        }
    }

    private void Update()
    {
        if (_player == null) return;

        switch (_state)
        {
            case AIState.Patrol: UpdatePatrol(); break;
            case AIState.Chase:  UpdateChase();  break;
            case AIState.Attack: UpdateAttack(); break;
        }

        UpdateAnimator();
    }

    // ── Patrol ────────────────────────────────────────────────────────────────
    private void UpdatePatrol()
    {
        _agent.speed = patrolSpeed;

        if (CanSeePlayer())
        {
            TransitionTo(AIState.Chase);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            // No waypoints: just stand idle
            _agent.ResetPath();
            return;
        }

        if (!_isWaiting && (!_agent.hasPath || _agent.remainingDistance < 0.5f))
            StartCoroutine(PatrolWait());
    }

    private IEnumerator PatrolWait()
    {
        _isWaiting = true;
        _agent.ResetPath();
        yield return new WaitForSeconds(patrolWaitTime);

        _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
        if (patrolPoints[_patrolIndex] != null)
            _agent.SetDestination(patrolPoints[_patrolIndex].position);

        _isWaiting = false;
    }

    // ── Chase ─────────────────────────────────────────────────────────────────
    private void UpdateChase()
    {
        _agent.speed = chaseSpeed;
        _agent.SetDestination(_player.position);

        float dist = DistToPlayer();

        if (dist <= attackRange)
        {
            TransitionTo(AIState.Attack);
        }
        else if (dist > sightRange * 1.5f)  // lost the player
        {
            TransitionTo(AIState.Patrol);
        }
    }

    // ── Attack ────────────────────────────────────────────────────────────────
    private void UpdateAttack()
    {
        // Face the player
        Vector3 lookDir = (_player.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);

        _agent.ResetPath();

        // Re-enter chase if player backed away
        if (DistToPlayer() > attackRange * 1.3f)
        {
            TransitionTo(AIState.Chase);
            return;
        }

        // Attack on cooldown
        if (Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + attackCooldown;
            StartCoroutine(ExecuteAttack());
        }
    }

    private IEnumerator ExecuteAttack()
    {
        PlaySound(attackSound);
        if (_anim != null) _anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackAnimDelay);

        // Check still in range before applying damage
        if (DistToPlayer() <= attackRange * 1.2f && _playerHealth != null)
            _playerHealth.TakeDamage(attackDamage);
    }

    // ── Perception ────────────────────────────────────────────────────────────
    private bool CanSeePlayer()
    {
        if (_player == null) return false;
        float dist = DistToPlayer();
        if (dist > sightRange) return false;

        // Angle check
        Vector3 dirToPlayer = (_player.position - transform.position).normalized;
        float   angle       = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > sightAngle) return false;

        // Line-of-sight check
        if (Physics.Raycast(EyePosition(), dirToPlayer, dist, sightMask))
            return false;   // something is blocking

        return true;
    }

    private Vector3 EyePosition() =>
        transform.position + Vector3.up * 1.6f;   // approximate eye height

    private float DistToPlayer() =>
        Vector3.Distance(transform.position, _player.position);

    // ── State transitions ─────────────────────────────────────────────────────
    private void TransitionTo(AIState next)
    {
        if (next == _state) return;

        // Entry actions
        if (next == AIState.Chase)
            PlaySound(alertSound);

        _state = next;
    }

    /// Called by EnemyHealth when this enemy takes a hit.
    public void OnHit()
    {
        if (_state == AIState.Patrol)
            TransitionTo(AIState.Chase);
    }

    // ── Animator sync ─────────────────────────────────────────────────────────
    private void UpdateAnimator()
    {
        if (_anim == null) return;
        _anim.SetFloat("Speed", _agent.velocity.magnitude);
        _anim.SetBool("IsChasing", _state == AIState.Chase || _state == AIState.Attack);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Sight range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // FOV arc (cyan)
        Gizmos.color = Color.cyan;
        Vector3 leftDir  = Quaternion.Euler(0f, -sightAngle, 0f) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0f,  sightAngle, 0f) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir  * sightRange);
        Gizmos.DrawRay(transform.position, rightDir * sightRange);
    }
}
