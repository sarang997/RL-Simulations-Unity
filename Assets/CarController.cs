using UnityEngine;
using Unity.MLAgents.Policies; // Optional, if you want to detect Heuristic mode.

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float groundHeight = 0.5f;

    private Rigidbody rb;
    [SerializeField] private BehaviorParameters behaviorParameters;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody found on car!");
            return;
        }

        // Constrain "car" so it only moves in XZ plane
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Snap it to a certain Y so it's on the ground
        Vector3 startPos = transform.position;
        transform.position = new Vector3(startPos.x, groundHeight, startPos.z);
    }

    /// <summary>
    /// The method used by the Agent (or manually in Heuristic) to set movement.
    /// </summary>
    public void Move(Vector3 movement)
    {
        rb.linearVelocity = movement * moveSpeed;
    }

    void FixedUpdate()
    {
        // Optional: If you want manual WASD control in Heuristic mode
        if (behaviorParameters != null &&
            behaviorParameters.BehaviorType == BehaviorType.HeuristicOnly)
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput   = Input.GetAxis("Vertical");
            Vector3 movement      = new Vector3(horizontalInput, 0f, verticalInput).normalized;
            Move(movement);
        }
    }
}