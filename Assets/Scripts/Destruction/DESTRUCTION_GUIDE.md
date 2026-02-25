# Destructible Environments – Setup Guide

Progressive BBC2-style destruction integrated with the FPS weapon system.

---

## Architecture Overview

```
Weapon/Grenade/Melee fires
        │
        ▼
  IDestructible.ApplyDamage / ApplyExplosionDamage
        │
        ▼
  DestructibleHealth          ← health + stage machine
        │ stage crosses threshold
        ▼
  FractureController          ← releases pre-fractured chunk children
        │ per-chunk physics activation
        ▼
  DebrisObject                ← lifetime timer → fade → pool return
        │ registration
        ▼
  DestructionManager          ← global cap, screen-shake, LOD throttle
```

---

## 1 · Project Setup

### Option A – Pure Scripted (No external packages, recommended to start)

All scripts in `Assets/Scripts/Destruction/` work out-of-the-box in Unity 2022+.
Pre-fracture your meshes in Blender (see §4).

### Option B – OpenFracture (runtime fracture, more visual variety)

1. `Window > Package Manager > + > Add package from git URL`
2. Paste: `https://github.com/dgreenheck/OpenFracture.git`
3. Import the package.
4. In `Project Settings > Player > Scripting Define Symbols` add:
   ```
   OPENFRACTURE_INSTALLED
   ```
5. On your destructible's `FractureController` component, tick **Use Open Fracture**.
6. Add OpenFracture's `Fracture` component to the `WholeObject` child.

### Option C – Libre Fracture 2.0 (graph-based joint system)

1. `Add package from git URL`: `https://github.com/HunterProduction/unity-libre-fracture-2.0.git`
2. Use their editor tool to pre-fracture and bake a joint graph.
3. On damage, call `LibreFracture.Break(point, force)` instead of our FractureController.
4. Still use `DestructibleHealth` for health management and events.

---

## 2 · Pre-Fracturing a Wall in Blender (Option A workflow)

### Blender Cell Fracture

1. Open Blender → `Edit > Preferences > Add-ons` → enable **Object: Cell Fracture**.
2. Model your wall (e.g. 4m × 3m × 0.3m box).
3. Select the wall → `Object > Quick Effects > Cell Fracture`.
   - Source: `Own Vertices`
   - Source Limit: `50` (adjust for quality/performance)
   - Noise: `0.5` (irregular-looking breaks)
   - Material index offset: `1` (so inside faces get a second material slot)
4. Click **OK** – Blender creates 50 child mesh objects.
5. Select all fragments → `File > Export > FBX`.
   - Enable **Selected Objects**, set **Path Mode: Copy**.

### Unity Import

1. Drag the FBX into `Assets/Models/`.
2. In the FBX importer, set **Read/Write** enabled on the mesh.
3. Create a prefab:

```
DestructibleWall  (root – add DestructibleHealth + FractureController + DestructionVFX + AudioSource)
├── WholeObject   (the original intact wall mesh + MeshCollider)
└── ChunksContainer  (empty parent, name it exactly "ChunksContainer")
    ├── chunk_001  (each fragment: MeshRenderer + MeshCollider + Rigidbody isKinematic=true)
    ├── chunk_002
    └── ...  (50 total)
```

4. Set all chunks to **inactive** in the editor.
5. Assign `ChunksContainer` transform to `FractureController.chunksContainer`.
6. Assign `WholeObject` to `FractureController.wholeObject`.

> **Tip**: Use a "concrete interior" material on the inside faces (Material slot 1)
> with a URP Lit shader, slightly darker and rougher than the exterior face.

---

## 3 · Creating the Wall Prefab in Unity

### Inspector Settings – DestructibleHealth

| Field | Value |
|-------|-------|
| Max Health | 500 |
| Start Health | 500 |
| Damaged At | 0.75 |
| Critical At | 0.50 |
| Structural At | 0.25 |
| Mat Intact | Concrete_Clean (URP Lit) |
| Mat Damaged | Concrete_Cracked1 (URP Lit) |
| Mat Critical | Concrete_Cracked2 |
| Mat Structural | Concrete_Broken |
| Chunk Radius Damaged | 0.6 |
| Chunk Radius Critical | 1.2 |
| Chunk Radius Structural | 2.0 |
| Bullet Resistance | 1.0 (concrete), 0.5 (wood), 2.0 (metal) |
| Explosion Resistance | 0.7 |

### Inspector Settings – FractureController

| Field | Value |
|-------|-------|
| Whole Object | WholeObject child GO |
| Chunks Container | ChunksContainer child transform |
| Max Active Chunks | 40 |
| Chunk Launch Force | 200 |
| Chunk Torque | 15 |
| Debris Lifetime | 10 |
| Fade Duration | 2 |
| Collapse Wave Delay | 0.05 |

### Inspector Settings – DestructionVFX

| Field | Value |
|-------|-------|
| Dust PS | Assign a Dust particle child |
| Collapse PS | Assign a large smoke/dust particle child |
| Debris Material Index | 1 (concrete) |
| Debris Count Per Hit | 4 |
| Impact Dust SFX | concrete_hit.wav |
| Collapse Rumble SFX | collapse_large.wav |

---

## 4 · Creating a Full Destructible House

### Hierarchy

