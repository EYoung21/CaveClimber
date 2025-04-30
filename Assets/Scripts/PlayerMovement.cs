// This class is no longer used. All movement and animation logic has been 
// moved to the PlayerController script for better integration with Doodle Jump mechanics.
// See PlayerController.cs for the current implementation.

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Keeping this script for backward compatibility but marking it as deprecated
    [HideInInspector] 
    public float speed;

    private void Start()
    {
        Debug.LogWarning("PlayerMovement class is deprecated. All functionality has been moved to PlayerController.");
        // Disable this component
        this.enabled = false;
    }
}