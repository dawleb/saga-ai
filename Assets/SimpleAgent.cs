using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class SimpleAgent : Agent
{
    public Transform target;
    public float moveSpeed = 3f;
    public float attackRange = 1.5f;

    private float previousDistanceToTarget;

    public override void OnEpisodeBegin()
    {
        // Reset agent position.
        transform.localPosition = new Vector3(5f, 0.5f, 5f);

        // Reset player position.
        target.localPosition = new Vector3(-5f, 0.5f, -5f);

        // Initial distance.
        previousDistanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Observe target position relative to agent.
        Vector3 targetRelativePosition =
            target.localPosition - transform.localPosition;

        sensor.AddObservation(targetRelativePosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get movement actions.
        float moveX = Mathf.Clamp(
            actions.ContinuousActions[0],
            -1f,
            1f
        );

        float moveZ = Mathf.Clamp(
            actions.ContinuousActions[1],
            -1f,
            1f
        );

        Vector3 movement = new Vector3(
            moveX,
            0f,
            moveZ
        );

        // Calculate current distance to target.
        float distanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );

        // Move toward the target only when outside attack range.
        if (distanceToTarget > attackRange)
        {
            transform.localPosition +=
                movement * moveSpeed * Time.deltaTime;
        }

        // Keep agent inside training area.
        Vector3 position = transform.localPosition;

        position.x = Mathf.Clamp(position.x, -7f, 7f);
        position.z = Mathf.Clamp(position.z, -7f, 7f);

        transform.localPosition = position;

        // Calculate distance after movement.
        distanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );

        // Reward progress toward target.
        float distanceReward =
            previousDistanceToTarget - distanceToTarget;

        AddReward(distanceReward * 2f);

        // Save distance for next step.
        previousDistanceToTarget = distanceToTarget;

        // Small time penalty.
        AddReward(-0.001f);

        // Reward reaching attack range.
        if (distanceToTarget <= attackRange)
        {
            AddReward(0.001f);
        }
    }

    public override void Heuristic(
        in ActionBuffers actionsOut
    )
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