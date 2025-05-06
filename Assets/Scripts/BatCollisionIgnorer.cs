using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BatCollisionIgnorer : MonoBehaviour
{
    [Tooltip("Assign the main physical collider of the parent Bat object here.")]
    public Collider2D mainBatCollider; 

    [Tooltip("Select the layers the bat should ignore collisions with (e.g., Ground, Enemy).")]
    public LayerMask layersToIgnore;

    private Collider2D triggerCollider;

    void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("BatCollisionIgnorer's Collider2D MUST be set to 'Is Trigger'. Forcing it now.", gameObject);
            triggerCollider.isTrigger = true;
        }

        if (mainBatCollider == null)
        {
            Debug.LogError("BatCollisionIgnorer requires 'Main Bat Collider' to be assigned in the Inspector!", gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the main collider reference is set and the other collider exists
        if (mainBatCollider == null || other == null)
        {
            return; 
        }

        // Check if the layer of the object we entered is in the layersToIgnore mask
        if (((1 << other.gameObject.layer) & layersToIgnore) != 0)
        {
            // Tell Physics2D to ignore collisions between the main bat collider and the other collider
            Physics2D.IgnoreCollision(mainBatCollider, other, true);
            // Optional: Log which collision is being ignored
            // Debug.Log($"Bat proactively ignoring collision between {mainBatCollider.name} and {other.name}");
        }
    }

    // Optional: If you ever need the bat to collide with these layers again later, 
    // you might implement OnTriggerExit2D to set IgnoreCollision back to false.
    // For simply passing through, this is likely not needed.
    // void OnTriggerExit2D(Collider2D other)
    // {
    //     if (mainBatCollider != null && other != null && ((1 << other.gameObject.layer) & layersToIgnore) != 0)
    //     {
    //         Physics2D.IgnoreCollision(mainBatCollider, other, false);
    //         // Debug.Log($"Bat re-enabling collision between {mainBatCollider.name} and {other.name}");
    //     }
    // }
} 