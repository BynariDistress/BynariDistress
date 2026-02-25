using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Generic on/off switch (lever, button, console panel).
/// Fires UnityEvents so you can wire it to any target in the inspector.
///
/// Example: wire OnActivate to DoorInteractable.Unlock() to create
/// a key-card switch that unlocks a door.
/// </summary>
public class SwitchInteractable : MonoBehaviour, IInteractable
{
    [Header("Switch")]
    [SerializeField] private bool  startOn          = false;
    [SerializeField] private bool  toggleable        = true;   // false = one-shot

    [Header("Events")]
    public UnityEvent OnActivate;    // called when switched ON
    public UnityEvent OnDeactivate;  // called when switched OFF

    [Header("Animation")]
    [SerializeField] private Animator switchAnimator;
    [SerializeField] private string   onTrigger  = "Activate";
    [SerializeField] private string   offTrigger = "Deactivate";

    [Header("Audio")]
    [SerializeField] private AudioClip onSound;
    [SerializeField] private AudioClip offSound;

    // ── Private state ─────────────────────────────────────────────────────────
    private bool        _isOn;
    private bool        _used;   // for one-shot switches
    private AudioSource _audio;

    public string InteractPrompt => _isOn ? "Switch Off" : "Switch On";

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _isOn  = startOn;
    }

    // ── IInteractable ─────────────────────────────────────────────────────────
    public void Interact(PlayerController player)
    {
        if (!toggleable && _used) return;   // one-shot already fired

        _isOn = !_isOn;
        _used = true;

        if (_isOn)
        {
            OnActivate?.Invoke();
            TriggerAnim(onTrigger);
            PlaySound(onSound);
        }
        else
        {
            OnDeactivate?.Invoke();
            TriggerAnim(offTrigger);
            PlaySound(offSound);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void TriggerAnim(string trig)
    {
        if (switchAnimator != null && !string.IsNullOrEmpty(trig))
            switchAnimator.SetTrigger(trig);
    }

    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }
}
