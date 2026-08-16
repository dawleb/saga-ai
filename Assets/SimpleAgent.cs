using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class SimpleAgent : Agent
{
    public Transform target;
    public float moveSpeed = 1f;
    public float attackRange = 1.5f;

    [Header("Rotation")]
    public float rotationSpeed = 10f;

    private float previousDistanceToTarget;
    private Animator animator;

    public override void Initialize()
    {
        animator =
            GetComponentInChildren<Animator>();
    }

    public override void OnEpisodeBegin()
    {
        ResetForNewFight();
    }

    // Reset stanu agenta przed rozpoczęciem nowej walki.
    public void ResetForNewFight()
    {
        // Reset agenta.
        transform.localPosition =
            new Vector3(5f, 0f, 5f);

        if (animator != null)
        {
            animator.SetBool(
                "IsWalking",
                false
            );
        }

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

        // Aktualny dystans.
        float distanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );

        // --------------------------------
        // RUCH
        // --------------------------------

        // Ruch tylko jeśli jesteśmy poza
        // zasięgiem ataku.
        if (distanceToTarget > attackRange)
        {
            Vector3 oldPosition =
                transform.localPosition;

            transform.localPosition +=
                movement *
                moveSpeed *
                Time.deltaTime;

            // Obracaj tylko podczas rzeczywistego ruchu.
            if (movement.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        movement
                    );

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed *
                        Time.deltaTime
                    );
            }

            bool isActuallyMoving =
                Vector3.Distance(
                    oldPosition,
                    transform.localPosition
                ) > 0.0001f;

            if (animator != null)
            {
                animator.SetBool(
                    "IsWalking",
                    isActuallyMoving
                );
            }
        }
        else
        {
            // W zasięgu ataku:
            // zatrzymaj ruch i chodzenie.
            if (animator != null)
            {
                animator.SetBool(
                    "IsWalking",
                    false
                );
            }
        }

        // --------------------------------
        // GRANICE ARENY
        // --------------------------------

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

        // --------------------------------
        // DYSTANS PO RUCHU
        // --------------------------------

        distanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );

        // --------------------------------
        // NAGRODA ZA ZBLIŻANIE
        // --------------------------------

        float distanceReward =
            previousDistanceToTarget -
            distanceToTarget;

        AddReward(
            distanceReward * 2f
        );

        previousDistanceToTarget =
            distanceToTarget;

        // --------------------------------
        // KARA ZA CZAS
        // --------------------------------

        AddReward(-0.001f);

        // --------------------------------
        // NAGRODA ZA ZASIĘG ATAKU
        // --------------------------------

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