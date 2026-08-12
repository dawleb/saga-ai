using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 3f;

    private Coroutine moveCoroutine;

    private void Update()
    {
        // Keyboard movement is kept for testing.
        // Mouse movement is handled by PlayerClickController.

        if (Keyboard.current == null)
            return;

        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current.aKey.isPressed)
            moveX = -1f;

        if (Keyboard.current.dKey.isPressed)
            moveX = 1f;

        if (Keyboard.current.sKey.isPressed)
            moveZ = -1f;

        if (Keyboard.current.wKey.isPressed)
            moveZ = 1f;

        Vector3 movement = new Vector3(moveX, 0f, moveZ);

        transform.localPosition +=
            movement * moveSpeed * Time.deltaTime;

        Vector3 position = transform.localPosition;

        position.x = Mathf.Clamp(position.x, -7f, 7f);
        position.z = Mathf.Clamp(position.z, -7f, 7f);

        transform.localPosition = position;
    }

    public void MoveTo(Vector3 targetPosition)
    {
        targetPosition.y = transform.position.y;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveToTarget(targetPosition));
    }

    private System.Collections.IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;
        moveCoroutine = null;
    }
}