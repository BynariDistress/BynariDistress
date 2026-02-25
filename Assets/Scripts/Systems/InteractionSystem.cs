using UnityEngine;
using TMPro;          // remove this line if not using TextMeshPro

/// <summary>
/// Raycast-based interaction system (E key).
///
/// Attach to the Player GameObject.
/// Any GameObject that implements IInteractable will show a prompt
/// when the player looks at it within <interactRange>.
///
/// Setup:
///  • Assign <interactPromptText> to a TextMeshPro (or legacy Text) UI element
///    that shows the interaction hint (e.g., "E  Open Door").
///  • Leave <interactLayer> as Default or restrict to specific layers.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Raycast")]
    [SerializeField] private float     interactRange = 2.5f;
    [SerializeField] private LayerMask interactLayer = ~0;

    [Header("UI Prompt")]
    [SerializeField] private TMP_Text interactPromptText;  // swap for Text if needed

    // ── Private state ─────────────────────────────────────────────────────────
    private Camera        _cam;
    private PlayerController _player;
    private IInteractable _lookingAt;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _cam    = Camera.main;
        _player = GetComponent<PlayerController>();

        if (_cam == null)
            Debug.LogError("[InteractionSystem] No main camera found.");
    }

    private void Update()
    {
        DetectInteractable();
        HandleInteractInput();
    }

    // ── Detection ─────────────────────────────────────────────────────────────
    private void DetectInteractable()
    {
        if (_cam == null) return;

        IInteractable found = null;

        if (Physics.Raycast(_cam.transform.position, _cam.transform.forward,
                            out RaycastHit hit, interactRange, interactLayer))
        {
            // Walk up the hierarchy: the collider's root may hold the script
            found = hit.collider.GetComponentInParent<IInteractable>() ??
                    hit.collider.GetComponent<IInteractable>();
        }

        // Update prompt only when target changes
        if (found != _lookingAt)
        {
            _lookingAt = found;
            UpdatePrompt();
        }
    }

    private void UpdatePrompt()
    {
        if (interactPromptText == null) return;

        if (_lookingAt != null)
        {
            interactPromptText.text    = $"[E]  {_lookingAt.InteractPrompt}";
            interactPromptText.enabled = true;
        }
        else
        {
            interactPromptText.text    = string.Empty;
            interactPromptText.enabled = false;
        }
    }

    // ── Interaction ───────────────────────────────────────────────────────────
    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && _lookingAt != null)
        {
            _lookingAt.Interact(_player);
        }
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(Camera.main.transform.position,
                       Camera.main.transform.forward * interactRange);
    }
}
