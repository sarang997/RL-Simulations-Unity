using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

/// <summary>
/// A more advanced agent that navigates toward a Target while avoiding Walls.
/// Provides step penalty, distance-based reward shaping, and a timeout penalty.
/// </summary>
[RequireComponent(typeof(CarController))]
public class RethoughtAgent : Agent
{
    [Header("Scene References")]
    public GameObject target;
    public GameObject ground;

    private CarController carController;
    private BehaviorParameters behaviorParameters;

    [Header("Spawn Settings")]
    public float spawnMargin = 2f;      // Margin to avoid walls
    public float checkRadius = 0.5f;    // Overlap check radius
    public LayerMask obstacleLayer;     // For walls/obstacles
    public int maxSpawnTries = 100;     // Retries for random spawn

    [Header("Reward Settings")]
    [Tooltip("Small negative reward each step to discourage 'doing nothing' forever.")]
    public float stepPenalty = -0.001f;

    [Tooltip("Reward scale for moving closer to target each step.")]
    public float distanceRewardScale = 0.01f;

    [Tooltip("Extra penalty if agent's velocity is below this threshold (reduces jitter).")]
    public float idleVelocityThreshold = 0.1f;
    public float idlePenalty = -0.001f;

    [Tooltip("Penalty if the episode times out (agent neither hits target nor wall).")]
    public float timeOutPenalty = -10f;

    // Track final reward from the last episode for printing
    private float lastEpisodeReward = 0f;

    // Track the distance from agent to target at the previous step for reward shaping
    private float prevDistanceToTarget;

    public override void Initialize()
    {
        carController = GetComponent<CarController>();
        behaviorParameters = GetComponent<BehaviorParameters>();
    }

    public override void OnEpisodeBegin()
    {
        // Print the total reward from the *previous* episode
        Debug.Log($"Episode ended with net reward: {lastEpisodeReward}");

        // Randomly place the Agent
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        Bounds bounds = groundRenderer.bounds;
        Vector3 agentPos = GetRandomPositionOnGround(bounds);
        transform.position = agentPos;

        // Randomly place the Target, ensuring some distance from the agent
        if (target != null)
        {
            Vector3 targetPos;
            int safetyCounter = 0;
            do
            {
                targetPos = GetRandomPositionOnGround(bounds);
                safetyCounter++;
                if (safetyCounter > maxSpawnTries) break;
            }
            while (Vector3.Distance(agentPos, targetPos) < 5f);

            target.transform.position = targetPos;
        }

        // Reset any velocity
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Initialize distance for reward shaping
        if (target != null)
        {
            prevDistanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        }
    }

    private Vector3 GetRandomPositionOnGround(Bounds bounds)
    {
        for (int i = 0; i < maxSpawnTries; i++)
        {
            float x = Random.Range(bounds.min.x + spawnMargin, bounds.max.x - spawnMargin);
            float z = Random.Range(bounds.min.z + spawnMargin, bounds.max.z - spawnMargin);
            float y = transform.position.y;  // Typically the same y as the agent

            Vector3 candidate = new Vector3(x, y, z);

            // Check for overlap with obstacles
            bool overlap = Physics.CheckSphere(candidate, checkRadius, obstacleLayer);
            if (!overlap)
            {
                return candidate;
            }
        }

        Debug.LogWarning("Failed to find valid random position! Returning center of ground.");
        return bounds.center;
    }

public override void CollectObservations(VectorSensor sensor)
{
    // Only observe the agent's own velocity
    if (TryGetComponent<Rigidbody>(out Rigidbody rb))
    {
        sensor.AddObservation(rb.linearVelocity.x);
        sensor.AddObservation(rb.linearVelocity.z);
    }
    else
    {
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
    }

    // Remove target position observations
}

    public override void OnActionReceived(ActionBuffers actions)
    {
        // If the agent is not in HeuristicOnly mode, apply the ML actions
        if (behaviorParameters == null || 
            behaviorParameters.BehaviorType != BehaviorType.HeuristicOnly)
        {
            float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
            float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

            Vector3 movement = new Vector3(moveX, 0f, moveZ).normalized;
            carController.Move(movement);
        }

        // -----------------------------
        // 1) Small negative step penalty
        // -----------------------------
        AddReward(stepPenalty);

        // -----------------------------------------------------
        // 2) Distance-based reward shaping (move closer = good)
        // -----------------------------------------------------
        if (target != null)
        {
            float currentDistanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            float distanceDiff = prevDistanceToTarget - currentDistanceToTarget; // + if we got closer
            AddReward(distanceDiff * distanceRewardScale);

            // Update for next step
            prevDistanceToTarget = currentDistanceToTarget;
        }

        // ----------------------------------
        // 3) Idle penalty if velocity is low
        // ----------------------------------
        if (TryGetComponent<Rigidbody>(out Rigidbody myRb))
        {
            if (myRb.linearVelocity.magnitude < idleVelocityThreshold)
            {
                AddReward(idlePenalty);
            }
        }

        // ----------------------------------
        // 4) Timeout penalty if step is max
        // ----------------------------------
        if (StepCount >= MaxStep - 1 && MaxStep > 0)
        {
            // We reached the end of the episode without hitting target or wall
            AddReward(timeOutPenalty);
            lastEpisodeReward = GetCumulativeReward();
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            AddReward(10f);
            lastEpisodeReward = GetCumulativeReward();
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-10f);
            lastEpisodeReward = GetCumulativeReward();
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Simple manual control via WASD/Arrow Keys
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetAxis("Horizontal");
        cont[1] = Input.GetAxis("Vertical");
    }
}