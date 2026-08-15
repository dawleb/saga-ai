using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastMoveMultiplier = 2f;

    [Header("Zoom")]
    public float zoomSpeed = 8f;
    public float zoomSmoothness = 12f;
    public float minHeight = 4f;
    public float maxHeight = 20f;

    [Header("Rotation")]
    public float rotationSpeed = 0.15f;
    public float rotationSmoothness = 12f;

    [Header("Camera Angle")]
    public float minPitch = 25f;
    public float maxPitch = 75f;

    private float currentPitch;
    private float currentYaw;

    private float targetZoom;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;

        currentPitch = angles.x;
        currentYaw = angles.y;

        targetZoom = transform.position.y;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null)
            return;

        Vector3 movement = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            movement += transform.forward;

        if (Keyboard.current.sKey.isPressed)
            movement -= transform.forward;

        if (Keyboard.current.dKey.isPressed)
            movement += transform.right;

        if (Keyboard.current.aKey.isPressed)
            movement -= transform.right;

        movement.y = 0f;

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        float speed = moveSpeed;

        if (Keyboard.current.leftShiftKey.isPressed)
            speed *= fastMoveMultiplier;

        transform.position +=
            movement *
            speed *
            Time.deltaTime;
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll =
            Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetZoom -=
                scroll *
                zoomSpeed;
        }

        targetZoom =
            Mathf.Clamp(
                targetZoom,
                minHeight,
                maxHeight
            );

        float newHeight =
            Mathf.Lerp(
                transform.position.y,
                targetZoom,
                zoomSmoothness *
                Time.deltaTime
            );

        Vector3 position =
            transform.position;

        position.y =
            newHeight;

        transform.position =
            position;
    }

    private void HandleRotation()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.rightButton.isPressed)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        currentYaw +=
            mouseDelta.x *
            rotationSpeed;

        currentPitch -=
            mouseDelta.y *
            rotationSpeed;

        currentPitch =
            Mathf.Clamp(
                currentPitch,
                minPitch,
                maxPitch
            );

        Quaternion targetRotation =
            Quaternion.Euler(
                currentPitch,
                currentYaw,
                0f
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSmoothness *
                Time.deltaTime
            );
    }
}