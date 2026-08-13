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

    [Header("Rotation")]
    public float rotationSpeed = 10f;

    private float previousDistanceToTarget;

    public override void OnEpisodeBegin()
    {
        ResetForNewFight();
    }

    // Reset stanu agenta przed rozpoczęciem nowej walki.
    public void ResetForNewFight()
    {
        // Reset agenta.
        transform.localPosition =
            new Vector3(5f, 0.5f, 5f);

        if (target == null)
            return;

        // Reset Playera.
        target.localPosition =
            new Vector3(-5f, 0.5f, -5f);

        // Reset dystansu.
        previousDistanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );
    }

    public override void CollectObservations(
        VectorSensor sensor
    )
    {
        if (target == null)
        {
            sensor.AddObservation(Vector3.zero);
            return;
        }

        // Pozycja Playera względem agenta.
        Vector3 targetRelativePosition =
            target.localPosition -
            transform.localPosition;

        sensor.AddObservation(
            targetRelativePosition
        );
    }

    public override void OnActionReceived(
        ActionBuffers actions
    )
    {
        if (target == null)
            return;

        // Ruch X.
        float moveX =
            Mathf.Clamp(
                actions.ContinuousActions[0],
                -1f,
                1f
            );

        // Ruch Z.
        float moveZ =
            Mathf.Clamp(
                actions.ContinuousActions[1],
                -1f,
                1f
            );

        Vector3 movement =
            new Vector3(
                moveX,
                0f,
                moveZ
            );

        // Obracaj potwora w kierunku ruchu.
        if (movement.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(movement);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }

        // Aktualny dystans.
        float distanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );

        // Nie wchodź w Playera.
        if (distanceToTarget > attackRange)
        {
            transform.localPosition +=
                movement *
                moveSpeed *
                Time.deltaTime;
        }

        // Granice areny.
        Vector3 position =
            transform.localPosition;

        position.x =
            Mathf.Clamp(
                position.x,
                -7f,
                7f
            );

        position.z =
            Mathf.Clamp(
                position.z,
                -7f,
                7f
            );

        transform.localPosition =
            position;

        // Dystans po ruchu.
        distanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );

        // Nagroda za zbliżanie się.
        float distanceReward =
            previousDistanceToTarget -
            distanceToTarget;

        AddReward(
            distanceReward * 2f
        );

        previousDistanceToTarget =
            distanceToTarget;

        // Mała kara za upływ czasu.
        AddReward(-0.001f);

        // Nagroda za pozostawanie w zasięgu.
        if (distanceToTarget <= attackRange)
        {
            AddReward(0.001f);
        }
    }

    public override void Heuristic(
        in ActionBuffers actionsOut
    )
    {
        var actions =
            actionsOut.ContinuousActions;

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