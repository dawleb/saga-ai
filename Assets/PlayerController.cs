using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;

    [Header("Animation")]
    public Animator animator;

    private Coroutine moveCoroutine;

    // The height the character uses for movement.
    private float movementHeight;

    // Prevents movement after the player dies.
    private bool isDead;

    public bool IsMoving { get; private set; }

    public bool IsDead
    {
        get { return isDead; }
    }

    private void Awake()
    {
        movementHeight = transform.position.y;
        IsMoving = false;
        isDead = false;

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }
    }

    // ====================================
    // MOVE TO
    // ====================================

    public void MoveTo(
        Vector3 targetPosition
    )
    {
        // Dead player cannot move.
        if (isDead)
        {
            return;
        }

        targetPosition.y =
            movementHeight;

        StopMovement();

        moveCoroutine =
            StartCoroutine(
                MoveToTarget(
                    targetPosition
                )
            );
    }

    // ====================================
    // SET DEAD
    // ====================================

    public void SetDead()
    {
        isDead = true;

        StopMovement();

        Debug.Log(
            "[PLAYER] PlayerController: player is dead. Movement stopped."
        );
    }

    // ====================================
    // STOP MOVEMENT
    // ====================================

    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(
                moveCoroutine
            );

            moveCoroutine = null;
        }

        IsMoving = false;

        UpdateMovementAnimation(
            Vector3.zero
        );
    }

    // ====================================
    // MOVE COROUTINE
    // ====================================

    private IEnumerator MoveToTarget(
        Vector3 targetPosition
    )
    {
        // Safety check.
        if (isDead)
        {
            yield break;
        }

        IsMoving = true;

        while (true)
        {
            // Stop immediately after death.
            if (isDead)
            {
                IsMoving = false;
                moveCoroutine = null;

                UpdateMovementAnimation(
                    Vector3.zero
                );

                yield break;
            }

            Vector3 currentPosition =
                transform.position;

            Vector3 direction =
                targetPosition -
                currentPosition;

            direction.y = 0f;

            float distance =
                direction.magnitude;

            if (distance <= 0.05f)
            {
                break;
            }

            UpdateMovementAnimation(
                direction
            );

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        direction.normalized
                    );

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed *
                        Time.deltaTime
                    );
            }

            currentPosition =
                Vector3.MoveTowards(
                    currentPosition,
                    targetPosition,
                    moveSpeed *
                    Time.deltaTime
                );

            currentPosition.y =
                targetPosition.y;

            transform.position =
                currentPosition;

            yield return null;
        }

        // Do not snap to the destination after death.
        if (isDead)
        {
            IsMoving = false;
            moveCoroutine = null;

            UpdateMovementAnimation(
                Vector3.zero
            );

            yield break;
        }

        transform.position =
            targetPosition;

        IsMoving = false;
        moveCoroutine = null;

        UpdateMovementAnimation(
            Vector3.zero
        );
    }

    // ====================================
    // UPDATE MOVEMENT ANIMATION
    // ====================================

    private void UpdateMovementAnimation(
        Vector3 worldDirection
    )
    {
        if (animator == null)
        {
            return;
        }

        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude < 0.001f)
        {
            animator.SetBool(
                "IsWalking",
                false
            );

            animator.SetFloat(
                "MoveX",
                0f
            );

            animator.SetFloat(
                "MoveY",
                0f
            );

            return;
        }

        Vector3 localDirection =
            transform.InverseTransformDirection(
                worldDirection.normalized
            );

        animator.SetBool(
            "IsWalking",
            true
        );

        animator.SetFloat(
            "MoveX",
            localDirection.x
        );

        animator.SetFloat(
            "MoveY",
            localDirection.z
        );
    }
}