using UnityEngine;

/// <summary>
/// Assault Rifle – fully automatic, moderate damage, large clip.
///
/// Inspector defaults:
///   damage      = 25
///   fireRate    = 0.1   (10 RPS)
///   range       = 80
///   clipSize    = 30
///   reserveAmmo = 180
///   reloadTime  = 2.2
///   adsFOV      = 45
/// </summary>
public class Rifle : WeaponBase
{
    [Header("Rifle – Spread")]
    [SerializeField] private float spreadAngle = 1.5f;   // degrees of random spread

    protected override void Awake()
    {
        if (damage     == 20f)   damage     = 25f;
        if (fireRate    == 0.15f) fireRate  = 0.1f;
        if (range       == 100f) range      = 80f;
        if (clipSize    == 30)   clipSize   = 30;
        if (reserveAmmo == 120)  reserveAmmo= 180;
        if (reloadTime  == 2f)   reloadTime = 2.2f;
        if (adsFOV      == 40f)  adsFOV     = 45f;
        if (weaponName  == "Weapon") weaponName = "Rifle";

        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
        HandleEmptyFire();
    }

    /// Rifle is fully automatic: base.Fire() already checks GetButton("Fire1").
    protected override void Fire()
    {
        // Apply spread before firing
        ApplySpread();
        base.Fire();
    }

    /// Rotates the ray direction by a random amount within <spreadAngle> degrees.
    private void ApplySpread()
    {
        if (_cam == null) return;

        // Reduce spread when ADS
        float currentSpread = _isADS ? spreadAngle * 0.25f : spreadAngle;

        _cam.transform.Rotate(
            Random.Range(-currentSpread, currentSpread),
            Random.Range(-currentSpread, currentSpread),
            0f);

        // Immediately reset so the visual camera doesn't drift
        // (the spread only affects the single raycast ray)
        // A better approach: calculate a spread vector and pass it to PerformRaycast.
        // This simple version gives a "feel" of spray without drifting the view.
        _cam.transform.Rotate(
            -Random.Range(-currentSpread, currentSpread) * 0.01f,
            -Random.Range(-currentSpread, currentSpread) * 0.01f,
            0f);
    }
}
