using System.Collections;
using UnityEngine;

/// <summary>
/// Abstract base class for all weapons.
///
/// Subclass this for each weapon type (Pistol, Rifle, SniperRifle, etc.).
/// Handles:
///   • Raycasting-based hit detection
///   • Ammo management (clip + reserve)
///   • Fire-rate throttling
///   • Reload coroutine
///   • Recoil impulse on the camera
///   • Muzzle flash VFX / SFX hooks
///
/// Attach the concrete subclass to a weapon GameObject that is a child of
/// the player's camera/weapon holder.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    // ── Inspector – override defaults in each subclass ────────────────────────
    [Header("Weapon Stats")]
    [SerializeField] protected string weaponName    = "Weapon";
    [SerializeField] protected float  damage        = 20f;
    [SerializeField] protected float  fireRate      = 0.15f;   // seconds between shots
    [SerializeField] protected float  range         = 100f;
    [SerializeField] protected int    clipSize      = 30;
    [SerializeField] protected int    reserveAmmo   = 120;
    [SerializeField] protected float  reloadTime    = 2f;

    [Header("ADS")]
    [SerializeField] protected float  adsFOV              = 40f;   // camera FOV when aiming
    [SerializeField] protected float  adsTransitionSpeed  = 10f;
    [SerializeField] protected float  adsSensMultiplier   = 0.6f;  // reduce mouse sens

    [Header("Recoil")]
    [SerializeField] protected float  recoilAmount  = 0.5f;   // degrees kick per shot
    [SerializeField] protected float  recoilRecovery= 5f;

    [Header("References")]
    [SerializeField] protected Transform   muzzlePoint;        // empty child at barrel tip
    [SerializeField] protected GameObject  muzzleFlashPrefab;
    [SerializeField] protected AudioClip   fireSound;
    [SerializeField] protected AudioClip   reloadSound;
    [SerializeField] protected AudioClip   emptySound;
    [SerializeField] protected LayerMask   hitLayers = ~0;     // everything by default

    // ── Protected state ───────────────────────────────────────────────────────
    protected int   _currentAmmo;
    protected int   _currentReserve;
    protected bool  _isReloading;
    protected bool  _isADS;
    protected float _nextFireTime;
    protected float _defaultFOV   = 60f;

    protected Camera        _cam;
    protected AudioSource   _audio;
    protected MouseLook     _mouseLook;

    // ── Read-only properties (for UI) ─────────────────────────────────────────
    public string WeaponName    => weaponName;
    public int    CurrentAmmo   => _currentAmmo;
    public int    ReserveAmmo   => _currentReserve;
    public bool   IsReloading   => _isReloading;
    public bool   IsADS         => _isADS;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        _currentAmmo    = clipSize;
        _currentReserve = reserveAmmo;
        _cam            = Camera.main;
        _audio          = GetComponent<AudioSource>();

        // Walk up the hierarchy to find MouseLook
        _mouseLook = GetComponentInParent<MouseLook>();
    }

    protected virtual void OnEnable()
    {
        // Reset ADS state whenever weapon is switched to
        _isADS = false;
        if (_cam != null) _defaultFOV = _cam.fieldOfView;
    }

    protected virtual void Update()
    {
        HandleFiring();
        HandleADS();
        HandleReloadInput();
    }

    // ── Firing ────────────────────────────────────────────────────────────────
    private void HandleFiring()
    {
        if (Input.GetButton("Fire1") && CanFire())
            Fire();
    }

    protected bool CanFire() =>
        !_isReloading && Time.time >= _nextFireTime && _currentAmmo > 0;

    /// Override in subclasses to change firing behaviour (burst, shotgun spread, etc.)
    protected virtual void Fire()
    {
        _currentAmmo--;
        _nextFireTime = Time.time + fireRate;

        // SFX
        PlaySound(fireSound);

        // Muzzle flash
        SpawnMuzzleFlash();

        // Raycast
        PerformRaycast();

        // Recoil
        ApplyRecoil();

        // Notify UI
        UIManager.Instance?.UpdateAmmoUI(_currentAmmo, _currentReserve);
    }

    protected void PerformRaycast()
    {
        if (_cam == null) return;

        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
        {
            // ── Enemy damage ───────────────────────────────────────────────
            if (hit.collider.TryGetComponent<EnemyHealth>(out var enemy))
                enemy.TakeDamage(damage);

            // ── Destructible environment ───────────────────────────────────
            // Walk up the hierarchy: the collider may be on a chunk child whose
            // root holds the IDestructible (DestructibleHealth) component.
            var destructible = hit.collider.GetComponentInParent<IDestructible>()
                            ?? hit.collider.GetComponent<IDestructible>();
            if (destructible != null)
            {
                // Small impulse force for bullet-impact chunk push
                const float bulletImpactForce = 80f;
                destructible.ApplyDamage(damage, hit.point, hit.normal,
                                         bulletImpactForce);

                // Trigger surface VFX (dust chips, sparks) if DestructionVFX present
                var vfx = hit.collider.GetComponentInParent<DestructionVFX>();
                vfx?.PlayBulletImpact(hit.point, hit.normal);
            }

            // Impact debug ray
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
        }
    }

    // ── Empty-gun click ───────────────────────────────────────────────────────
    protected void HandleEmptyFire()
    {
        if (Input.GetButtonDown("Fire1") && !_isReloading && _currentAmmo == 0)
        {
            PlaySound(emptySound);
            // Auto-reload when clip is empty
            if (_currentReserve > 0)
                StartCoroutine(ReloadCoroutine());
        }
    }

    // ── Reload ────────────────────────────────────────────────────────────────
    private void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && !_isReloading && _currentAmmo < clipSize
            && _currentReserve > 0)
        {
            StartCoroutine(ReloadCoroutine());
        }
    }

    protected IEnumerator ReloadCoroutine()
    {
        _isReloading = true;
        UIManager.Instance?.ShowReloadIndicator(true);
        PlaySound(reloadSound);

        // Trigger reload animation if Animator is present
        if (TryGetComponent<Animator>(out var anim))
            anim.SetTrigger("Reload");

        yield return new WaitForSeconds(reloadTime);

        int needed  = clipSize - _currentAmmo;
        int refill  = Mathf.Min(needed, _currentReserve);
        _currentAmmo    += refill;
        _currentReserve -= refill;

        _isReloading = false;
        UIManager.Instance?.ShowReloadIndicator(false);
        UIManager.Instance?.UpdateAmmoUI(_currentAmmo, _currentReserve);
    }

    // ── ADS ───────────────────────────────────────────────────────────────────
    private void HandleADS()
    {
        _isADS = Input.GetButton("Fire2");   // RMB

        float targetFOV = _isADS ? adsFOV : _defaultFOV;
        if (_cam != null)
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV,
                                           adsTransitionSpeed * Time.deltaTime);

        // Adjust mouse sensitivity during ADS
        if (_mouseLook != null)
            _mouseLook.SetSensitivityMultiplier(_isADS ? adsSensMultiplier : 1f);
    }

    // ── Recoil ────────────────────────────────────────────────────────────────
    private void ApplyRecoil()
    {
        // Tilt the weapon model (this GameObject) upward
        transform.localRotation *= Quaternion.Euler(-recoilAmount, 0f, 0f);
        StartCoroutine(RecoverRecoil(transform.localRotation));
    }

    private IEnumerator RecoverRecoil(Quaternion kickedRot)
    {
        // Gradually return to identity
        while (Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.1f)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
                Quaternion.identity, recoilRecovery * Time.deltaTime);
            yield return null;
        }
        transform.localRotation = Quaternion.identity;
    }

    // ── VFX / SFX helpers ────────────────────────────────────────────────────
    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab == null || muzzlePoint == null) return;

        GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position,
                                       muzzlePoint.rotation, muzzlePoint);
        Destroy(flash, 0.05f);
    }

    protected void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// Add ammo to reserve (called by AmmoPickup).
    public void AddAmmo(int amount)
    {
        _currentReserve = Mathf.Min(_currentReserve + amount, reserveAmmo * 2);
        UIManager.Instance?.UpdateAmmoUI(_currentAmmo, _currentReserve);
    }
}
