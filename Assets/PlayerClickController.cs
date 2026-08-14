using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerClickController : MonoBehaviour
{
    public Camera mainCamera;
    public PlayerController playerController;
    public GameObject selectionRing;

    [Header("Animation")]
    public Animator animator;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerController == null)
            playerController =
                GetComponent<PlayerController>();

        if (animator == null)
            animator =
                GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // -----------------------------
        // ANIMATION
        // -----------------------------

        if (animator != null &&
            playerController != null)
        {
            animator.SetBool(
                "IsWalking",
                playerController.IsMoving
            );
        }

        // -----------------------------
        // MOUSE
        // -----------------------------

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (mainCamera == null)
            return;

        Ray ray =
            mainCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

        // -----------------------------
        // RAYCAST
        // -----------------------------

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            100f
        ))
        {
            Debug.Log(
                "[PLAYER] Raycast hit nothing"
            );

            return;
        }

        Debug.Log(
            $"[PLAYER] Click hit: {hit.collider.name}"
        );

        // -----------------------------
        // ENEMY
        // -----------------------------

        Health targetHealth =
            hit.collider.GetComponentInParent<Health>();

        if (targetHealth != null &&
            targetHealth.gameObject != gameObject)
        {
            MoveToEnemy(targetHealth);
            return;
        }

        // -----------------------------
        // GROUND
        // -----------------------------

        MoveToGround(hit.point);
    }

    private void MoveToGround(
        Vector3 targetPosition
    )
    {
        if (playerController == null)
            return;

        targetPosition.y =
            transform.position.y;

        if (selectionRing != null)
        {
            selectionRing.SetActive(true);

            selectionRing.transform.position =
                targetPosition;
        }

        playerController.MoveTo(
            targetPosition
        );

        Debug.Log(
            $"[PLAYER] MOVE TO: {targetPosition}"
        );
    }

    private void MoveToEnemy(
        Health enemy
    )
    {
        if (playerController == null)
            return;

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
            direction = Vector3.back;

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

        playerController.MoveTo(
            attackPosition
        );

        Debug.Log(
            $"[PLAYER] MOVE TO ENEMY: {attackPosition}"
        );
    }
}