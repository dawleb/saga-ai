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

    [Header("Height")]
    public float characterHeight = 0.5f;

    [Header("Arena")]
    public float arenaMin = -7f;
    public float arenaMax = 7f;

    private float previousDistanceToTarget;

    private Animator animator;

    private bool isDead;

    public override void Initialize()
    {
        animator =
            GetComponentInChildren<Animator>();

        isDead = false;
    }

    public override void OnEpisodeBegin()
    {
        ResetForNewFight();
    }

    public void ResetForNewFight()
    {
        isDead = false;

        // Reset monster.
        transform.localPosition =
            new Vector3(
                5f,
                characterHeight,
                5f
            );

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            animator.SetBool(
                "IsWalking",
                false
            );
        }

        if (target == null)
        {
            previousDistanceToTarget = 0f;

            return;
        }

        // Reset player.
        target.localPosition =
            new Vector3(
                -5f,
                0.5f,
                -5f
            );

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
            sensor.AddObservation(
                Vector3.zero
            );

            return;
        }

        // Player position relative to the monster.
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
        if (isDead)
        {
            StopWalking();

            return;
        }

        if (target == null)
        {
            StopWalking();

            return;
        }

        if (actions.ContinuousActions.Length < 2)
        {
            StopWalking();

            return;
        }

        // Read movement input.
        float moveX =
            Mathf.Clamp(
                actions.ContinuousActions[0],
                -1f,
                1f
            );

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

        float distanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );

        // --------------------------------
        // MOVEMENT
        // --------------------------------

        if (distanceToTarget > attackRange)
        {
            Vector3 oldPosition =
                transform.localPosition;

            transform.localPosition +=
                movement *
                moveSpeed *
                Time.deltaTime;

            // Keep monster at its configured height.
            Vector3 position =
                transform.localPosition;

            position.y =
                characterHeight;

            // Keep monster inside the arena.
            position.x =
                Mathf.Clamp(
                    position.x,
                    arenaMin,
                    arenaMax
                );

            position.z =
                Mathf.Clamp(
                    position.z,
                    arenaMin,
                    arenaMax
                );

            transform.localPosition =
                position;

            bool isActuallyMoving =
                Vector3.Distance(
                    oldPosition,
                    transform.localPosition
                ) > 0.0001f;

            // Rotate only while actually moving.
            if (isActuallyMoving)
            {
                Vector3 direction =
                    transform.localPosition -
                    oldPosition;

                direction.y = 0f;

                if (direction.sqrMagnitude > 0.000001f)
                {
                    Quaternion targetRotation =
                        Quaternion.LookRotation(
                            direction
                        );

                    transform.rotation =
                        Quaternion.Slerp(
                            transform.rotation,
                            targetRotation,
                            rotationSpeed *
                            Time.deltaTime
                        );
                }
            }

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
            StopWalking();
        }

        // --------------------------------
        // ARENA LIMITS
        // --------------------------------

        Vector3 finalPosition =
            transform.localPosition;

        finalPosition.y =
            characterHeight;

        finalPosition.x =
            Mathf.Clamp(
                finalPosition.x,
                arenaMin,
                arenaMax
            );

        finalPosition.z =
            Mathf.Clamp(
                finalPosition.z,
                arenaMin,
                arenaMax
            );

        transform.localPosition =
            finalPosition;

        // --------------------------------
        // DISTANCE AFTER MOVEMENT
        // --------------------------------

        distanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );

        // --------------------------------
        // APPROACH REWARD
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
        // TIME PENALTY
        // --------------------------------

        AddReward(
            -0.001f
        );

        // --------------------------------
        // ATTACK RANGE REWARD
        // --------------------------------

        if (distanceToTarget <= attackRange)
        {
            AddReward(
                0.001f
            );
        }
    }

    private void StopWalking()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(
            "IsWalking",
            false
        );
    }

    // Called by CombatManager when the monster dies.
    public void SetDead()
    {
        isDead = true;

        StopWalking();
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
        {
            return;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            actions[0] = -1f;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            actions[0] = 1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            actions[1] = -1f;
        }

        if (Keyboard.current.wKey.isPressed)
        {
            actions[1] = 1f;
        }
    }
}