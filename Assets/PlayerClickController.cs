using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerClickController : MonoBehaviour
{
    public Camera mainCamera;
    public PlayerController playerController;
    public GameObject selectionRing;

    public float attackDistance = 1.5f;

    private UnitSelectable selectedUnit;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerController == null)
            playerController =
                GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Ray ray = mainCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit
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

        // Clicked on a selectable unit.
        UnitSelectable unit =
            hit.collider.GetComponentInParent<UnitSelectable>();

        if (unit != null)
        {
            SelectUnit(unit);
            return;
        }

        // Clicked on an enemy.
        Health targetHealth =
            hit.collider.GetComponentInParent<Health>();

        if (targetHealth != null &&
            targetHealth.gameObject != gameObject)
        {
            MoveToEnemy(targetHealth);
            return;
        }

        // Clicked on ground.
        if (selectedUnit != null)
        {
            MoveSelectedUnit(hit.point);
        }
    }

    private void SelectUnit(UnitSelectable unit)
    {
        if (selectedUnit != null)
            selectedUnit.Deselect();

        selectedUnit = unit;
        selectedUnit.Select();

        Debug.Log(
            "[PLAYER] Unit selected"
        );
    }

    private void MoveToEnemy(Health enemy)
{
    if (playerController == null)
        return;

    Collider playerCollider =
        GetComponent<Collider>();

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

    // Get the enemy's closest point toward Player.
    Vector3 enemySurface =
        enemyCollider.ClosestPoint(
            transform.position
        );

    // Small extra distance so the colliders
    // do not touch.
    float extraDistance = 0.1f;

    Vector3 attackPosition =
        enemySurface +
        direction * extraDistance;

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
        $"[PLAYER] Moving to enemy surface: " +
        $"{attackPosition}"
    );
}
    private void MoveSelectedUnit(
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
            $"[PLAYER] MOVE -> " +
            $"{targetPosition}"
        );
    }
}