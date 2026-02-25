# FPS Game – Unity Setup Guide

Complete step-by-step instructions for assembling the FPS project in Unity 2022+.

---

## 1 · Project Setup

| Step | Action |
|------|--------|
| 1 | **New Project** → Unity 2022.3 LTS → **Universal Render Pipeline (URP)** template |
| 2 | `Window > Package Manager` → install **TextMeshPro** (for UI text) |
| 3 | `Window > Package Manager` → install **AI Navigation** (for NavMesh / EnemyAI) |
| 4 | (Optional) Install **Cinemachine** for smoother camera effects |
| 5 | Copy all scripts from `Assets/Scripts/` into your Unity project |

---

## 2 · Player Prefab Hierarchy

```
Player  (GameObject with Tag = "Player")
├── [Components on Player root]
│     CharacterController  (Height=2, Radius=0.4, Center Y=1)
│     PlayerController.cs
│     MouseLook.cs         ← assign CameraHolder to "cameraTransform"
│     PlayerHealth.cs
│     InteractionSystem.cs ← assign interact prompt TMP_Text
│     GrenadeThrow.cs
│     MeleeAttack.cs
│     Flashlight.cs
│     AudioSource
│
├── CameraHolder  (empty, localPos Y=0.8)
│     ├── [Components]
│     │     MouseLook assigns this as cameraTransform (for pitch rotation)
│     │
│     └── Main Camera  (Camera component, FOV=60, Near=0.1)
│           └── WeaponHolder  (empty, localPos Z=0.3, Y=-0.2)
│                 ├── Pistol  (disabled by default)
│                 ├── Rifle   (disabled by default)
│                 └── SniperRifle (disabled by default)
│
└── FlashlightGO  (child of CameraHolder)
      Light  (Spot, Range=15, Angle=35, Intensity=3)
      ← assign to Flashlight.cs "flashlight" field
```

### Assigning references in the inspector

| Script field | Assign |
|---|---|
| `PlayerController.cameraHolder` | CameraHolder transform |
| `MouseLook.cameraTransform` | CameraHolder transform |
| `WeaponManager.weapons[]` | Pistol, Rifle, SniperRifle (in WeaponHolder) |
| `Flashlight.flashlight` | The Spot Light in FlashlightGO |
| `InteractionSystem.interactPromptText` | TMP_Text in Canvas |
| `MeleeAttack.weaponAnimator` | Knife model Animator (optional) |

---

## 3 · Player Controller – Feature Checklist

| Feature | Key | Script | Notes |
|---------|-----|--------|-------|
| Move forward/back | W / S | `PlayerController` | `Vertical` axis |
| Strafe left/right | A / D | `PlayerController` | `Horizontal` axis |
| Sprint | Left Shift + W | `PlayerController` | `IsSprinting` flag |
| Jump | Space | `PlayerController` | `jumpHeight` multiplier |
| Crouch (toggle) | C | `PlayerController` | Adjusts CC height + camera Y |
| Mouse look | Mouse X/Y | `MouseLook` | Yaw on body, pitch on CameraHolder |

---

## 4 · Weapon System

### Creating a Weapon Prefab

1. Create an empty child inside `WeaponHolder` → name it `Rifle`.
2. Add a 3D mesh (or placeholder cube) as a child for the gun model.
3. Add an empty child at the barrel tip → name it `MuzzlePoint`.
4. Assign scripts:
   - Add `Rifle.cs` (or `Pistol.cs` / `SniperRifle.cs`) to the root.
   - Add an `AudioSource` to the root.
   - Assign `muzzlePoint`, `fireSound`, `reloadSound`, `emptySound`.
5. Drag the weapon GO into `WeaponManager.weapons[]` (on the Player).
6. Leave the weapon **disabled** – WeaponManager enables only the active one.

### Weapon Controls

| Action | Input | Script |
|--------|-------|--------|
| Fire | LMB (hold) | `WeaponBase.Fire()` |
| Aim Down Sights | RMB (hold) | `WeaponBase.HandleADS()` |
| Reload | R | `WeaponBase.HandleReloadInput()` |
| Cycle weapons | Scroll wheel | `WeaponManager` |
| Direct slot | 1–9 keys | `WeaponManager` |

### Adding a New Weapon Type

```csharp
// MyNewGun.cs
public class MyNewGun : WeaponBase
{
    protected override void Awake()
    {
        weaponName  = "Shotgun";
        damage      = 15f;    // per pellet
        fireRate    = 0.8f;
        clipSize    = 8;
        reserveAmmo = 32;
        base.Awake();
    }

    protected override void Fire()
    {
        if (!Input.GetButtonDown("Fire1")) return;
        // Shotgun fires 8 pellets
        for (int i = 0; i < 8; i++)
        {
            // Randomise direction before calling base raycasting
            PerformRaycast();
        }
        // Play sound, flash, recoil...
    }
}
```

---

## 5 · Enemy Setup

### NavMesh Baking

1. Mark all floor/wall/obstacle meshes as **Navigation Static**
   (Inspector > Static dropdown > Navigation Static).
2. Open `Window > AI > Navigation` → **Bake** tab → click **Bake**.

### Enemy Prefab

