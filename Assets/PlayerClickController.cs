using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerClickController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public PlayerController playerController;
    public GameObject selectionRing;

    [Header("Selection")]
    public GameObject selectionSquare;

    [Header("Animation")]
    public Animator animator;

    private bool isSelected;

    public bool IsSelected
    {
        get
        {
            return isSelected;
        }
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        SetSelected(false);
    }

    private void Update()
    {
        UpdateAnimation();

        if (Mouse.current == null)
        {
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (mainCamera == null)
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            100f
        ))
        {
            return;
        }

        Debug.Log(
            $"[PLAYER] Click hit: {hit.collider.name}"
        );

        if (hit.collider.transform.IsChildOf(transform))
        {
            SetSelected(true);

            Debug.Log(
                "[PLAYER] Player selected"
            );

            return;
        }

        if (!isSelected)
        {
            Debug.Log(
                "[PLAYER] Player is not selected. " +
                "Click ignored."
            );

            return;
        }

        Health targetHealth =
            hit.collider.GetComponentInParent<Health>();

        if (targetHealth != null &&
            targetHealth.gameObject != gameObject)
        {
            MoveToEnemy(targetHealth);

            return;
        }

        MoveToGround(hit.point);
    }

    private void UpdateAnimation()
    {
        if (animator != null &&
            playerController != null)
        {
            animator.SetBool(
                "IsWalking",
                playerController.IsMoving
            );
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionSquare != null)
        {
            selectionSquare.SetActive(selected);

            if (selected)
            {
                selectionSquare.transform.localPosition =
                    new Vector3(
                        0f,
                        0.03f,
                        0f
                    );
            }
        }

        Debug.Log(
            selected
                ? $"[SELECTION] {name} selected"
                : $"[SELECTION] {name} deselected"
        );
    }

    public void MoveToGround(Vector3 targetPosition)
    {
        if (!isSelected)
        {
            Debug.Log(
                "[PLAYER] Cannot move. " +
                "Player is not selected."
            );

            return;
        }

        if (playerController == null)
        {
            return;
        }

        targetPosition.y =
            transform.position.y;

        if (selectionRing != null)
        {
            selectionRing.SetActive(true);

            selectionRing.transform.position =
                targetPosition;
        }

        playerController.MoveTo(targetPosition);

        Debug.Log(
            $"[PLAYER] MOVE TO: {targetPosition}"
        );
    }

    public void MoveToEnemy(Health enemy)
    {
        if (!isSelected)
        {
            return;
        }

        if (playerController == null)
        {
            return;
        }

        Collider enemyCollider =
            enemy.GetComponentInChildren<Collider>();

        if (enemyCollider == null)
        {
            Debug.LogWarning(
                "[PLAYER] Enemy has no Collider!"
            );

            return;
        }

        Vector3 direction =
            transform.position -
            enemy.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector3.back;
        }

        direction.Normalize();

        Vector3 enemySurface =
            enemyCollider.ClosestPoint(
                transform.position
            );

        Vector3 attackPosition =
            enemySurface +
            direction * 0.1f;

        attackPosition.y =
            transform.position.y;

        if (selectionRing != null)
        {
            selectionRing.SetActive(true);

            selectionRing.transform.position =
                attackPosition;
        }

        playerController.MoveTo(attackPosition);

        Debug.Log(
            $"[PLAYER] MOVE TO ENEMY: {attackPosition}"
        );
    }
}