using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;

    private Coroutine moveCoroutine;

    // The height the character walks at, captured once at startup.
    // Reading transform.position.y on every click meant that any drift in Y
    // was baked into the next move, so the error grew click after click.
    private float movementHeight;

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        movementHeight = transform.position.y;
    }

    private void Update()
    {
        if (moveCoroutine == null)
            IsMoving = false;
    }

    public void MoveTo(Vector3 targetPosition)
    {
        // Zachowujemy aktualną wysokość w WORLD SPACE.
        targetPosition.y = movementHeight;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

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
            Vector3 currentPosition =
                transform.position;

            Vector3 direction =
                targetPosition -
                currentPosition;

            // Ruch tylko po X/Z.
            direction.y = 0f;

            float distance =
                direction.magnitude;

            if (distance <= 0.05f)
                break;

            // Obrót w kierunku ruchu.
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

            // Ruch.
            currentPosition =
                Vector3.MoveTowards(
                    currentPosition,
                    targetPosition,
                    moveSpeed *
                    Time.deltaTime
                );

            // Nigdy nie zmieniaj wysokości.
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