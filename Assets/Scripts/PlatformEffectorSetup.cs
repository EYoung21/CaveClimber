using UnityEngine;

[RequireComponent(typeof(PlatformEffector2D))]
public class PlatformEffectorSetup : MonoBehaviour
{
    [Header("One-Way Platform Settings")]
    [Range(0, 360)]
    public float surfaceArc = 180f;
    public bool useOneWay = true;
    public bool useSideCollisionPrevention = true;
    
    private PlatformEffector2D platformEffector;
    
    void Awake()
    {
        platformEffector = GetComponent<PlatformEffector2D>();
        SetupEffector();
    }
    
    void OnValidate()
    {
        // Update in editor when values change
        if (platformEffector == null)
            platformEffector = GetComponent<PlatformEffector2D>();
            
        SetupEffector();
    }
    
    void SetupEffector()
    {
        if (platformEffector == null) return;
        
        // Configure the platform effector
        platformEffector.useOneWay = useOneWay;
        platformEffector.surfaceArc = surfaceArc;
        
        if (useSideCollisionPrevention)
        {
            // Set up to prevent side collisions (increase arc to ~270)
            platformEffector.surfaceArc = 270f;
            platformEffector.rotationalOffset = 180f;
        }
    }
} 