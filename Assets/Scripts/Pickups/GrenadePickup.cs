using UnityEngine;

/// <summary>
/// Grenade pickup – adds grenades to the player's GrenadeThrow component.
/// </summary>
public class GrenadePickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int   grenadeAmount = 2;
    [SerializeField] private bool  autoCollect   = true;
    [SerializeField] private AudioClip pickupSound;

    public string InteractPrompt => $"Pick up Grenades (+{grenadeAmount})";

    public void Interact(PlayerController player)
    {
        Collect(player.GetComponent<GrenadeThrow>());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoCollect) return;
        if (!other.CompareTag("Player")) return;
        Collect(other.GetComponent<GrenadeThrow>());
    }

    private void Collect(GrenadeThrow gt)
    {
        if (gt == null) return;
        gt.AddGrenade(grenadeAmount);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
