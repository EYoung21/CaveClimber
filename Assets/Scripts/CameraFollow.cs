using UnityEngine;
using UnityEngine.UI;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;
    public Text scoreText;

    void LateUpdate()
    {
        // Only follow player upward (Doodle Jump style)
        if (target.position.y > transform.position.y)
        {
            Vector3 newPos = new Vector3(transform.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, newPos, smoothSpeed);
        }
    }

    void Start()
    {
        if (scoreText == null)
            scoreText = GameObject.Find("ScoreText").GetComponent<Text>();
    }
}   