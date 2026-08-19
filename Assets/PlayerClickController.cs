using UnityEngine;

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

    [Header("Ranged Attack")]
    [Tooltip("Maximum distance at which the player can attack.")]
    public float attackRange = 6f;

    [Header("Animation")]
    public Animator animator;

    private bool isSelected;
    private GameObject selectedEnemyMarker;
    private Health selectedEnemy;

    public Health SelectedEnemy
    {
        get { return selectedEnemy; }
    }

    public bool IsSelected
    {
        get { return isSelected; }
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
        HideEnemySelection();
    }

    private void Update()
    {
        UpdateAnimation();
    }

    // ====================================
    // UPDATE ANIMATION
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
    // SET SELECTED
    // ====================================

    public void SetSelected(bool selected)
    {
        // Dead player can never be selected.
        if (playerController != null &&
            playerController.IsDead)
        {
            selected = false;
        }

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

            HideSelectionRing();

            if (playerController != null)
            {
                playerController.StopMovement();
            }
        }

        Debug.Log(
            selected
                ? $"[SELECTION] {name} selected"
                : $"[SELECTION] {name} deselected"
        );
    }

    // ====================================
    // HIDE SELECTION RING
    // ====================================

    public void HideSelectionRing()
    {
        if (selectionRing != null)
        {
            selectionRing.SetActive(false);
        }
    }

    // ====================================
    // PLAYER DIED
    // ====================================

    public void SetDead()
    {
        // Remove player selection immediately.
        isSelected = false;

        // Hide player selection square.
        if (selectionSquare != null)
        {
            selectionSquare.SetActive(false);
        }

        // Hide movement ring immediately.
        HideSelectionRing();

        // Remove enemy target marker.
        HideEnemySelection();

        // Stop movement.
        if (playerController != null)
        {
            playerController.SetDead();
        }

        Debug.Log(
            "[SELECTION] Player died. " +
            "Selection and movement ring removed."
        );
    }

    // ====================================
    // SHOW ENEMY SELECTION
    // ====================================

    private void ShowEnemySelection(
        Health enemy
    )
    {
        if (enemy == null)
        {
            return;
        }

        if (enemy.IsDead())
        {
            HideEnemySelection();
            return;
        }

        HideEnemySelectionVisualOnly();

        selectedEnemy = enemy;

        Transform marker =
            FindChildByName(
                enemy.transform,
                enemySelectionMarkerName
            );

        if (marker == null)
        {
            Debug.LogWarning(
                $"[SELECTION] Could not find '{enemySelectionMarkerName}' inside {enemy.name}."
            );

            return;
        }

        selectedEnemyMarker =
            marker.gameObject;

        selectedEnemyMarker.SetActive(true);
    }

    // ====================================
    // HIDE ENEMY SELECTION
    // ====================================

    private void HideEnemySelection()
    {
        HideEnemySelectionVisualOnly();

        selectedEnemy = null;
    }

    private void HideEnemySelectionVisualOnly()
    {
        if (selectedEnemyMarker == null)
        {
            return;
        }

        selectedEnemyMarker.SetActive(false);
        selectedEnemyMarker = null;
    }

    // ====================================
    // FIND CHILD
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

        for (int i = 0;
             i < parent.childCount;
             i++)
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
        if (playerController == null ||
            playerController.IsDead)
        {
            HideSelectionRing();
            return;
        }

        if (!isSelected)
        {
            return;
        }

        HideEnemySelection();

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
            $"[COMMAND] Move to position: {targetPosition}"
        );
    }

    // ====================================
    // MOVE TO ENEMY
    // ====================================

    public void MoveToEnemy(
        Health enemy
    )
    {
        if (playerController == null ||
            playerController.IsDead)
        {
            HideSelectionRing();
            return;
        }

        if (!isSelected ||
            enemy == null)
        {
            return;
        }

        if (enemy.IsDead())
        {
            HideEnemySelection();
            HideSelectionRing();
            return;
        }

        ShowEnemySelection(
            enemy
        );

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

        Vector3 targetPosition =
            enemySurface +
            direction * 0.1f;

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
            $"[COMMAND] Move to enemy: {enemy.name}"
        );
    }

    // ====================================
    // ATTACK ENEMY
    // ====================================

    public void AttackEnemyAtRange(
        Health enemy
    )
    {
        if (playerController == null ||
            playerController.IsDead)
        {
            HideSelectionRing();
            return;
        }

        if (!isSelected ||
            enemy == null)
        {
            return;
        }

        if (enemy.IsDead())
        {
            HideEnemySelection();
            HideSelectionRing();
            return;
        }

        selectedEnemy = enemy;

        ShowEnemySelection(
            enemy
        );

        Vector3 direction =
            enemy.transform.position -
            transform.position;

        direction.y = 0f;

        float distance =
            direction.magnitude;

        if (distance > attackRange)
        {
            Debug.Log(
                $"[ATTACK] {enemy.name} is out of range. " +
                $"Distance: {distance:F2}, " +
                $"Range: {attackRange:F2}"
            );

            return;
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized
                );
        }

        HideSelectionRing();

        ShootAtEnemy(
            enemy
        );
    }

    // ====================================
    // SHOOT
    // ====================================

    private void ShootAtEnemy(
        Health enemy
    )
    {
        if (enemy == null)
        {
            return;
        }

        if (enemy.IsDead())
        {
            HideEnemySelection();
            HideSelectionRing();
            return;
        }

        if (playerController != null &&
            playerController.IsDead)
        {
            HideSelectionRing();
            return;
        }

        Debug.Log(
            $"[ATTACK] Shooting at {enemy.name}"
        );

        gameObject.SendMessage(
            "Attack",
            enemy,
            SendMessageOptions.DontRequireReceiver
        );
    }
}