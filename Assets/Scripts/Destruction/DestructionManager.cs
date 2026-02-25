using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global singleton that oversees all active destruction events.
///
/// Responsibilities:
///  1. Caps the total number of live physics debris objects (performance).
///  2. When the cap is exceeded, force-fades the oldest/slowest debris.
///  3. Provides a centralised channel so UI / analytics can listen.
///  4. Applies optional screen-shake to the player camera on large collapses.
///
/// Place ONE instance on a persistent Manager GameObject in your scene.
/// </summary>
public class DestructionManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static DestructionManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Performance Caps")]
    [SerializeField] private int maxActiveDebrisObjects = 150;
    [SerializeField] private int maxFracturesPerFrame   = 2;   // throttle simultaneous fractures

    [Header("Screen Shake")]
    [SerializeField] private bool  enableScreenShake   = true;
    [SerializeField] private float shakeIntensitySmall = 0.05f;  // Damaged stage
    [SerializeField] private float shakeIntensityLarge = 0.25f;  // Destroyed stage
    [SerializeField] private float shakeDuration       = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool logDestructionEvents = false;

    // ── Private state ─────────────────────────────────────────────────────────
    private LinkedList<DebrisObject> _activeDebris = new LinkedList<DebrisObject>();
    private int _fracturesThisFrame;

    // ── Properties ────────────────────────────────────────────────────────────
    public int ActiveDebrisCount => _activeDebris.Count;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void LateUpdate()
    {
        _fracturesThisFrame = 0;   // reset throttle counter each frame
    }

    // ── Debris registration ───────────────────────────────────────────────────

    /// Called by DebrisObject or FractureController when a chunk becomes active.
    public void RegisterDebris(DebrisObject debris)
    {
        if (debris == null) return;
        _activeDebris.AddLast(debris);

        // Enforce cap: evict slowest/oldest debris objects
        while (_activeDebris.Count > maxActiveDebrisObjects)
            ForceEvict();
    }

    /// Called by DebrisObject when it's about to return to pool or be destroyed.
    public void UnregisterDebris(DebrisObject debris)
    {
        _activeDebris.Remove(debris);
    }

    // ── Structure events ──────────────────────────────────────────────────────

    /// Called by DestructibleHealth on each stage change.
    public void OnStructureDamaged(DestructibleHealth structure, DestructionStage stage)
    {
        if (logDestructionEvents)
            Debug.Log($"[DestructionManager] {structure.name} → {stage}");

        if (!enableScreenShake) return;

        switch (stage)
        {
            case DestructionStage.Critical:
                ShakeCamera(shakeIntensitySmall, shakeDuration);
                break;
            case DestructionStage.Structural:
                ShakeCamera(shakeIntensitySmall * 1.5f, shakeDuration);
                break;
            case DestructionStage.Destroyed:
                ShakeCamera(shakeIntensityLarge, shakeDuration * 1.5f);
                break;
        }
    }

    /// Returns true if another fracture can be processed this frame (throttle).
    public bool CanFractureThisFrame()
    {
        if (_fracturesThisFrame >= maxFracturesPerFrame) return false;
        _fracturesThisFrame++;
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private void ForceEvict()
    {
        // Evict the first (oldest) debris in the list
        var node = _activeDebris.First;
        if (node == null) return;

        _activeDebris.RemoveFirst();
        var debris = node.Value;
        if (debris != null)
            debris.gameObject.SetActive(false);   // DebrisObject.OnDisable handles pool return
    }

    // ── Screen Shake ──────────────────────────────────────────────────────────
    private void ShakeCamera(float intensity, float duration)
    {
        // Find the CameraShake component on the main camera (if present)
        if (Camera.main != null
            && Camera.main.TryGetComponent<CameraShake>(out var shake))
        {
            shake.Shake(intensity, duration);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Optional lightweight camera shake component.
// Attach to the Main Camera.
// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Positional camera shake triggered by DestructionManager.
/// Attach to the Main Camera or CameraHolder.
/// Uses a diminishing perlin-noise offset so it doesn't drift.
/// </summary>
public class CameraShake : MonoBehaviour
{
    private float   _intensity;
    private float   _duration;
    private float   _elapsed;
    private Vector3 _originalLocalPos;
    private bool    _shaking;

    public void Shake(float intensity, float duration)
    {
        // Allow stronger shake to override a weaker one
        if (intensity > _intensity || !_shaking)
        {
            _intensity       = intensity;
            _duration        = duration;
            _elapsed         = 0f;
            _originalLocalPos = transform.localPosition;
            _shaking         = true;
        }
    }

    private void Update()
    {
        if (!_shaking) return;

        _elapsed += Time.deltaTime;
        float progress  = _elapsed / _duration;
        float remaining = 1f - Mathf.Clamp01(progress);   // fade out

        float x = (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * _intensity * remaining;
        float y = (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * _intensity * remaining;

        transform.localPosition = _originalLocalPos + new Vector3(x, y, 0f);

        if (_elapsed >= _duration)
        {
            transform.localPosition = _originalLocalPos;
            _shaking  = false;
        }
    }
}
