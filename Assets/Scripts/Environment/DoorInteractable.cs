using System.Collections;
using UnityEngine;

/// <summary>
/// A door that opens/closes when the player presses E.
/// Implements IInteractable for use with InteractionSystem.cs.
///
/// Setup:
///  1. The door panel (the moving part) should be a child of this GameObject.
///     Assign it to <doorPanel>.
///  2. Adjust <openRotation> to the angle the door swings to (e.g., 90 degrees Y).
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Door")]
    [SerializeField] private Transform doorPanel;           // the swinging/sliding part
    [SerializeField] private Vector3   openRotation   = new Vector3(0f, 90f, 0f);
    [SerializeField] private float     openSpeed      = 3f;
    [SerializeField] private bool      lockedByDefault= false;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;

    // ── IInteractable ─────────────────────────────────────────────────────────
    public string InteractPrompt => _isLocked ? "Locked"
                                  : _isOpen   ? "Close Door"
                                  :             "Open Door";

    // ── Private state ─────────────────────────────────────────────────────────
    private bool   _isOpen;
    private bool   _isLocked;
    private bool   _isMoving;
    private AudioSource _audio;

    private Quaternion _closedRot;
    private Quaternion _openRot;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _audio    = GetComponent<AudioSource>();
        _isLocked = lockedByDefault;

        if (doorPanel != null)
        {
            _closedRot = doorPanel.localRotation;
            _openRot   = Quaternion.Euler(openRotation);
        }
    }

    // ── Interaction ───────────────────────────────────────────────────────────
    public void Interact(PlayerController player)
    {
        if (_isMoving) return;

        if (_isLocked)
        {
            PlaySound(lockedSound);
            return;
        }

        _isOpen = !_isOpen;
        StartCoroutine(MoveDoor(_isOpen ? _openRot : _closedRot));
        PlaySound(_isOpen ? openSound : closeSound);
    }

    private IEnumerator MoveDoor(Quaternion targetRot)
    {
        _isMoving = true;

        while (Quaternion.Angle(doorPanel.localRotation, targetRot) > 0.5f)
        {
            doorPanel.localRotation = Quaternion.Slerp(
                doorPanel.localRotation, targetRot, openSpeed * Time.deltaTime);
            yield return null;
        }

        doorPanel.localRotation = targetRot;
        _isMoving = false;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void Unlock() => _isLocked = false;
    public void Lock()   => _isLocked = true;

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }
}
