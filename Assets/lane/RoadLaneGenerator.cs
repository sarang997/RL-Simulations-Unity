using UnityEngine;
public class RoadLaneGenerator : MonoBehaviour
{
    [Header("Lane Orientation")]
    [Tooltip("Which direction should the lane extend? (normalized at runtime)")]
    [SerializeField] private Vector3 laneDirection = Vector3.forward;

    [Header("Road Lane Dimensions")]
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float laneLength = 10f;

    [Header("Lane Height/Thickness")]
    [SerializeField] private float laneHeight = 0.1f;

    [Header("Checkpoint Settings")]
    [Tooltip("How tall the checkpoint collider is (so vehicles/players intersect it)")]
    [SerializeField] private float checkpointHeight = 2f;
    [Tooltip("Check if you want the checkpoint to be a trigger collider")]
    [SerializeField] private bool isCheckpointTrigger = true;

    [Header("Optional Lane Material")]
    [SerializeField] private Material laneMaterial;

    [Header("Lane Identification")]
    [SerializeField] private string laneId;

    // Arrow Visualization Settings
    [Header("Arrow Gizmo Settings")]
    [Tooltip("Lift the arrow above the ground/lane by this amount.")]
    [SerializeField] private float arrowYOffset = 0.5f;

    [Tooltip("The angle of each side of the arrowhead")]
    [SerializeField] private float arrowHeadAngle = 30f;

    [Tooltip("How long the arrowhead lines are")]
    [SerializeField] private float arrowHeadLength = 3.0f;

