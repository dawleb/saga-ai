using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Rotation")]
    public float rotationSpeed = 10f;

    private Coroutine moveCoroutine;

    // Player aktualnie się porusza.
    public bool IsMoving { get; private set; }

    private void Update()
    {
        // Ruch jest sterowany przez PlayerClickController.
        // Tutaj nie używamy WASD.

        if (moveCoroutine == null)
        {
            IsMoving = false;
        }
    }

    public void MoveTo(Vector3 targetPosition)
    {
        targetPosition.y =
            transform.position.y;

        // Zatrzymaj poprzedni ruch.
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine =
            StartCoroutine(
                MoveToTarget(targetPosition)
            );
    }

    private IEnumerator MoveToTarget(
        Vector3 targetPosition
    )
    {
        IsMoving = true;

        while (true)
        {
            Vector3 direction =
                targetPosition -
                transform.position;

            direction.y = 0f;

            float distance =
                direction.magnitude;

            // Dotarliśmy do celu.
            if (distance <= 0.05f)
            {
                break;
            }

            // Obrót w kierunku ruchu.
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();

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

            // Ruch.
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed *
                    Time.deltaTime
                );

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

            yield return null;
        }

        // Ustaw dokładnie na celu.
        transform.position =
            targetPosition;

        IsMoving = false;

        moveCoroutine = null;
    }
}