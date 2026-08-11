using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class SimpleAgent : Agent
{
    public Transform target;
    public float moveSpeed = 3f;

    private float previousDistanceToTarget;

    public override void OnEpisodeBegin()
    {
        // Reset agent
        transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // Randomize target
        float randomX = Random.Range(-7f, 7f);
        float randomZ = Random.Range(-7f, 7f);

        target.localPosition = new Vector3(randomX, 0.5f, randomZ);

        // Initial distance
        previousDistanceToTarget =
            Vector3.Distance(transform.localPosition, target.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target position relative to agent
        Vector3 targetRelativePosition =
            target.localPosition - transform.localPosition;

        sensor.AddObservation(targetRelativePosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get actions
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector3 movement = new Vector3(moveX, 0f, moveZ);

        // Move agent
        transform.localPosition += movement * moveSpeed * Time.deltaTime;

        // Keep agent inside training area
        Vector3 position = transform.localPosition;

        position.x = Mathf.Clamp(position.x, -7f, 7f);
        position.z = Mathf.Clamp(position.z, -7f, 7f);

        transform.localPosition = position;

        // Current distance
        float distanceToTarget =
            Vector3.Distance(transform.localPosition, target.localPosition);

        // Reward progress toward target
        float distanceReward =
            previousDistanceToTarget - distanceToTarget;

        AddReward(distanceReward * 2f);

        // Save distance for next step
        previousDistanceToTarget = distanceToTarget;

        // Small time penalty
        AddReward(-0.001f);

        // Reached target
        if (distanceToTarget < 1f)
        {
            AddReward(1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;

        actions[0] = 0f;
        actions[1] = 0f;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.aKey.isPressed)
            actions[0] = -1f;

        if (Keyboard.current.dKey.isPressed)
            actions[0] = 1f;

        if (Keyboard.current.sKey.isPressed)
            actions[1] = -1f;

        if (Keyboard.current.wKey.isPressed)
            actions[1] = 1f;
    }
}
