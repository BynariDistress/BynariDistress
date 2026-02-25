using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages pre-fractured chunk children on a destructible object.
/// Supports two modes, selected in the inspector:
///
///   ┌─────────────────────────────────────────────────────────────────┐
///   │  MODE A – Pre-fractured (default, zero external dependencies)  │
///   │                                                                 │
///   │  Hierarchy on your prefab:                                      │
///   │    DestructibleWall                                             │
///   │    ├── WholeObject      (MeshRenderer + Collider, intact look)  │
///   │    └── ChunksContainer  (parent of all chunk children)          │
///   │         ├── chunk_001   (MeshRenderer + Collider + Rigidbody)   │
///   │         ├── chunk_002                                           │
///   │         └── ...                                                 │
///   │                                                                 │
///   │  All chunks start with Rigidbody.isKinematic = true.           │
///   │  ReleaseChunksNear() sets nearby ones kinematic = false.       │
///   └─────────────────────────────────────────────────────────────────┘
///
///   ┌─────────────────────────────────────────────────────────────────┐
///   │  MODE B – OpenFracture  (requires OpenFracture package)        │
///   │                                                                 │
///   │  Add OPENFRACTURE_INSTALLED to Project Settings >              │
///   │  Player > Scripting Define Symbols.                            │
///   │  FractureController will delegate to the Fracture component.   │
///   └─────────────────────────────────────────────────────────────────┘
/// </summary>
public class FractureController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Mode")]
    [SerializeField] private bool useOpenFracture = false;
    [Tooltip("Show this when the object is intact. Hide it when fully destroyed.")]
    [SerializeField] private GameObject wholeObject;
    [Tooltip("Parent transform containing all pre-fractured chunk children.")]
    [SerializeField] private Transform  chunksContainer;

    [Header("Chunk Release")]
    [Tooltip("Maximum chunks that can be physics-active simultaneously from this object.")]
    [SerializeField] private int   maxActiveChunks  = 40;
    [Tooltip("Base upward/outward force added to each released chunk.")]
    [SerializeField] private float chunkLaunchForce = 200f;
    [Tooltip("Random torque range applied to each chunk for tumbling.")]
    [SerializeField] private float chunkTorque      = 15f;

    [Header("Debris Lifecycle")]
    [SerializeField] private float debrisLifetime   = 10f;   // seconds before fade
    [SerializeField] private float fadeDuration     = 2f;

    [Header("Collapse")]
    [Tooltip("Delay between releasing chunks during the collapse animation.")]
    [SerializeField] private float collapseWaveDelay = 0.05f;

    // ── Private state ─────────────────────────────────────────────────────────
    private List<Rigidbody> _allChunkRbs = new List<Rigidbody>();
    private int             _activeCount;
    private bool            _fullyCollapsed;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        InitChunks();
    }

    // ── Initialisation ────────────────────────────────────────────────────────
    private void InitChunks()
    {
        if (chunksContainer == null) return;

        _allChunkRbs.Clear();

        foreach (Transform child in chunksContainer)
        {
            // Ensure each chunk has a Rigidbody (add one if missing)
            if (!child.TryGetComponent<Rigidbody>(out var rb))
                rb = child.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // Ensure it has a collider
            if (child.GetComponent<Collider>() == null)
                child.gameObject.AddComponent<MeshCollider>();

            // Add DebrisObject component for lifecycle management
            if (child.GetComponent<DebrisObject>() == null)
            {
                var debris = child.gameObject.AddComponent<DebrisObject>();
                debris.Setup(debrisLifetime, fadeDuration);
            }

            child.gameObject.SetActive(false);
            _allChunkRbs.Add(rb);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Release chunks whose world-position is within <radius> of <center>.
    /// Called progressively as health stages are crossed.
    /// </summary>
    public void ReleaseChunksNear(Vector3 center, float radius, float force)
    {
#if OPENFRACTURE_INSTALLED
        if (useOpenFracture)
        {
            TriggerOpenFracture(center, force);
            return;
        }
#endif
        ReleaseNearScripted(center, radius, force);
    }

    /// <summary>
    /// Release ALL remaining chunks at once – called at the Destroyed stage.
    /// Plays a wave animation (bottom-up) for cinematic collapse feel.
    /// </summary>
    public void CollapseAll(Vector3 blastCenter, float force)
    {
        if (_fullyCollapsed) return;
        _fullyCollapsed = true;
        StartCoroutine(CollapseWave(blastCenter, force));
    }

    // ── Scripted fractured mode ───────────────────────────────────────────────
    private void ReleaseNearScripted(Vector3 center, float radius, float force)
    {
        int released = 0;

        foreach (var rb in _allChunkRbs)
        {
            if (rb == null || !rb.isKinematic) continue;  // already released
            if (_activeCount >= maxActiveChunks) break;

            float dist = Vector3.Distance(rb.transform.position, center);
            if (dist > radius) continue;

            ActivateChunk(rb, center, force);
            released++;
        }
    }

    private void ActivateChunk(Rigidbody rb, Vector3 forceCenter, float force)
    {
        // Detach from parent so it moves independently
        rb.transform.SetParent(null);
        rb.gameObject.SetActive(true);

        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Physics: push away from force center + upward arc
        Vector3 dir = (rb.transform.position - forceCenter).normalized;
        dir += Vector3.up * 0.4f;
        float appliedForce = (force + chunkLaunchForce) * Random.Range(0.7f, 1.3f);
        rb.AddForce(dir * appliedForce, ForceMode.Impulse);

        // Random tumble
        rb.AddTorque(Random.insideUnitSphere * chunkTorque, ForceMode.Impulse);

        // Register with global debris manager
        DestructionManager.Instance?.RegisterDebris(rb.GetComponent<DebrisObject>());
        _activeCount++;
    }

    private IEnumerator CollapseWave(Vector3 blastCenter, float force)
    {
        // Sort chunks bottom-to-top for a natural collapse cascade
        _allChunkRbs.Sort((a, b) =>
            a.transform.position.y.CompareTo(b.transform.position.y));

        // Hide whole mesh – structure is now fully replaced by chunks
        if (wholeObject != null) wholeObject.SetActive(false);

        foreach (var rb in _allChunkRbs)
        {
            if (rb == null) continue;
            if (!rb.isKinematic)
            {
                // Already released – just boost with extra force
                if (!rb.isKinematic)
                    rb.AddExplosionForce(force, blastCenter, 10f, 1f, ForceMode.Impulse);
                continue;
            }

            ActivateChunk(rb, blastCenter, force);
            yield return new WaitForSeconds(collapseWaveDelay);
        }
    }

    // ── OpenFracture bridge ───────────────────────────────────────────────────
#if OPENFRACTURE_INSTALLED
    // Import: using OpenFracture;  (add to the top of this file when enabled)
    //
    // OpenFracture's Fracture component is detected via GetComponent.
    // Its public API:
    //   fracture.FragmentCount          – number of pieces
    //   fracture.FractureOptions        – FractureParameters scriptable object
    //   void fracture.fractureMesh()    – triggers the runtime fracture
    //
    // NOTE: OpenFracture generates fragment GameObjects at runtime, which is
    // more expensive than pre-fractured chunks. For performance-critical scenes
    // prefer Mode A (pre-fractured) and use OpenFracture only in editor to
    // pre-bake the chunks.

    private void TriggerOpenFracture(Vector3 center, float force)
    {
        // Try to find the Fracture component using a string reference so that
        // this file compiles even without the package (the #if guard handles it).
        var fracture = wholeObject != null
            ? wholeObject.GetComponent("Fracture") as MonoBehaviour
            : GetComponent("Fracture") as MonoBehaviour;

        if (fracture == null)
        {
            Debug.LogWarning("[FractureController] OpenFracture: No 'Fracture' component found. " +
                             "Falling back to scripted mode.");
            ReleaseNearScripted(center, 5f, force);
            return;
        }

        // Invoke fractureMesh() via reflection
        var method = fracture.GetType().GetMethod("fractureMesh");
        method?.Invoke(fracture, null);
    }
#endif
}
