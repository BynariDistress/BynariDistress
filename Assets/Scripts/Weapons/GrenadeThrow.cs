using System.Collections;
using UnityEngine;

/// <summary>
/// Handles grenade inventory and throwing on G key press.
///
/// Attach to the Player GameObject.
/// Assign <grenadePrefab> to a prefab that has Grenade.cs + Rigidbody.
/// Assign <throwOrigin> to an empty child at roughly the player's hand position,
/// or simply use the Camera transform.
/// </summary>
public class GrenadeThrow : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Grenade")]
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private Transform  throwOrigin;   // where the grenade spawns

    [Header("Throw")]
    [SerializeField] private float throwForce      = 15f;
    [SerializeField] private float throwUpward     = 3f;   // adds upward arc
    [SerializeField] private int   startingGrenades= 3;

    [Header("Cooldown")]
    [SerializeField] private float throwCooldown   = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip throwSound;

    // ── Private state ─────────────────────────────────────────────────────────
    private int         _grenadeCount;
    private float       _nextThrowTime;
    private AudioSource _audio;

    public int GrenadeCount => _grenadeCount;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _grenadeCount = startingGrenades;
        _audio        = GetComponent<AudioSource>();

        if (throwOrigin == null)
            throwOrigin = Camera.main?.transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            TryThrow();
    }

    // ── Throw ─────────────────────────────────────────────────────────────────
    private void TryThrow()
    {
        if (_grenadeCount <= 0)
        {
            Debug.Log("[GrenadeThrow] No grenades left.");
            return;
        }

        if (Time.time < _nextThrowTime) return;

        _grenadeCount--;
        _nextThrowTime = Time.time + throwCooldown;

        if (_audio != null && throwSound != null)
            _audio.PlayOneShot(throwSound);

        ThrowGrenade();
        UIManager.Instance?.UpdateGrenadeCount(_grenadeCount);
    }

    private void ThrowGrenade()
    {
        if (grenadePrefab == null || throwOrigin == null) return;

        GameObject g = Instantiate(grenadePrefab, throwOrigin.position,
                                   throwOrigin.rotation);

        if (g.TryGetComponent<Rigidbody>(out var rb))
        {
            Vector3 throwDir = throwOrigin.forward + throwOrigin.up * throwUpward;
            rb.AddForce(throwDir.normalized * throwForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);  // spin
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void AddGrenade(int amount = 1)
    {
        _grenadeCount += amount;
        UIManager.Instance?.UpdateGrenadeCount(_grenadeCount);
    }
}
