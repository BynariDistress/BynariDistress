using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple singleton object pool for debris chunks.
///
/// For pre-fractured mode, chunks are children of each destructible prefab
/// and do not use this pool (they are destroyed/disabled in-place).
///
/// This pool is intended for SPAWNED debris (e.g. rubble pieces that pop out
/// of any surface at impact, generic rock/concrete fragments defined per
/// material type in <debrisPrefabs>).
///
/// Usage:
///   GameObject chunk = DebrisPool.Instance.GetFromPool(prefabIndex);
///   // … position, activate …
///   DebrisPool.Instance.ReturnToPool(chunk);   // called by DebrisObject
/// </summary>
public class DebrisPool : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static DebrisPool Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Pool Settings")]
    [SerializeField] private int poolSizePerPrefab = 30;

    [Header("Debris Prefabs (indexed by material type)")]
    [Tooltip("0=Generic, 1=Concrete, 2=Wood, 3=Metal, 4=Brick")]
    [SerializeField] private GameObject[] debrisPrefabs;

    // ── Private state ─────────────────────────────────────────────────────────
    private Dictionary<int, Queue<GameObject>> _pools;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitPools();
    }

    private void InitPools()
    {
        _pools = new Dictionary<int, Queue<GameObject>>();
        if (debrisPrefabs == null) return;

        for (int i = 0; i < debrisPrefabs.Length; i++)
        {
            if (debrisPrefabs[i] == null) continue;

            var queue = new Queue<GameObject>();
            for (int j = 0; j < poolSizePerPrefab; j++)
            {
                var go = Instantiate(debrisPrefabs[i], transform);
                go.SetActive(false);
                queue.Enqueue(go);
            }
            _pools[i] = queue;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <param name="prefabIndex">0=Generic, 1=Concrete, 2=Wood, 3=Metal, 4=Brick</param>
    public GameObject GetFromPool(int prefabIndex = 0)
    {
        if (_pools == null || !_pools.TryGetValue(prefabIndex, out var queue))
            return FallbackSpawn(prefabIndex);

        if (queue.Count == 0)
            return FallbackSpawn(prefabIndex);

        var go = queue.Dequeue();
        go.SetActive(false);   // caller must position then enable
        return go;
    }

    /// Return an object to the pool. Returns true if accepted, false if pool is full.
    public bool ReturnToPool(GameObject go)
    {
        if (go == null) return false;

        // Find which pool this belongs to by prefab match
        int prefabIndex = FindPrefabIndex(go);
        if (prefabIndex < 0) return false;  // not from any pool

        go.SetActive(false);
        go.transform.SetParent(transform);
        _pools[prefabIndex].Enqueue(go);
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private GameObject FallbackSpawn(int index)
    {
        if (debrisPrefabs == null || index >= debrisPrefabs.Length
            || debrisPrefabs[index] == null)
        {
            Debug.LogWarning($"[DebrisPool] No prefab at index {index}.");
            return null;
        }
        // Overflow: spawn a new object (will be destroyed when returned
        // if pool doesn't want it)
        return Instantiate(debrisPrefabs[index]);
    }

    private int FindPrefabIndex(GameObject go)
    {
        if (debrisPrefabs == null) return -1;
        string goName = go.name.Replace("(Clone)", "").Trim();
        for (int i = 0; i < debrisPrefabs.Length; i++)
        {
            if (debrisPrefabs[i] != null && debrisPrefabs[i].name == goName)
                return i;
        }
        return -1;
    }

    // ── Convenience spawner ───────────────────────────────────────────────────
    /// Spawn a debris chunk at a world position with an explosion force impulse.
    public void SpawnDebrisAt(Vector3 position, Vector3 normal,
                              float force, int prefabIndex = 0)
    {
        var go = GetFromPool(prefabIndex);
        if (go == null) return;

        go.transform.position = position + normal * 0.05f;
        go.transform.rotation = Random.rotation;
        go.SetActive(true);

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            Vector3 dir = (normal + Vector3.up * 0.5f).normalized;
            rb.AddForce(dir * force * Random.Range(0.5f, 1.5f), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }
    }
}
