using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;

    private Coroutine moveCoroutine;

    // The height the character uses for movement.
    private float movementHeight;

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        movementHeight = transform.position.y;
        IsMoving = false;
    }

    // ====================================
    // MOVE TO
    // ====================================

    public void MoveTo(Vector3 targetPosition)
    {
        targetPosition.y = movementHeight;

        StopMovement();

        moveCoroutine =
            StartCoroutine(
                MoveToTarget(targetPosition)
            );
    }

    // ====================================
    // STOP MOVEMENT
    // ====================================

    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        IsMoving = false;
    }

    // ====================================
    // MOVE COROUTINE
    // ====================================

    private IEnumerator MoveToTarget(
        Vector3 targetPosition)
    {
        IsMoving = true;

        while (true)
        {
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

            // Rotate toward the movement direction.
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

            // Move toward the target.
            currentPosition =
                Vector3.MoveTowards(
                    currentPosition,
                    targetPosition,
                    moveSpeed *
                    Time.deltaTime
                );

            // Keep the character at the fixed movement height.
            currentPosition.y =
                targetPosition.y;

            transform.position =
                currentPosition;

            yield return null;
        }

        transform.position =
            targetPosition;

        IsMoving = false;
        moveCoroutine = null;
    }
}