```
EnemyRoot
├── [Components]
│     NavMeshAgent   (Speed=3.5, StoppingDistance=1.5)
│     Animator       (see Animator section below)
│     EnemyAI.cs     ← assign patrol waypoints
│     EnemyHealth.cs
│     AudioSource
│     Capsule Collider
└── Model (humanoid mesh + rig)
```

### Enemy Patrol

1. Create several empty GameObjects at floor level → name them `Waypoint_01`, etc.
2. Drag them into `EnemyAI.patrolPoints[]`.

### Animator Controller for Enemy

Create `EnemyAnimator.controller`:

| Parameter | Type |
|-----------|------|
| `Speed` | Float |
| `IsChasing` | Bool |
| `Attack` | Trigger |
| `Die` | Trigger |
| `IsDead` | Bool |

Transitions:
- Idle → Walk: `Speed > 0.1`
- Walk → Run: `Speed > 3.0`
- Any State → Attack: `Attack` trigger
- Any State → Die: `Die` trigger

---

## 6 · UI Canvas Setup

Create a **Screen Space – Overlay** Canvas with these child elements:

```
Canvas
├── Crosshair          Image, anchored center, 32×32 px white dot/ring
├── AmmoText           TMP_Text, bottom-right, "30 / 120"
├── WeaponNameText     TMP_Text, above ammo
├── GrenadeText        TMP_Text, next to ammo
├── HealthBar          Slider, bottom-left (Fill = red Image)
├── HealthText         TMP_Text, inside health bar
├── ReloadText         TMP_Text, center-bottom  "RELOADING…" (disabled by default)
├── HitIndicator       Image, full-screen, red color, alpha=0 (disabled by default)
├── DeathScreen        Panel, center, with "YOU DIED" text (inactive by default)
├── PauseScreen        Panel (inactive by default)
└── InteractPrompt     TMP_Text, center slightly below middle (disabled by default)
```

Assign each element to `UIManager` in the inspector.

Subscribe `PlayerHealth.OnHealthChanged` to `UIManager.UpdateHealth`:
1. Select Player in hierarchy.
2. In `PlayerHealth` inspector, find `OnHealthChanged` event.
3. Click `+` → drag UIManager → select `UIManager.UpdateHealth`.

---

## 7 · Animations

### Player Weapon Animations

| Clip Name | Trigger | Description |
|-----------|---------|-------------|
| `Fire` | `Fire` | Quick recoil snap |
| `Reload` | `Reload` | Eject mag, insert new |
| `Aim` | Bool `IsAiming` | Move weapon to iron-sight position |
| `Melee` | `Melee` | Knife slash or fist punch |

### Recommended Free Assets (Unity Asset Store)

| Asset | Purpose |
|-------|---------|
| **Starter Assets – First Person** | Ready-made FPS player with animations |
| **Kenney's FPS Arms** | Low-poly weapon arms |
| **FREE Stylized Explosion VFX** | Grenade/explosion particles |
| **Simple FPS Weapons Pack** | Gun models + idle/fire animations |
| **Low Poly FPS Pack** | Guns + muzzle flash prefabs |

---

## 8 · Sound Effects (free sources)

- **Freesound.org** – search: "gunshot", "reload", "footstep", "explosion"
- **Kenney.nl/assets** – UI sounds, impacts, footsteps
- Import `.wav` or `.ogg` files → drag into each AudioClip field in the inspector

---

## 9 · Testing Checklist

Test each feature by entering **Play Mode**:

```
[ ] WASD movement feels smooth
[ ] Sprint (Shift) increases speed
[ ] Jump (Space) leaves the ground
[ ] Crouch (C) lowers camera; camera rises back on second press
[ ] Mouse look rotates view, pitch clamped at ±85°
[ ] LMB fires; ammo count decreases
[ ] R reloads; ammo restored from reserve
[ ] RMB zooms FOV (ADS)
[ ] Scroll wheel cycles weapons; 1/2/3 keys select directly
[ ] Q performs melee; enemy health decreases in range
[ ] G throws grenade; explosion damages nearby enemies
[ ] F toggles flashlight on/off
[ ] E opens door / shows interact prompt when looking at interactable
[ ] Enemy patrols between waypoints
[ ] Enemy chases when player enters sight range
[ ] Enemy attacks and damages player at close range
[ ] Health bar updates on damage
[ ] Ammo HUD updates after firing and reloading
[ ] Death screen appears when health reaches 0
```

---

## 10 · Scene Quick-Start

1. `File > New Scene` → Empty.
2. Add a **Plane** (scale 10×10) as the ground, mark Navigation Static.
3. Add a few cubes as cover/obstacles.
4. Drag your **Player prefab** into the scene at `(0, 1, 0)`.
5. Drag your **Canvas prefab** (or create the UI elements above).
6. Drag your **UIManager** onto the Canvas.
7. Add 1–2 **Enemy prefabs** at `(5, 0, 5)`.
8. Add an **AmmoPickup** and **HealthPickup** in the level.
9. Bake the NavMesh.
10. Press **Play** – the game should be fully playable.

---

*All scripts are in `Assets/Scripts/`. Subfolders: Player, Weapons, Systems, Enemy, UI, Pickups, Environment, Interfaces.*
