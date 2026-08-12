using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 3f;

    private void Update()
    {
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
}