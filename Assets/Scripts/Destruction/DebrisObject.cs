using System.Collections;
using UnityEngine;

/// <summary>
/// Attached automatically by FractureController to every chunk child.
///
/// Lifecycle:
///   Activated (kinematic = false)
///     → spins freely under physics
///     → after <activeLifetime> seconds  OR  velocity < sleepThreshold
///     → starts DebrisFade (alpha → 0)
///     → returned to pool / destroyed
///
/// The component starts disabled; FractureController.ActivateChunk() enables it.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DebrisObject : MonoBehaviour
{
    // ── Config (set by FractureController.Setup()) ────────────────────────────
    private float _activeLifetime = 10f;
    private float _fadeDuration   = 2f;

    [Header("Sleep Detection")]
    [SerializeField] private float sleepVelocityThreshold = 0.05f;
    [SerializeField] private float sleepCheckDelay        = 2f;   // wait before checking

    // ── Private state ─────────────────────────────────────────────────────────
    private Rigidbody   _rb;
    private Renderer    _renderer;
    private bool        _fadingOut;
    private float       _spawnTime;

    // ── Setup (called by FractureController before activation) ────────────────
    public void Setup(float lifetime, float fadeDuration)
    {
        _activeLifetime = lifetime;
        _fadeDuration   = fadeDuration;
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _rb       = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        _fadingOut = false;
        _spawnTime = Time.time;
        StartCoroutine(LifetimeRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _fadingOut = false;
    }

    // ── Lifetime management ───────────────────────────────────────────────────
    private IEnumerator LifetimeRoutine()
    {
        // Give the chunk time to fly before checking sleep
        yield return new WaitForSeconds(sleepCheckDelay);

        // Wait until physics velocity drops or max lifetime is reached
        while (!_fadingOut)
        {
            bool timeExpired  = (Time.time - _spawnTime) >= _activeLifetime;
            bool almostSleep  = _rb != null && _rb.linearVelocity.sqrMagnitude
                                    < sleepVelocityThreshold * sleepVelocityThreshold;

            if (timeExpired || almostSleep)
            {
                StartFade();
                yield break;
            }

            yield return new WaitForSeconds(0.5f);  // poll interval
        }
    }

    // ── Fade ──────────────────────────────────────────────────────────────────
    private void StartFade()
    {
        if (_fadingOut) return;
        _fadingOut = true;

        // Freeze physics so the chunk settles before disappearing
        if (_rb != null)
        {
            _rb.linearVelocity        = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic     = true;
        }

        StartCoroutine(FadeAndDespawn());
    }

    private IEnumerator FadeAndDespawn()
    {
        if (_renderer == null)
        {
            ReturnToPool();
            yield break;
        }

        // We need a material instance to change alpha – do NOT modify shared material
        Material mat = _renderer.material;   // creates per-instance copy

        // Ensure URP material is in transparent mode
        SetUrpTransparent(mat);

        Color startColor = mat.color;
        float elapsed    = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
            Color c     = startColor;
            c.a         = alpha;
            mat.color   = c;
            yield return null;
        }

        ReturnToPool();
    }

    /// Enable alpha-blending on a URP Lit or Standard material at runtime.
    private static void SetUrpTransparent(Material mat)
    {
        // URP Lit shader
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);          // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);             // Alpha blend
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        // Legacy Standard shader
        else if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3f);             // Fade mode
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }
    }

    // ── Pool / destroy ────────────────────────────────────────────────────────
    private void ReturnToPool()
    {
        DestructionManager.Instance?.UnregisterDebris(this);

        // Try to return to pool; if none, destroy
        if (!DebrisPool.Instance?.ReturnToPool(gameObject) ?? true)
            Destroy(gameObject);
    }
}
