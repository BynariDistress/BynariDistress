using UnityEngine;

/// <summary>
/// Plays particle effects and audio at each destruction stage.
///
/// Attach to the same GameObject as DestructibleHealth.
/// Wire up particle systems and audio clips in the inspector.
///
/// Particle System naming guide:
///   dustPS    – puff of dust/smoke (loop=false, trigger once per stage)
///   sparksPS  – metal sparks (optional, for metal/pipe objects)
///   debrisPS  – small flying pebble/chip particles (short burst)
///
/// The script listens to DestructibleHealth.OnStageChanged and plays
/// the appropriate VFX for each stage.
/// </summary>
[RequireComponent(typeof(DestructibleHealth))]
public class DestructionVFX : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Particle Systems (assign children)")]
    [SerializeField] private ParticleSystem dustPS;      // generic dust cloud
    [SerializeField] private ParticleSystem debrisPS;    // small chips/pebbles
    [SerializeField] private ParticleSystem collapsePS;  // large cloud on full destroy
    [SerializeField] private ParticleSystem smokePS;     // lingering smoke (loop)

    [Header("Per-Stage Emission Multipliers")]
    [SerializeField] private float damagedDustAmount    = 5f;
    [SerializeField] private float criticalDustAmount   = 15f;
    [SerializeField] private float structuralDustAmount = 30f;
    [SerializeField] private float destroyedDustAmount  = 60f;

    [Header("Impact Debris (pooled)")]
    [Tooltip("0=Generic 1=Concrete 2=Wood 3=Metal 4=Brick – index into DebrisPool")]
    [SerializeField] private int   debrisMaterialIndex = 1;   // 1 = Concrete
    [SerializeField] private int   debrisCountPerHit   = 4;
    [SerializeField] private float debrisForce         = 80f;

    [Header("Audio")]
    [SerializeField] private AudioClip impactDust;
    [SerializeField] private AudioClip bigCrumble;
    [SerializeField] private AudioClip collapseRumble;

    // ── Private ───────────────────────────────────────────────────────────────
    private DestructibleHealth _health;
    private AudioSource        _audio;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _health = GetComponent<DestructibleHealth>();
        _audio  = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _health.OnStageChanged.AddListener(OnStageChanged);
    }

    private void OnDisable()
    {
        _health.OnStageChanged.RemoveListener(OnStageChanged);
    }

    // ── Stage response ────────────────────────────────────────────────────────
    private void OnStageChanged(DestructionStage stage)
    {
        switch (stage)
        {
            case DestructionStage.Damaged:
                BurstDust(damagedDustAmount);
                BurstDebrisPS();
                PlaySound(impactDust);
                break;

            case DestructionStage.Critical:
                BurstDust(criticalDustAmount);
                BurstDebrisPS();
                PlaySound(bigCrumble);
                break;

            case DestructionStage.Structural:
                BurstDust(structuralDustAmount);
                BurstDebrisPS();
                PlaySound(bigCrumble);
                break;

            case DestructionStage.Destroyed:
                BurstDust(destroyedDustAmount);
                PlayCollapse();
                StartSmokeLoop();
                PlaySound(collapseRumble);
                break;
        }
    }

    // ── Called externally for bullet-hit sparks ───────────────────────────────
    /// Spawn pooled debris chips at a bullet impact point.
    public void PlayBulletImpact(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (DebrisPool.Instance == null) return;

        for (int i = 0; i < debrisCountPerHit; i++)
            DebrisPool.Instance.SpawnDebrisAt(hitPoint, hitNormal,
                                              debrisForce, debrisMaterialIndex);
    }

    // ── Particle helpers ──────────────────────────────────────────────────────
    private void BurstDust(float count)
    {
        if (dustPS == null) return;
        var emission = new ParticleSystem.EmitParams { count = (int)count };
        dustPS.Emit(emission, (int)count);
    }

    private void BurstDebrisPS()
    {
        debrisPS?.Play();
    }

    private void PlayCollapse()
    {
        collapsePS?.Play();
    }

    private void StartSmokeLoop()
    {
        if (smokePS != null && !smokePS.isPlaying)
            smokePS.Play();
    }

    // ── Audio ─────────────────────────────────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }
}
