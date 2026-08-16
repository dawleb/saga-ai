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

    // ====================================
    // RESET
    // ====================================

    public void ResetForNewFight()
    {
        isDead = false;

        // --------------------------------
        // RESET MONSTER
        // --------------------------------
        //
        // WAŻNE:
        // Nie ustawiamy tutaj Y = 0.5.
        // Zachowujemy wysokość ustawioną
        // przez scenę / fizykę.
        //

        Vector3 monsterPosition =
            transform.localPosition;

        monsterPosition.x = 5f;
        monsterPosition.z = 5f;

        transform.localPosition =
            monsterPosition;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            animator.applyRootMotion = false;

            animator.SetBool(
                "IsWalking",
                false
            );
        }

        // --------------------------------
        // RESET PLAYER
        // --------------------------------

        if (target == null)
        {
            previousDistanceToTarget = 0f;

            return;
        }

        Vector3 playerPosition =
            target.localPosition;

        playerPosition.x = -5f;
        playerPosition.z = -5f;

        // Nie zmieniamy Y Playera.
        target.localPosition =
            playerPosition;

        previousDistanceToTarget =
            Vector3.Distance(
                transform.localPosition,
                target.localPosition
            );
    }

    // ====================================
    // OBSERVATIONS
    // ====================================

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

        Vector3 targetRelativePosition =
            target.localPosition -
            transform.localPosition;

        sensor.AddObservation(
            targetRelativePosition
        );
    }

    // ====================================
    // ACTION
    // ====================================

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

        // --------------------------------
        // MOVEMENT INPUT
        // --------------------------------

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

            // Poruszamy tylko X/Z.
            // Y zostaje nietknięte.
            transform.localPosition +=
                movement *
                moveSpeed *
                Time.deltaTime;

            // --------------------------------
            // ARENA LIMITS
            // --------------------------------

            Vector3 position =
                transform.localPosition;

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

            // --------------------------------
            // CHECK ACTUAL MOVEMENT
            // --------------------------------

            bool isActuallyMoving =
                Vector3.Distance(
                    oldPosition,
                    transform.localPosition
                ) > 0.0001f;

            // --------------------------------
            // ROTATION
            // --------------------------------

            if (isActuallyMoving)
            {
                Vector3 direction =
                    transform.localPosition -
                    oldPosition;

                direction.y = 0f;

                if (direction.sqrMagnitude >
                    0.000001f)
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

            // --------------------------------
            // WALK ANIMATION
            // --------------------------------

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
        //
        // Tylko X/Z.
        // Nie dotykamy Y.
        //

        Vector3 finalPosition =
            transform.localPosition;

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

    // ====================================
    // STOP WALKING
    // ====================================

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

    // ====================================
    // DEATH
    // ====================================

    // Called by CombatManager when
    // the monster dies.
    public void SetDead()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        StopWalking();

        // --------------------------------
        // IMPORTANT
        // --------------------------------
        //
        // Nie ustawiamy tutaj Y.
        //
        // Wcześniej było:
        //
        // position.y = characterHeight;
        //
        // co wymuszało Y = 0.5.
        //
        // Teraz Zombie pozostaje dokładnie
        // na wysokości, na której znajdowało
        // się w momencie śmierci.
        //

        Vector3 position =
            transform.localPosition;

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

        // --------------------------------
        // STOP ROOT MOTION
        // --------------------------------

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        Debug.Log(
            $"[AGENT] Monster death position: " +
            $"{transform.localPosition}"
        );
    }

    // ====================================
    // HEURISTIC
    // ====================================

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