    private void Awake()
    {
        // Generate a random lane ID if not already set
        if (string.IsNullOrEmpty(laneId))
        {
            laneId = $"lane_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
    }

    private void Start()
    {
        // Normalize the direction
        laneDirection = laneDirection.normalized;

        // Rename the current GameObject to match the lane_id
        gameObject.name = laneId;

        // 1. Create and setup the road mesh with proper naming
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "road";
        road.transform.SetParent(transform);
        road.transform.localPosition = Vector3.zero;
        road.transform.localRotation = Quaternion.identity;
        road.transform.localScale = new Vector3(laneWidth, 0.1f, laneLength);

        // Update material assignment
        Renderer roadRenderer = road.GetComponent<Renderer>();
        if (laneMaterial != null)
        {
            roadRenderer.material = laneMaterial;
        }
        else
        {
            // Set default road material
            roadRenderer.material = Resources.Load<Material>("Road");
        }

        // 2. Create Entrance Checkpoint
        GameObject entranceCheckpoint = CreateCheckpoint("entrance", 
            transform.position - (laneDirection * (laneLength / 2f)), 
            new Color(0f, 1f, 0f, 0.5f),
            "Checkpoint",
            "Checkpoint");
        entranceCheckpoint.transform.SetParent(transform);

        // 3. Create Reward Checkpoints
        float spacing = laneLength / 4f;
        for (int i = 1; i <= 3; i++)
        {
            float distanceFromStart = (spacing * i) - (laneLength / 2f);
            GameObject rewardCheckpoint = CreateCheckpoint($"reward_{i}", 
                transform.position + (laneDirection * distanceFromStart), 
                new Color(1f, 1f, 0f, 0.5f),
                "Checkpoint",
                "Checkpoint");
            rewardCheckpoint.transform.SetParent(transform);
        }

        // 4. Create Exit Checkpoint
        GameObject exitCheckpoint = CreateCheckpoint("exit", 
            transform.position + (laneDirection * (laneLength / 2f)), 
            new Color(1f, 0f, 0f, 0.5f),
            "WrongCheckpoint",
            "WrongCheckpoint");
        exitCheckpoint.transform.SetParent(transform);

        // 5. Create Wall Colliders
        float wallHeight = 2f; // You might want to make this configurable via SerializeField
        float wallThickness = 0.1f;

        // Left Wall
        GameObject leftWall = new GameObject("wall_left");
        leftWall.transform.SetParent(transform);
        BoxCollider leftCollider = leftWall.AddComponent<BoxCollider>();
        leftCollider.size = new Vector3(wallThickness, wallHeight, laneLength);
        leftWall.tag = "Wall";  // Add wall tag
        leftWall.layer = LayerMask.NameToLayer("Wall"); // Add wall layer
        
        // Position left wall along the lane
        Vector3 leftPosition = transform.position + Vector3.Cross(laneDirection, Vector3.up) * (laneWidth / 2f);
        leftPosition.y += wallHeight / 2f;
        leftWall.transform.position = leftPosition;
        leftWall.transform.rotation = Quaternion.LookRotation(laneDirection);

        // Right Wall
        GameObject rightWall = new GameObject("wall_right");
        rightWall.transform.SetParent(transform);
        BoxCollider rightCollider = rightWall.AddComponent<BoxCollider>();
        rightCollider.size = new Vector3(wallThickness, wallHeight, laneLength);
        rightWall.tag = "Wall";  // Add wall tag
        rightWall.layer = LayerMask.NameToLayer("Wall"); // Add wall layer
        
        // Position right wall along the lane
        Vector3 rightPosition = transform.position - Vector3.Cross(laneDirection, Vector3.up) * (laneWidth / 2f);
        rightPosition.y += wallHeight / 2f;
        rightWall.transform.position = rightPosition;
        rightWall.transform.rotation = Quaternion.LookRotation(laneDirection);

        // Optional: Add visual representation for debugging
        #if UNITY_EDITOR
            AddDebugVisual(leftWall, new Color(0.7f, 0.7f, 0.7f, 0.5f));  // Light gray
            AddDebugVisual(rightWall, new Color(0.7f, 0.7f, 0.7f, 0.5f)); // Light gray
        #endif
    }

    // Updated helper method with layer parameter
    private GameObject CreateCheckpoint(string name, Vector3 position, Color debugColor, string tag, string layer)
    {
        GameObject checkpoint = new GameObject(name);
        checkpoint.tag = tag;
        checkpoint.layer = LayerMask.NameToLayer(layer);
        
        // Add LaneIdentifier component to exit checkpoint
        if (name == "exit")
        {
            var ExitIdentifier = checkpoint.AddComponent<LaneIdentifier>();
            ExitIdentifier.SetLaneId(laneId);
        }
                // Add LaneIdentifier component to exit checkpoint
        if (name == "entrance")
        {
            var EntranceIdentifier = checkpoint.AddComponent<LaneIdentifier>();
            EntranceIdentifier.SetLaneId(laneId);
        }

        // Position the checkpoint
        position.y += checkpointHeight / 2f;
        checkpoint.transform.position = position;
        checkpoint.transform.rotation = Quaternion.LookRotation(laneDirection);

        // Add a BoxCollider
        BoxCollider checkpointCollider = checkpoint.AddComponent<BoxCollider>();
        checkpointCollider.isTrigger = isCheckpointTrigger;
        checkpointCollider.size = new Vector3(laneWidth, checkpointHeight, 0.1f);

        // Add visual representation for debugging
        #if UNITY_EDITOR
            var debugVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugVisual.transform.SetParent(checkpoint.transform);
            debugVisual.transform.localPosition = Vector3.zero;
            debugVisual.transform.localScale = checkpointCollider.size;
            debugVisual.GetComponent<Renderer>().material.color = debugColor;
            debugVisual.GetComponent<Collider>().enabled = false;
        #endif

        return checkpoint;
    }

    /// <summary>
    /// Draw a Gizmo arrow in the Scene View from one end of the lane to the other.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Use inspector values for direction if not yet playing
        Vector3 direction = laneDirection.normalized;

        // Calculate the near end (entrance) and far end (exit) of the lane.
        // The lane is centered at transform.position, so we go +/- half the lane length in 'direction'.
        Vector3 arrowStart = transform.position - direction * (laneLength * 0.5f) + Vector3.up * arrowYOffset;
        Vector3 arrowEnd   = transform.position + direction * (laneLength * 0.5f) + Vector3.up * arrowYOffset;

        // Draw main line of the arrow
        Gizmos.color = Color.green;
        Gizmos.DrawLine(arrowStart, arrowEnd);

        // Calculate the direction of the arrow for the arrowhead
        Vector3 arrowDir = (arrowEnd - arrowStart).normalized;

        // Draw arrowhead at the far end
        DrawArrowHead(arrowEnd, arrowDir);
    }

    /// <summary>
    /// Helper to draw a larger arrowhead at the end of the arrow.
    /// </summary>
    private void DrawArrowHead(Vector3 arrowTip, Vector3 direction)
    {
        // We'll rotate the direction vector +/- arrowHeadAngle around Y to form the arrowhead
        Vector3 rightHead = Quaternion.LookRotation(direction)
            * Quaternion.Euler(0, 180 + arrowHeadAngle, 0)
            * Vector3.forward;

        Vector3 leftHead = Quaternion.LookRotation(direction)
            * Quaternion.Euler(0, 180 - arrowHeadAngle, 0)
            * Vector3.forward;

        // Draw each side of the arrowhead
        Gizmos.DrawLine(arrowTip, arrowTip + rightHead * arrowHeadLength);
        Gizmos.DrawLine(arrowTip, arrowTip + leftHead * arrowHeadLength);
    }

    // Add this new helper method for debug visuals
    private void AddDebugVisual(GameObject parent, Color debugColor)
    {
        var debugVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugVisual.transform.SetParent(parent.transform);
        debugVisual.transform.localPosition = Vector3.zero;
        debugVisual.transform.localScale = parent.GetComponent<BoxCollider>().size;
        debugVisual.GetComponent<Renderer>().material.color = debugColor;
        debugVisual.GetComponent<Collider>().enabled = false;
    }

    public void SetLaneLength(float length)
    {
        laneLength = length;
    }
}  