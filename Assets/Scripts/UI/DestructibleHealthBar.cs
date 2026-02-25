using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-space health bar for debug/development visualization.
///
/// Shows above a destructible object in the scene view while Play Mode is active.
/// Hides automatically in Release builds (or disable via the inspector).
///
/// Setup:
///  1. Add this to the same GameObject as DestructibleHealth.
///  2. Assign <healthBarPrefab> – a prefab with a World Space Canvas containing
///     a Slider and optional TMP_Text label.
///     OR leave it null and the bar is created procedurally (simple).
///
/// The bar rotates to face the camera each frame (billboarding).
/// </summary>
[RequireComponent(typeof(DestructibleHealth))]
public class DestructibleHealthBar : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Bar Settings")]
    [SerializeField] private Vector3 offset          = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private Vector3 scale           = new Vector3(1.5f, 0.25f, 1f);
    [SerializeField] private bool    showInRelease   = false;   // hide in non-debug builds
    [SerializeField] private bool    hideWhenFull    = true;    // only show when damaged

    [Header("Colors")]
    [SerializeField] private Color colorFull      = Color.green;
    [SerializeField] private Color colorDamaged   = Color.yellow;
    [SerializeField] private Color colorCritical  = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color colorDestroyed = Color.red;

    // ── Private state ─────────────────────────────────────────────────────────
    private DestructibleHealth _health;
    private Transform          _barRoot;
    private Image              _fillImage;
    private TMP_Text           _labelText;
    private Camera             _cam;
    private bool               _initialised;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _health = GetComponent<DestructibleHealth>();
        _cam    = Camera.main;

#if !UNITY_EDITOR
        if (!showInRelease)
        {
            enabled = false;
            return;
        }
#endif
        BuildBar();
    }

    private void LateUpdate()
    {
        if (!_initialised) return;

        // Billboard: face camera
        if (_cam != null)
            _barRoot.LookAt(_cam.transform);

        // Toggle visibility
        bool show = !hideWhenFull || _health.HealthPercent < 0.99f;
        _barRoot.gameObject.SetActive(show && !_health.IsDestroyed);
    }

    /// Called by DestructibleHealth after every damage tick.
    public void Refresh()
    {
        if (!_initialised) return;
        UpdateFill();
    }

    // ── Bar construction ──────────────────────────────────────────────────────
    private void BuildBar()
    {
        // Root transform
        _barRoot = new GameObject("_HealthBar").transform;
        _barRoot.SetParent(transform);
        _barRoot.localPosition = offset;
        _barRoot.localScale    = scale;

        // World-space canvas
        var canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(_barRoot);
        canvasGO.transform.localPosition = Vector3.zero;
        canvasGO.transform.localScale    = Vector3.one * 0.01f;  // scale to world units

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rectTransform = canvasGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200f, 30f);

        // Background
        var bgGO  = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin  = Vector2.zero;
        bgRect.anchorMax  = Vector2.one;
        bgRect.offsetMin  = Vector2.zero;
        bgRect.offsetMax  = Vector2.zero;

        // Fill
        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform);
        _fillImage  = fillGO.AddComponent<Image>();
        _fillImage.color = colorFull;
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin  = new Vector2(0f, 0f);
        fillRect.anchorMax  = new Vector2(1f, 1f);
        fillRect.offsetMin  = Vector2.zero;
        fillRect.offsetMax  = Vector2.zero;
        _fillImage.type     = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(canvasGO.transform);
        _labelText  = labelGO.AddComponent<TextMeshProUGUI>();
        _labelText.alignment    = TextAlignmentOptions.Center;
        _labelText.fontSize     = 14f;
        _labelText.color        = Color.white;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin  = Vector2.zero;
        labelRect.anchorMax  = Vector2.one;
        labelRect.offsetMin  = Vector2.zero;
        labelRect.offsetMax  = Vector2.zero;

        _initialised = true;
        UpdateFill();
    }

    private void UpdateFill()
    {
        if (_fillImage == null) return;

        float pct = _health.HealthPercent;
        _fillImage.fillAmount = pct;
        _fillImage.color = pct > 0.75f ? colorFull
                         : pct > 0.50f ? colorDamaged
                         : pct > 0.25f ? colorCritical
                         :               colorDestroyed;

        if (_labelText != null)
            _labelText.text = $"{(int)_health.CurrentHealth} / {(int)_health.MaxHealth}";
    }
}
