using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    [Header("Road Direction Settings")]
    [Tooltip("Normalized for your forward lane; the other lane is reversed.")]
    [SerializeField] public Vector3 roadDirection = Vector3.forward;

    [Header("Lane Spacing")]
    [Tooltip("Center-to-center distance between the two lanes.")]
    [SerializeField] private float laneSpacing = 3f;

    [Header("Lane Settings")]
    [Tooltip("Width of each lane")]
    [SerializeField] private float laneWidth = 3f;
    [Tooltip("Length of each lane")]
    [SerializeField] public float laneLength = 10f;
    [Tooltip("Height (thickness) of the lane road mesh")]
    [SerializeField] private float laneHeight = 0.1f;
    [Tooltip("Checkpoint collider height")]
    [SerializeField] private float checkpointHeight = 2f;
    [Tooltip("If true, checkpoint colliders are triggers")]
    [SerializeField] private bool isCheckpointTrigger = true;
    [Tooltip("Optional material for both lanes")]
    [SerializeField] private Material laneMaterial;

    [Header("Specific Lane IDs")]
    [Tooltip("If you want to override the ID for Lane #1 (forward)")]
    [SerializeField] private string laneId_Forward;

    [Tooltip("If you want to override the ID for Lane #2 (backward)")]
    [SerializeField] private string laneId_Backward;

    private void Start()
    {
        GenerateRoad();
    }

    private void GenerateRoad()
    {
        // Normalize once
        roadDirection = roadDirection.normalized;

        // Container for both lanes
        GameObject lanesParent = new GameObject("Lanes");
        lanesParent.transform.SetParent(transform, false);

        // Calculate half-offset (sideways)
        Vector3 perp = Vector3.Cross(roadDirection, Vector3.up).normalized;
        Vector3 halfOffset = perp * (laneSpacing * 0.5f);

        // ------ FORWARD (OUTGOING) LANE ------
        GameObject forwardLaneGO = new GameObject("ForwardLane");
        forwardLaneGO.transform.SetParent(lanesParent.transform, false);
        forwardLaneGO.transform.position = transform.position + halfOffset;

        RoadLaneGenerator forwardLane = forwardLaneGO.AddComponent<RoadLaneGenerator>();

        // Assign public fields
        forwardLane.laneDirection      = roadDirection;
        forwardLane.laneWidth          = laneWidth;
        forwardLane.laneLength         = laneLength;
        forwardLane.laneHeight         = laneHeight;
        forwardLane.checkpointHeight   = checkpointHeight;
        forwardLane.isCheckpointTrigger= isCheckpointTrigger;
        forwardLane.laneMaterial       = laneMaterial;

        // If user provided a custom ID for this lane, use it
        // Otherwise, RoadLaneGenerator will default to a random ID at runtime
        if (!string.IsNullOrEmpty(laneId_Forward))
        {
            forwardLane.laneId = laneId_Forward;
        }

        // ------ BACKWARD (INCOMING) LANE ------
        GameObject backwardLaneGO = new GameObject("BackwardLane");
        backwardLaneGO.transform.SetParent(lanesParent.transform, false);
        backwardLaneGO.transform.position = transform.position - halfOffset;

        RoadLaneGenerator backwardLane = backwardLaneGO.AddComponent<RoadLaneGenerator>();

        // Assign public fields
        backwardLane.laneDirection      = -roadDirection; // reversed
        backwardLane.laneWidth          = laneWidth;
        backwardLane.laneLength         = laneLength;
        backwardLane.laneHeight         = laneHeight;
        backwardLane.checkpointHeight   = checkpointHeight;
        backwardLane.isCheckpointTrigger= isCheckpointTrigger;
        backwardLane.laneMaterial       = laneMaterial;

        // If user provided a custom ID for this lane
        if (!string.IsNullOrEmpty(laneId_Backward))
        {
            backwardLane.laneId = laneId_Backward;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw arrow to show main roadDirection
        Gizmos.color = Color.blue;
        Vector3 center = transform.position;
        float arrowLen = 2f;

        Gizmos.DrawLine(center, center + roadDirection.normalized * arrowLen);

        // Arrowhead
        float headSize = 0.5f;
        Vector3 right = Quaternion.Euler(0, 30, 0) * -roadDirection;
        Vector3 left  = Quaternion.Euler(0, -30, 0) * -roadDirection;

        Gizmos.DrawLine(
            center + roadDirection.normalized * arrowLen,
            center + roadDirection.normalized * arrowLen + right.normalized * headSize
        );
        Gizmos.DrawLine(
            center + roadDirection.normalized * arrowLen,
            center + roadDirection.normalized * arrowLen + left.normalized * headSize
        );

        // Show lane spacing in yellow
        Gizmos.color = Color.yellow;
        Vector3 halfOffset = Vector3.Cross(roadDirection, Vector3.up).normalized * (laneSpacing * 0.5f);
        Gizmos.DrawLine(center + halfOffset, center - halfOffset);
    }
}