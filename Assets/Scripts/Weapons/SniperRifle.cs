using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sniper Rifle – bolt-action, high damage, extreme range, tight scope.
///
/// Inspector defaults:
///   damage      = 150
///   fireRate    = 1.5   (bolt-action delay)
///   range       = 500
///   clipSize    = 5
///   reserveAmmo = 20
///   reloadTime  = 3.0
///   adsFOV      = 12    (tight scope view)
///
/// Assign <scopeOverlay> to a full-screen RawImage scope texture in the UI
/// (enabled only while ADS).
/// </summary>
public class SniperRifle : WeaponBase
{
    [Header("Sniper – Scope")]
    [SerializeField] private GameObject scopeOverlay;   // UI panel with scope reticle
    [SerializeField] private GameObject weaponModel;    // hide weapon model while scoped

    [Header("Sniper – Sway")]
    [SerializeField] private float swayAmount    = 0.02f;
    [SerializeField] private float swaySmoothing = 4f;

    private Quaternion _targetSway;

    protected override void Awake()
    {
        if (damage     == 20f)    damage     = 150f;
        if (fireRate    == 0.15f)  fireRate  = 1.5f;
        if (range       == 100f)  range      = 500f;
        if (clipSize    == 30)    clipSize   = 5;
        if (reserveAmmo == 120)   reserveAmmo= 20;
        if (reloadTime  == 2f)    reloadTime = 3f;
        if (adsFOV      == 40f)   adsFOV    = 12f;
        if (weaponName  == "Weapon") weaponName = "Sniper Rifle";

        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetScopeVisible(false);
    }

    private void OnDisable()
    {
        SetScopeVisible(false);
    }

    protected override void Update()
    {
        base.Update();
        HandleEmptyFire();
        HandleScopeVisual();
        HandleWeaponSway();
    }

    /// Sniper is semi-automatic (bolt-action feel).
    protected override void Fire()
    {
        if (!Input.GetButtonDown("Fire1")) return;
        base.Fire();
        StartCoroutine(BoltAction());
    }

    /// Simulate bolt-action: briefly disable firing during the cycle.
    private IEnumerator BoltAction()
    {
        if (TryGetComponent<Animator>(out var anim))
            anim.SetTrigger("BoltAction");

        yield return new WaitForSeconds(0.3f);
        // The fire rate timer in the base already handles the delay.
    }

    // ── Scope visual toggle ───────────────────────────────────────────────────
    private void HandleScopeVisual()
    {
        bool nowADS = Input.GetButton("Fire2");
        if (nowADS != _isADS)  // state changed
            SetScopeVisible(nowADS);
    }

    private void SetScopeVisible(bool show)
    {
        if (scopeOverlay != null)  scopeOverlay.SetActive(show);
        if (weaponModel  != null)  weaponModel.SetActive(!show);
    }

    // ── Weapon sway ───────────────────────────────────────────────────────────
    private void HandleWeaponSway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _targetSway = Quaternion.Euler(
            -mouseY * swayAmount,
             mouseX * swayAmount,
             0f);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            _targetSway,
            swaySmoothing * Time.deltaTime);
    }
}
