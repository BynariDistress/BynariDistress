using UnityEngine;

/// <summary>
/// Centralised input reference sheet.
///
/// This class does NOT need to be attached to a GameObject.
/// Other scripts should use Unity's built-in Input class directly,
/// but you can extend this with static helpers or migrate to Unity's
/// new Input System package.
///
/// ════════════════════════════════════════════════════════════════
///  INPUT MAP  (Legacy Input System – Project Settings > Input Manager)
/// ════════════════════════════════════════════════════════════════
///
///  Movement
///  ─────────────────────────────────────────────
///  W / ↑           →  Vertical   axis +
///  S / ↓           →  Vertical   axis −
///  A / ←           →  Horizontal axis −
///  D / →           →  Horizontal axis +
///  Space           →  Jump button  ("Jump")
///  Left Shift      →  Sprint       (Input.GetKey(KeyCode.LeftShift))
///
///  Actions
///  ─────────────────────────────────────────────
///  C               →  Crouch toggle
///  R               →  Reload
///  F               →  Flashlight toggle
///  G               →  Throw grenade
///  E               →  Interact / Use
///  Q               →  Melee attack
///  Escape          →  Unlock cursor / Pause
///
///  Mouse
///  ─────────────────────────────────────────────
///  LMB             →  Fire1 ("Fire1")    – shoot
///  RMB             →  Fire2 ("Fire2")    – aim / ADS
///  Scroll Wheel    →  Mouse ScrollWheel axis – cycle weapons
///  1–9             →  Direct weapon slot select
///
/// ════════════════════════════════════════════════════════════════
///  HOW TO MIGRATE TO THE NEW INPUT SYSTEM
/// ════════════════════════════════════════════════════════════════
///
///  1. Install "Input System" package via Window > Package Manager.
///  2. In Project Settings > Player > Active Input Handling set to
///     "Input System Package (New)" or "Both".
///  3. Create an InputActions asset (right-click in Project > Create >
///     Input Actions). Map each action below.
///  4. Generate a C# class from the asset and replace Input.GetKey
///     calls with actions.Player.Move.ReadValue<Vector2>() etc.
///
/// ════════════════════════════════════════════════════════════════
///  PAUSE / GAME MANAGER (minimal example)
/// ════════════════════════════════════════════════════════════════
/// </summary>
public static class InputMap
{
    // Key constants – change here to re-bind everywhere
    public static readonly KeyCode Crouch    = KeyCode.C;
    public static readonly KeyCode Flashlight= KeyCode.F;
    public static readonly KeyCode Grenade   = KeyCode.G;
    public static readonly KeyCode Interact  = KeyCode.E;
    public static readonly KeyCode Melee     = KeyCode.Q;
    public static readonly KeyCode Reload    = KeyCode.R;
    public static readonly KeyCode Sprint    = KeyCode.LeftShift;
    public static readonly KeyCode Jump      = KeyCode.Space;
    public static readonly KeyCode Pause     = KeyCode.Escape;
}

/// <summary>
/// Optional PauseManager – attach to a Manager empty GameObject in the scene.
/// </summary>
public class PauseManager : MonoBehaviour
{
    private bool _paused;

    private void Update()
    {
        if (Input.GetKeyDown(InputMap.Pause))
            TogglePause();
    }

    private void TogglePause()
    {
        _paused = !_paused;
        Time.timeScale = _paused ? 0f : 1f;
        UIManager.Instance?.TogglePauseScreen(_paused);
    }

    private void OnApplicationQuit()
    {
        Time.timeScale = 1f; // safety reset
    }
}
