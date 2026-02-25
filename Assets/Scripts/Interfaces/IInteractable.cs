/// <summary>
/// Implement this interface on any GameObject the player can interact with
/// when pressing the E key (via InteractionSystem.cs).
///
/// Examples: doors, switches, pickup items, NPCs.
/// </summary>
public interface IInteractable
{
    /// <summary>Short label shown in the interact UI prompt (e.g. "Open Door").</summary>
    string InteractPrompt { get; }

    /// <summary>Called once when the player presses E while looking at this object.</summary>
    void Interact(PlayerController player);
}
