using UnityEngine;

/// <summary>
/// Manages equipping, switching, and tracking the active weapon.
///
/// Setup:
///  1. Create a "WeaponHolder" empty child of the camera.
///  2. Add each weapon prefab as a child of WeaponHolder (disabled by default).
///  3. Drag all weapon GameObjects into the <weapons> array (inspector order
///     determines the cycle order).
///
/// Controls:
///  • Scroll wheel (middle mouse) – cycle through weapons
///  • Number keys 1–9            – direct slot select
/// </summary>
public class WeaponManager : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Weapons")]
    [SerializeField] private WeaponBase[] weapons;          // assign all weapon GOs
    [SerializeField] private int          startingIndex = 0;

    [Header("Scroll Sensitivity")]
    [SerializeField] private float scrollThreshold = 0.1f;

    // ── Private state ─────────────────────────────────────────────────────────
    private int _currentIndex = -1;

    // ── Public read-only ──────────────────────────────────────────────────────
    public WeaponBase CurrentWeapon =>
        (_currentIndex >= 0 && _currentIndex < weapons.Length)
            ? weapons[_currentIndex]
            : null;

    public int CurrentIndex => _currentIndex;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        // Disable all weapons first
        foreach (var w in weapons)
            if (w != null) w.gameObject.SetActive(false);

        EquipWeapon(startingIndex);
    }

    private void Update()
    {
        HandleScrollSwitch();
        HandleNumberKeys();
    }

    // ── Switch logic ──────────────────────────────────────────────────────────
    private void HandleScrollSwitch()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < scrollThreshold) return;

        // Scroll up = previous, scroll down = next  (reverse to taste)
        int direction = scroll > 0f ? -1 : 1;
        CycleWeapon(direction);
    }

    private void HandleNumberKeys()
    {
        for (int i = 0; i < Mathf.Min(weapons.Length, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                EquipWeapon(i);
                return;
            }
        }
    }

    private void CycleWeapon(int direction)
    {
        if (weapons.Length == 0) return;
        int next = (_currentIndex + direction + weapons.Length) % weapons.Length;
        EquipWeapon(next);
    }

    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length) return;
        if (index == _currentIndex) return;
        if (weapons[index] == null) return;

        // Deactivate current weapon
        if (_currentIndex >= 0 && weapons[_currentIndex] != null)
            weapons[_currentIndex].gameObject.SetActive(false);

        _currentIndex = index;
        weapons[_currentIndex].gameObject.SetActive(true);

        // Update HUD
        UIManager.Instance?.UpdateWeaponName(weapons[_currentIndex].WeaponName);
        UIManager.Instance?.UpdateAmmoUI(
            weapons[_currentIndex].CurrentAmmo,
            weapons[_currentIndex].ReserveAmmo);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// Add ammo to the currently held weapon (called by AmmoPickup).
    public void AddAmmoToCurrent(int amount)
    {
        CurrentWeapon?.AddAmmo(amount);
    }
}
