using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class SimpleAgent : Agent
{
    public Transform target;
    public float moveSpeed = 3f;

private void Start()
{
    Debug.Log("SIMPLE AGENT STARTED");
}

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // Randomize the target position at the start of each episode
        float randomX = Random.Range(-7f, 7f);
        float randomZ = Random.Range(-7f, 7f);

        target.localPosition = new Vector3(randomX, 0.5f, randomZ);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // <My position
        sensor.AddObservation(transform.localPosition);

        // Target position
        sensor.AddObservation(target.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 movement = new Vector3(moveX, 0f, moveZ);

        transform.localPosition += movement * moveSpeed * Time.deltaTime;

        // Small penalty for every step
        AddReward(-0.001f);

        // Calculate the distance to the target
        float distanceToTarget =
            Vector3.Distance(transform.localPosition, target.localPosition);

        // Reward the agent for reaching the target
        if (distanceToTarget < 1f)
        {
            AddReward(1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("HEURISTIC IS RUNNING");
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