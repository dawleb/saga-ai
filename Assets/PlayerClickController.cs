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
            playerController = GetComponent<PlayerController>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        SetSelected(false);
        HideEnemySelection();
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (animator != null && playerController != null)
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
                    new Vector3(0f, 0.03f, 0f);
            }
        }

        if (!selected)
        {
            HideEnemySelection();

            if (selectionRing != null)
            {
                selectionRing.SetActive(false);
            }

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

    // Shows the enemy marker only for a right-click movement command.
    private void ShowEnemySelection(Health enemy)
    {
        if (enemy == null)
        {
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

        selectedEnemyMarker = marker.gameObject;
        selectedEnemyMarker.SetActive(true);
    }

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

    private Transform FindChildByName(
        Transform parent,
        string childName)
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
            Transform child = parent.GetChild(i);

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

    // Right-click on the ground moves the unit to the selected position.
    public void MoveToGround(Vector3 targetPosition)
    {
        if (!isSelected ||
            playerController == null)
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

    // Right-click on an enemy moves the unit toward the enemy.
    // This is the only command that shows the enemy marker.
    public void MoveToEnemy(Health enemy)
    {
        if (!isSelected ||
            playerController == null ||
            enemy == null)
        {
            return;
        }

        ShowEnemySelection(enemy);

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

    // Left-clicking an enemy selects it as the attack target.
    // The unit never moves because of this command.
    public void AttackEnemyAtRange(Health enemy)
    {
        if (!isSelected ||
            enemy == null)
        {
            return;
        }

        selectedEnemy = enemy;

        // Show the enemy marker for the current attack target.
        ShowEnemySelection(enemy);

        Vector3 direction =
            enemy.transform.position -
            transform.position;

        direction.y = 0f;

        float distance =
            direction.magnitude;

        // Enemy is outside the weapon range.
        // Keep the target selected, but do not move.
        if (distance > attackRange)
        {
            Debug.Log(
                $"[ATTACK] {enemy.name} is out of range. " +
                $"Distance: {distance:F2}, " +
                $"Range: {attackRange:F2}"
            );

            return;
        }

        // Face the enemy.
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized
                );
        }

        // The movement destination ring is not needed for an attack.
        if (selectionRing != null)
        {
            selectionRing.SetActive(false);
        }

        // Fire without starting movement.
        ShootAtEnemy(enemy);
    }

    private void ShootAtEnemy(Health enemy)
    {
        if (enemy == null)
        {
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