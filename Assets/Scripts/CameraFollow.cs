using UnityEngine;
using UnityEngine.UI;
using TMPro; // Use TMPro namespace if you used it for scoreText

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset; // Z offset is important, X/Y can be 0 if centering
    // public TextMeshProUGUI scoreText; // ScoreText handled by GameManager now
    
    private bool canFollow = false; // Flag to control following
    private Camera mainCamera;

    void Start()
    {
        // Cache the camera
        mainCamera = Camera.main;
        
        // Optional: Reset camera position relative to player start if needed
        // transform.position = target.position + offset;
    }

    void LateUpdate()
    {
        if (canFollow && target != null)
        {
            // Calculate the potential new Y position based on target + offset
            float desiredY = target.position.y + offset.y;

            // --- KEY CHANGE: Only move up --- 
            // Check if the desired Y position is higher than the current camera Y position
            if (desiredY > transform.position.y)
            {
                // Calculate the full desired position using the higher Y
                Vector3 desiredPosition = new Vector3(transform.position.x, desiredY, transform.position.z); 
                
                // Smoothly move towards the desired position
                transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed); 
            }
            // --- End Key Change ---
        }
    }
    
    // Public method to enable following
    public void StartFollowing()
    {
        if (canFollow) return; // Already following
        
        canFollow = true;
        Debug.Log("Camera started following player.");
        
        // --- KEY CHANGE: Immediately snap camera Y to player Y + offset Y --- 
        if (target != null)
        {
            Vector3 currentPos = transform.position;
            currentPos.y = target.position.y + offset.y; // Adjust Y based on player + offset
            transform.position = currentPos; // Apply the snap
            Debug.Log($"Camera Y snapped to: {currentPos.y}");
        }
        // --- End of Key Change ---
    }
    
    // Removed Start method related to finding scoreText
}   