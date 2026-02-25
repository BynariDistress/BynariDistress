using UnityEngine;
using UnityEngine.UI;
using TMPro;            // remove if not using TextMeshPro

/// <summary>
/// Singleton HUD manager.
///
/// Attach to a Canvas (or a persistent manager GameObject).
///
/// All scripts call UIManager.Instance.XYZ() to update the HUD.
///
/// UI elements to create in the Canvas:
///  ┌─────────────────────────────────────────────────────────┐
///  │  [CROSSHAIR image – center of screen]                   │
///  │  [HEALTH BAR Slider – bottom left]                      │
///  │  [AMMO TEXT "24 / 120" – bottom right]                  │
///  │  [WEAPON NAME text – above ammo]                        │
///  │  [GRENADE COUNT text "GRN: 3" – bottom right area]      │
///  │  [RELOAD text "RELOADING..." – center bottom]           │
///  │  [HIT INDICATOR image – center, flashes red on damage]  │
///  │  [DEATH SCREEN panel – center, initially inactive]      │
///  │  [INTERACT PROMPT text – center slightly below middle]  │
///  └─────────────────────────────────────────────────────────┘
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // enable for persistent HUD across scenes
    }

    // ── Inspector references ──────────────────────────────────────────────────
    [Header("Ammo")]
    [SerializeField] private TMP_Text ammoText;         // "24 / 120"
    [SerializeField] private TMP_Text weaponNameText;   // "Rifle"
    [SerializeField] private TMP_Text grenadeText;      // "GRN: 3"

    [Header("Health")]
    [SerializeField] private Slider   healthSlider;
    [SerializeField] private TMP_Text healthText;       // optional numeric display

    [Header("Crosshair")]
    [SerializeField] private Image    crosshairImage;
    [SerializeField] private Image    crosshairDot;     // optional center dot

    [Header("Status")]
    [SerializeField] private TMP_Text reloadText;       // "RELOADING..."
    [SerializeField] private Image    hitIndicator;     // red flash on screen
    [SerializeField] private float    hitFlashDuration  = 0.15f;

    [Header("Panels")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject pauseScreen;

    // ── Private state ─────────────────────────────────────────────────────────
    private float _hitFlashTimer;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        // Initial state
        ShowReloadIndicator(false);
        if (hitIndicator   != null) hitIndicator.enabled   = false;
        if (deathScreen    != null) deathScreen.SetActive(false);
        if (pauseScreen    != null) pauseScreen.SetActive(false);
    }

    private void Update()
    {
        TickHitFlash();
    }

    // ── Ammo ──────────────────────────────────────────────────────────────────
    public void UpdateAmmoUI(int current, int reserve)
    {
        if (ammoText != null)
            ammoText.text = $"{current}  /  {reserve}";
    }

    public void UpdateWeaponName(string name)
    {
        if (weaponNameText != null)
            weaponNameText.text = name.ToUpper();
    }

    public void UpdateGrenadeCount(int count)
    {
        if (grenadeText != null)
            grenadeText.text = $"GRN: {count}";
    }

    // ── Health ────────────────────────────────────────────────────────────────
    public void UpdateHealth(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value    = current;
        }

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        // Flash hit indicator
        ShowHitFlash();
    }

    // ── Reload ────────────────────────────────────────────────────────────────
    public void ShowReloadIndicator(bool show)
    {
        if (reloadText != null) reloadText.enabled = show;
    }

    // ── Hit flash ─────────────────────────────────────────────────────────────
    public void ShowHitFlash()
    {
        if (hitIndicator == null) return;
        hitIndicator.enabled = true;
        _hitFlashTimer = hitFlashDuration;
    }

    private void TickHitFlash()
    {
        if (_hitFlashTimer <= 0f) return;
        _hitFlashTimer -= Time.deltaTime;

        if (hitIndicator != null)
        {
            float alpha = _hitFlashTimer / hitFlashDuration;
            Color c = hitIndicator.color;
            c.a = alpha * 0.5f;
            hitIndicator.color   = c;
            hitIndicator.enabled = _hitFlashTimer > 0f;
        }
    }

    // ── Crosshair ─────────────────────────────────────────────────────────────
    public void SetCrosshairVisible(bool visible)
    {
        if (crosshairImage != null) crosshairImage.enabled = visible;
        if (crosshairDot   != null) crosshairDot.enabled   = visible;
    }

    public void SetCrosshairColor(Color color)
    {
        if (crosshairImage != null) crosshairImage.color = color;
        if (crosshairDot   != null) crosshairDot.color   = color;
    }

    // ── Screens ───────────────────────────────────────────────────────────────
    public void ShowDeathScreen()
    {
        if (deathScreen != null) deathScreen.SetActive(true);
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void TogglePauseScreen(bool paused)
    {
        if (pauseScreen != null) pauseScreen.SetActive(paused);
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = paused;
    }
}