```
Building  (DestructibleBuilding)
├── Foundation   (DestructibleHealth, maxHP=2000, isSupport=true)
│     FractureController, DestructionVFX
│     WholeObject + ChunksContainer
├── Wall_North   (DestructibleHealth, maxHP=600, isSupport=true)
│     dependentSections: [Roof]
├── Wall_South   (DestructibleHealth, maxHP=600)
├── Wall_East    (DestructibleHealth, maxHP=600, isSupport=true)
│     dependentSections: [Roof]
├── Wall_West    (DestructibleHealth, maxHP=600)
├── Floor_1      (DestructibleHealth, maxHP=800)
│     StructuralIntegrity → supports: [Foundation]
└── Roof         (DestructibleHealth, maxHP=500)
      StructuralIntegrity → supports: [Wall_North (weight=0.5), Wall_East (weight=0.5)]
```

### Wiring DestructibleBuilding in the Inspector

1. Add `DestructibleBuilding` to the root.
2. Set `sections` array size to 7.
3. For each entry, assign the `DestructibleHealth` component and configure
   `isStructuralSupport` and `dependentSections`.
4. Tick **Collapse All On Foundation Loss** for full-building collapse.

### Structural Integrity (Roof example)

On the Roof's `StructuralIntegrity` component:
- `supports[0]` → Wall_North, failureThreshold=0.3, weight=0.5
- `supports[1]` → Wall_East,  failureThreshold=0.3, weight=0.5
- `minSupportNeeded` = 0.4 (needs at least one wall at >30% health)
- `gravityDamagePerSecond` = 40
- `gravityDamageDelay` = 1.5

Result: Shoot out both supporting walls → after 1.5 seconds the roof starts
accumulating gravity damage and crumbles.

---

## 5 · Performance Optimization

### Debris Cap

In `DestructionManager` (Inspector):
- `maxActiveDebrisObjects` = 150  ← lower on mobile
- `maxFracturesPerFrame` = 2      ← prevents hitches from simultaneous fractures

### LOD for Buildings

1. Add an `LODGroup` to the building root.
2. LOD0: Full detail (all wall meshes)
3. LOD1: Simplified hull mesh (>30m from camera)
4. LOD2: Very low poly or impostor (>80m)
5. When a section is destroyed at LOD1/2, skip chunk physics entirely (just hide).

### Physics Layer for Debris

1. Create a new layer: **"Debris"**.
2. In `Edit > Project Settings > Physics`, uncheck Debris↔Debris collisions.
3. Debris still collides with Default/Player/Enemy but not with other debris.
4. Halves physics calculations for large destruction events.

### Rigidbody Sleep Tuning

In `Edit > Project Settings > Physics`:
- `Sleep Threshold` = 0.005 (debris settles faster, saves CPU)
- `Default Solver Iterations` = 4 (reduce from default 6 for debris)

---

## 6 · Damage Values Quick Reference

| Weapon | Damage | Notes |
|--------|--------|-------|
| Pistol | 35 | Weak vs concrete (×bulletResistance) |
| Rifle | 25/bullet | High fire rate compensates |
| Sniper | 150 | Can one-shot thin wood walls |
| Grenade blast | 80 (center) | Handles full collapse radius |
| Melee | 20 (50% of 40) | Chips but rarely collapses |

**Wall HP reference:**

| Material | Max HP | Notes |
|----------|--------|-------|
| Thin wood plank | 100 | 3–4 rifle bursts |
| Brick wall | 400 | Grenade + sustained fire |
| Concrete wall | 700 | Heavy explosives needed |
| Reinforced concrete | 1500 | Only grenades + sniper |
| Crate (wood) | 80 | Pistol 3-shot |

---

## 7 · Testing Checklist

```
[ ] Shoot wall with rifle – surface cracks at 75% HP (material swaps)
[ ] Continue shooting – chunks break off near impact point at 50%
[ ] Throw grenade next to wall – large section blows out
[ ] Wall reaches 0% HP – full collapse wave plays
[ ] Debris fades out after 10s (no performance creep)
[ ] Shoot out both supporting walls of house – roof collapses after ~1.5s
[ ] Collapse shows bottom-up wave effect (not all at once)
[ ] DestructionManager limits debris count (no hitches above 150 objects)
[ ] CameraShake plays on large collapses
[ ] World-space health bars visible in editor debug mode
[ ] Pistol does less chunk damage than grenade (resistance values)
[ ] Sniper one-shots thin wood walls (HP=100, damage=150)
[ ] FPS stays above 30 with 3+ simultaneous collapses
```

---

## 8 · Free Assets

| Asset | Use |
|-------|-----|
| **Modular Building Pack Lite** (Asset Store) | Wall/floor/roof mesh segments |
| **Kenney Modular Buildings** (kenney.nl) | Low-poly full buildings |
| **HDRP/URP Particle Pack** (Asset Store) | Dust, explosion, smoke PS |
| **Freesound: "concrete crumble"** | Impact / collapse SFX |
| **Freesound: "dust whoosh"** | Stage-transition audio |
| **Blender Cell Fracture** (built-in add-on) | Pre-fracture your custom meshes |
| **OpenFracture** (github.com/dgreenheck) | Runtime fracture alternative |
| **Libre Fracture 2.0** (github.com/HunterProduction) | Joint-graph destruction |

---

*Scripts location: `Assets/Scripts/Destruction/`*
*Integration patches in: `Assets/Scripts/Weapons/WeaponBase.cs`, `Grenade.cs`, `Systems/MeleeAttack.cs`*
