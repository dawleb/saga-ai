using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerClickController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public PlayerController playerController;
    public GameObject selectionRing;

    [Header("Player Selection")]
    public GameObject selectionSquare;

    [Header("Enemy Selection")]
    [Tooltip("Marker placed as a child of the enemy/zombie.")]
    public string enemySelectionMarkerName = "SelectionMarker";

    [Header("Animation")]
    public Animator animator;

    private bool isSelected;

    private GameObject selectedEnemyMarker;

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
            playerController =
                GetComponent<PlayerController>();
        }

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        SetSelected(false);

        // Make sure no enemy marker is selected at start.
        HideEnemySelection();
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

        // ====================================
        // PLAYER CLICK
        // ====================================

        if (hit.collider.transform.IsChildOf(transform))
        {
            SetSelected(true);

            HideEnemySelection();

            Debug.Log(
                "[PLAYER] Player selected"
            );

            return;
        }

        // ====================================
        // PLAYER MUST BE SELECTED
        // ====================================

        if (!isSelected)
        {
            Debug.Log(
                "[PLAYER] Player is not selected. " +
                "Click ignored."
            );

            return;
        }

        // ====================================
        // ENEMY CLICK
        // ====================================

        Health targetHealth =
            hit.collider.GetComponentInParent<Health>();

        if (targetHealth != null &&
            targetHealth.gameObject != gameObject)
        {
            ShowEnemySelection(
                targetHealth
            );

            MoveToEnemy(
                targetHealth
            );

            return;
        }

        // ====================================
        // GROUND CLICK
        // ====================================

        HideEnemySelection();

        MoveToGround(
            hit.point
        );
    }

    // ====================================
    // ANIMATION
    // ====================================

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

    // ====================================
    // PLAYER SELECTION
    // ====================================

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionSquare != null)
        {
            selectionSquare.SetActive(
                selected
            );

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

        if (!selected)
        {
            HideEnemySelection();
        }

        Debug.Log(
            selected
                ? $"[SELECTION] {name} selected"
                : $"[SELECTION] {name} deselected"
        );
    }

    // ====================================
    // SHOW ENEMY MARKER
    // ====================================

    private void ShowEnemySelection(
        Health enemy
    )
    {
        if (enemy == null)
        {
            return;
        }

        // If another enemy was selected,
        // hide its marker first.
        HideEnemySelection();

        Transform marker =
            FindChildByName(
                enemy.transform,
                enemySelectionMarkerName
            );

        if (marker == null)
        {
            Debug.LogWarning(
                $"[SELECTION] Could not find " +
                $"'{enemySelectionMarkerName}' " +
                $"inside {enemy.name}."
            );

            return;
        }

        selectedEnemyMarker =
            marker.gameObject;

        selectedEnemyMarker.SetActive(
            true
        );

        Debug.Log(
            $"[SELECTION] Enemy marker ON: {enemy.name}"
        );
    }

    // ====================================
    // HIDE ENEMY MARKER
    // ====================================

    private void HideEnemySelection()
    {
        if (selectedEnemyMarker == null)
        {
            return;
        }

        selectedEnemyMarker.SetActive(
            false
        );

        Debug.Log(
            "[SELECTION] Enemy marker OFF."
        );

        selectedEnemyMarker = null;
    }

    // ====================================
    // FIND CHILD RECURSIVELY
    // ====================================

    private Transform FindChildByName(
        Transform parent,
        string childName
    )
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child =
                parent.GetChild(i);

            Transform result =
                FindChildByName(
                    child,
                    childName
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    // ====================================
    // MOVE TO GROUND
    // ====================================

    public void MoveToGround(
        Vector3 targetPosition
    )
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
            selectionRing.SetActive(
                true
            );

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

    // ====================================
    // MOVE TO ENEMY
    // ====================================

    public void MoveToEnemy(
        Health enemy
    )
    {
        if (!isSelected)
        {
            return;
        }

        if (playerController == null)
        {
            return;
        }

        if (enemy == null)
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
            direction =
                Vector3.back;
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
            selectionRing.SetActive(
                true
            );

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