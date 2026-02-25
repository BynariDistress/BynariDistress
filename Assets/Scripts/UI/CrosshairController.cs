using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dynamically resizes the crosshair spread based on movement, sprinting,
/// and ADS state.
///
/// Attach to the Crosshair RectTransform (the four-line or dot image group).
/// Assign <weaponManager> to find current weapon state.
/// </summary>
public class CrosshairController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Spread")]
    [SerializeField] private float idleSpread    = 20f;
    [SerializeField] private float moveSpread    = 45f;
    [SerializeField] private float sprintSpread  = 80f;
    [SerializeField] private float adsSpread     = 0f;    // hide while ADS

    [SerializeField] private float spreadLerp    = 8f;

    [Header("References")]
    [SerializeField] private RectTransform[] crosshairLines; // 4 lines (top/bot/left/right)
    [SerializeField] private WeaponManager   weaponManager;
    [SerializeField] private PlayerController player;

    // ── Private state ─────────────────────────────────────────────────────────
    private float[] _defaultOffsets;   // each line's base offset from center
    private float   _currentSpread;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (crosshairLines == null || crosshairLines.Length != 4)
            return;

        _defaultOffsets = new float[4];
        for (int i = 0; i < 4; i++)
            _defaultOffsets[i] = GetOffset(crosshairLines[i]);

        _currentSpread = idleSpread;
    }

    private void Update()
    {
        float target = CalculateTargetSpread();
        _currentSpread = Mathf.Lerp(_currentSpread, target, spreadLerp * Time.deltaTime);
        ApplySpread(_currentSpread);
    }

    // ── Spread calculation ────────────────────────────────────────────────────
    private float CalculateTargetSpread()
    {
        // ADS hides crosshair
        if (weaponManager != null && weaponManager.CurrentWeapon != null
            && weaponManager.CurrentWeapon.IsADS)
        {
            UIManager.Instance?.SetCrosshairVisible(false);
            return adsSpread;
        }

        UIManager.Instance?.SetCrosshairVisible(true);

        if (player == null) return idleSpread;

        if (player.IsSprinting)     return sprintSpread;
        if (player.CurrentSpeed > 0.1f) return moveSpread;

        return idleSpread;
    }

    // ── Apply spread to each line ─────────────────────────────────────────────
    private void ApplySpread(float spread)
    {
        if (crosshairLines == null) return;

        for (int i = 0; i < crosshairLines.Length; i++)
        {
            if (crosshairLines[i] == null) continue;

            float offset = (_defaultOffsets != null ? _defaultOffsets[i] : 0f) + spread;

            // Lines are laid out: 0=top, 1=bottom, 2=left, 3=right
            switch (i)
            {
                case 0: crosshairLines[i].anchoredPosition = new Vector2(0,  offset); break;
                case 1: crosshairLines[i].anchoredPosition = new Vector2(0, -offset); break;
                case 2: crosshairLines[i].anchoredPosition = new Vector2(-offset, 0); break;
                case 3: crosshairLines[i].anchoredPosition = new Vector2( offset, 0); break;
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    /// Get the dominant (largest absolute) offset component of a RectTransform.
    private float GetOffset(RectTransform rt)
    {
        Vector2 pos = rt.anchoredPosition;
        return Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.y));
    }
}
