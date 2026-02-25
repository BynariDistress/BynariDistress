using UnityEngine;

/// <summary>
/// Implement on any environment object that can be damaged and destroyed.
///
/// Consumed by:
///  • WeaponBase.PerformRaycast()  – bullet / raycast hits
///  • Grenade.Explode()            – explosion radius damage
///  • MeleeAttack hit check        – close-range melee
///
/// Concrete implementations:
///  • DestructibleHealth.cs        – main component for all destructibles
/// </summary>
public interface IDestructible
{
    /// <summary>
    /// Apply point damage at a specific world-space hit location.
    /// Called by weapon raycasts (bullets).
    /// </summary>
    /// <param name="damage">Raw damage value from the weapon.</param>
    /// <param name="hitPoint">World position where the projectile struck.</param>
    /// <param name="hitNormal">Surface normal at the hit point.</param>
    /// <param name="impactForce">Small physics push for bullet-hit feel (e.g. 100).</param>
    void ApplyDamage(float damage, Vector3 hitPoint, Vector3 hitNormal,
                     float impactForce = 100f);

    /// <summary>
    /// Apply area-of-effect explosion damage.
    /// Called by Grenade.Explode() using Physics.OverlapSphere results.
    /// Damage falloff from center to edge is handled internally.
    /// </summary>
    /// <param name="maxDamage">Maximum damage at explosion center.</param>
    /// <param name="explosionCenter">World position of the detonation.</param>
    /// <param name="radius">Blast radius.</param>
    /// <param name="force">Physics explosion force (applied to debris).</param>
    void ApplyExplosionDamage(float maxDamage, Vector3 explosionCenter,
                              float radius, float force);
}
