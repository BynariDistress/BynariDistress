using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Multi-section building with hierarchical / cascading destruction.
///
/// Inspired by BBC2: walls crumble independently, removing a load-bearing
/// wall eventually brings the floor/ceiling above it down.
///
/// ── Hierarchy Example ────────────────────────────────────────────────────────
///
///   Building (DestructibleBuilding)
///   ├── Foundation       (DestructibleHealth, isSupport=true,  supportsAll)
///   ├── Wall_North       (DestructibleHealth, isSupport=true,  supportsRoof)
///   ├── Wall_South       (DestructibleHealth, isSupport=false)
///   ├── Wall_East        (DestructibleHealth, isSupport=true,  supportsRoof)
///   ├── Wall_West        (DestructibleHealth, isSupport=false)
///   ├── Floor_1          (DestructibleHealth, dependsOn=Foundation)
///   └── Roof             (DestructibleHealth, dependsOn=Wall_North,Wall_East)
///
/// ── Inspector ────────────────────────────────────────────────────────────────
///  • sections[]   – list of BuildingSection structs (assign in inspector).
///  • collapseAll  – if true, destroying the foundation destroys everything.
///
/// </summary>
public class DestructibleBuilding : MonoBehaviour
{
    // ── Data ──────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class BuildingSection
    {
        [Tooltip("The DestructibleHealth component for this section of the building.")]
        public DestructibleHealth section;

        [Tooltip("If destroyed, sections listed in 'dependentSections' lose support.")]
        public bool isStructuralSupport = false;

        [Tooltip("When this support is destroyed, these sections begin collapsing.")]
        public DestructibleHealth[] dependentSections;

        [Tooltip("Delay before dependents start collapsing (seconds). Adds cinematic feel.")]
        public float collapseDelay = 0.5f;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Building Sections")]
    [SerializeField] private BuildingSection[] sections;

    [Header("Collapse Behaviour")]
    [SerializeField] private bool  collapseAllOnFoundationLoss = true;
    [Tooltip("Force applied to all sections when the building globally collapses.")]
    [SerializeField] private float globalCollapseForce         = 1500f;
    [SerializeField] private float sectionCollapseStagger      = 0.3f;  // wave effect

    [Header("Audio")]
    [SerializeField] private AudioClip buildingCollapseSound;
    [SerializeField] private float     buildingCollapseVolume = 1.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private bool _globalCollapsing;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        // Subscribe to each section's destruction event
        foreach (var s in sections)
        {
            if (s.section == null) continue;
            // Capture loop variable for closure correctness
            BuildingSection captured = s;
            s.section.OnDestroyed.AddListener(() => OnSectionDestroyed(captured));
        }
    }

    // ── Section collapse ──────────────────────────────────────────────────────
    private void OnSectionDestroyed(BuildingSection destroyedSection)
    {
        if (!destroyedSection.isStructuralSupport) return;

        // Check if this was the foundation (index 0) and collapse all
        if (collapseAllOnFoundationLoss && sections.Length > 0
            && sections[0].section == destroyedSection.section)
        {
            StartCoroutine(GlobalCollapse());
            return;
        }

        // Cascade: damage dependent sections to trigger their own destruction
        if (destroyedSection.dependentSections == null) return;

        foreach (var dep in destroyedSection.dependentSections)
        {
            if (dep == null || dep.IsDestroyed) continue;
            StartCoroutine(DelayedDependentCollapse(dep, destroyedSection.collapseDelay));
        }
    }

    /// When a support is lost, dependent sections take heavy damage after a delay.
    private IEnumerator DelayedDependentCollapse(DestructibleHealth target, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target == null || target.IsDestroyed) yield break;

        // Deal enough damage to push the dependent section to Destroyed stage
        float killDamage = target.CurrentHealth + 1f;
        target.ApplyExplosionDamage(killDamage, target.transform.position, 5f,
                                    globalCollapseForce);
    }

    // ── Global collapse ───────────────────────────────────────────────────────
    private IEnumerator GlobalCollapse()
    {
        if (_globalCollapsing) yield break;
        _globalCollapsing = true;

        // Screen shake
        DestructionManager.Instance?.OnStructureDamaged(null, DestructionStage.Destroyed);

        // Collapse sound at building center
        if (buildingCollapseSound != null)
            AudioSource.PlayClipAtPoint(buildingCollapseSound,
                                        transform.position, buildingCollapseVolume);

        // Stagger destruction of all sections bottom-to-top
        // Sort by Y position for natural bottom-up collapse
        var sorted = new List<BuildingSection>(sections);
        sorted.Sort((a, b) =>
        {
            float yA = a.section != null ? a.section.transform.position.y : 0f;
            float yB = b.section != null ? b.section.transform.position.y : 0f;
            return yA.CompareTo(yB);
        });

        foreach (var s in sorted)
        {
            if (s.section == null || s.section.IsDestroyed) continue;

            float damage = s.section.CurrentHealth + 1f;
            s.section.ApplyExplosionDamage(damage, transform.position, 20f,
                                           globalCollapseForce);
            yield return new WaitForSeconds(sectionCollapseStagger);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// Force a full building collapse immediately (e.g. scripted event, cutscene).
    public void ForceCollapse()
    {
        StartCoroutine(GlobalCollapse());
    }

    /// Returns true if every section is destroyed.
    public bool IsFullyDestroyed()
    {
        foreach (var s in sections)
            if (s.section != null && !s.section.IsDestroyed) return false;
        return true;
    }
}
