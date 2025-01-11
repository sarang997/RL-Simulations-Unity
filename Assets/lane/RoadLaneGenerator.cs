using UnityEngine;

public class RoadLaneGenerator : MonoBehaviour
{
    [Header("Lane Orientation")]
    [Tooltip("Which direction should the lane extend? (normalized at runtime)")]
    [SerializeField] public Vector3 laneDirection = Vector3.forward;

    [Header("Road Lane Dimensions")]
    [SerializeField] public float laneWidth = 3f;
    [SerializeField] public float laneLength = 10f;

    [Header("Lane Height/Thickness")]
    [SerializeField] public float laneHeight = 0.1f;

    [Header("Checkpoint Settings")]
    [Tooltip("How tall the checkpoint collider is (so vehicles/players intersect it)")]
    [SerializeField] public float checkpointHeight = 2f;
    [Tooltip("Check if you want the checkpoint to be a trigger collider")]
    [SerializeField] public bool isCheckpointTrigger = true;

    [Header("Optional Lane Material")]
    [SerializeField] public Material laneMaterial;

    [Header("Lane Identification")]
    [SerializeField] public string laneId;

    // Arrow Visualization Settings
    [Header("Arrow Gizmo Settings")]
    [Tooltip("Lift the arrow above the ground/lane by this amount.")]
    [SerializeField] public float arrowYOffset = 0.5f;

    [Tooltip("The angle of each side of the arrowhead")]
    [SerializeField] public float arrowHeadAngle = 30f;

    [Tooltip("How long the arrowhead lines are")]
    [SerializeField] public float arrowHeadLength = 3.0f;

    private void Awake()
    {
        // If user didn't specify a laneId, create a random one
        if (string.IsNullOrEmpty(laneId))
        {
            laneId = $"lane_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
    }

    private void Start()
    {
        // Normalize the direction
        laneDirection = laneDirection.normalized;

        // (Optional) rename this GameObject to match the laneId, if you want:
        gameObject.name = laneId;

        // 1. Create the road mesh
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "road";
        road.transform.SetParent(transform);
        road.transform.localPosition = Vector3.zero;
        road.transform.localRotation = Quaternion.identity;
        road.transform.localScale = new Vector3(laneWidth, 0.1f, laneLength);

        // Assign material if available
        Renderer roadRenderer = road.GetComponent<Renderer>();
        if (laneMaterial != null)
        {
            roadRenderer.material = laneMaterial;
        }
        else
        {
            // Load a default "Road" material if it exists in Resources
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

        // 5. Create basic walls (if desired)
        float wallHeight = 2f;
        float wallThickness = 0.1f;

        // Left Wall
        GameObject leftWall = new GameObject("wall_left");
        leftWall.transform.SetParent(transform);
        BoxCollider leftCollider = leftWall.AddComponent<BoxCollider>();
        leftCollider.size = new Vector3(wallThickness, wallHeight, laneLength);
        leftWall.tag = "Wall";
        leftWall.layer = LayerMask.NameToLayer("Wall");
        
        Vector3 leftPosition = transform.position + Vector3.Cross(laneDirection, Vector3.up) * (laneWidth / 2f);
        leftPosition.y += wallHeight / 2f;
        leftWall.transform.position = leftPosition;
        leftWall.transform.rotation = Quaternion.LookRotation(laneDirection);

        // Right Wall
        GameObject rightWall = new GameObject("wall_right");
        rightWall.transform.SetParent(transform);
        BoxCollider rightCollider = rightWall.AddComponent<BoxCollider>();
        rightCollider.size = new Vector3(wallThickness, wallHeight, laneLength);
        rightWall.tag = "Wall";
        rightWall.layer = LayerMask.NameToLayer("Wall"); 
        
        Vector3 rightPosition = transform.position - Vector3.Cross(laneDirection, Vector3.up) * (laneWidth / 2f);
        rightPosition.y += wallHeight / 2f;
        rightWall.transform.position = rightPosition;
        rightWall.transform.rotation = Quaternion.LookRotation(laneDirection);

        #if UNITY_EDITOR
            AddDebugVisual(leftWall, new Color(0.7f, 0.7f, 0.7f, 0.5f));
            AddDebugVisual(rightWall, new Color(0.7f, 0.7f, 0.7f, 0.5f));
        #endif
    }

    private GameObject CreateCheckpoint(string name, Vector3 position, Color debugColor, string tag, string layer)
    {
        GameObject checkpoint = new GameObject(name);
        checkpoint.tag = tag;
        checkpoint.layer = LayerMask.NameToLayer(layer);

        // If 'exit' or 'entrance', attach a LaneIdentifier, if you have that script
        if (name == "exit")
        {
            var exitId = checkpoint.AddComponent<LaneIdentifier>();
            exitId.SetLaneId(laneId);
        }
        if (name == "entrance")
        {
            var entranceId = checkpoint.AddComponent<LaneIdentifier>();
            entranceId.SetLaneId(laneId);
        }

        position.y += checkpointHeight / 2f;
        checkpoint.transform.position = position;
        checkpoint.transform.rotation = Quaternion.LookRotation(laneDirection);

        BoxCollider coll = checkpoint.AddComponent<BoxCollider>();
        coll.isTrigger = isCheckpointTrigger;
        coll.size = new Vector3(laneWidth, checkpointHeight, 0.1f);

        #if UNITY_EDITOR
            var debugVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugVisual.transform.SetParent(checkpoint.transform);
            debugVisual.transform.localPosition = Vector3.zero;
            debugVisual.transform.localScale = coll.size;
            debugVisual.GetComponent<Renderer>().material.color = debugColor;
            debugVisual.GetComponent<Collider>().enabled = false;
        #endif

        return checkpoint;
    }

    private void OnDrawGizmos()
    {
        Vector3 direction = laneDirection.normalized;
        Vector3 arrowStart = transform.position - direction * (laneLength * 0.5f) + Vector3.up * arrowYOffset;
        Vector3 arrowEnd   = transform.position + direction * (laneLength * 0.5f) + Vector3.up * arrowYOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(arrowStart, arrowEnd);

        Vector3 arrowDir = (arrowEnd - arrowStart).normalized;
        DrawArrowHead(arrowEnd, arrowDir);
    }

    private void DrawArrowHead(Vector3 arrowTip, Vector3 direction)
    {
        Vector3 rightHead = Quaternion.LookRotation(direction)
            * Quaternion.Euler(0, 180 + arrowHeadAngle, 0)
            * Vector3.forward;
        Vector3 leftHead = Quaternion.LookRotation(direction)
            * Quaternion.Euler(0, 180 - arrowHeadAngle, 0)
            * Vector3.forward;

        Gizmos.DrawLine(arrowTip, arrowTip + rightHead * arrowHeadLength);
        Gizmos.DrawLine(arrowTip, arrowTip + leftHead * arrowHeadLength);
    }

    #if UNITY_EDITOR
    private void AddDebugVisual(GameObject parent, Color debugColor)
    {
        GameObject debugVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugVisual.transform.SetParent(parent.transform);
        debugVisual.transform.localPosition = Vector3.zero;
        debugVisual.transform.localScale = parent.GetComponent<BoxCollider>().size;
        debugVisual.GetComponent<Renderer>().material.color = debugColor;
        debugVisual.GetComponent<Collider>().enabled = false;
    }
    #endif

    public void SetLaneLength(float length)
    {
        laneLength = length;
    }
}