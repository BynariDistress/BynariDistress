using UnityEngine;

/// <summary>
/// Pistol – semi-automatic, moderate damage, small clip.
///
/// Inspector defaults (set in Prefab):
///   damage      = 35
///   fireRate    = 0.25  (one shot per 0.25 s = 4 RPS)
///   range       = 50
///   clipSize    = 12
///   reserveAmmo = 48
///   reloadTime  = 1.5
///   adsFOV      = 50
/// </summary>
public class Pistol : WeaponBase
{
    // No extra fields needed – all configured via inspector on the base class.

    protected override void Awake()
    {
        // Set default stats for pistol before base initialises ammo counters
        if (damage      == 20f)  damage      = 35f;
        if (fireRate     == 0.15f) fireRate   = 0.25f;
        if (range        == 100f) range       = 50f;
        if (clipSize     == 30)  clipSize     = 12;
        if (reserveAmmo  == 120) reserveAmmo  = 48;
        if (reloadTime   == 2f)  reloadTime   = 1.5f;
        if (adsFOV       == 40f) adsFOV       = 50f;
        if (weaponName   == "Weapon") weaponName = "Pistol";

        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
        HandleEmptyFire();  // click sound + auto-reload when empty
    }

    /// Pistol is semi-automatic: only fires on button DOWN, not held.
    protected override void Fire()
    {
        // GetButton would make it automatic – pistol uses GetButtonDown
        if (!Input.GetButtonDown("Fire1")) return;
        base.Fire();
    }
}
