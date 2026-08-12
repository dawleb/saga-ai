using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerClickController : MonoBehaviour
{
    public Camera mainCamera;
    public PlayerController playerController;
    public GameObject selectionRing;

    private UnitSelectable selectedUnit;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
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

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("[PLAYER] Raycast hit nothing");
            return;
        }

        Debug.Log($"[PLAYER] Click hit: {hit.collider.name}");

        UnitSelectable unit = hit.collider.GetComponent<UnitSelectable>();

        // Clicked on a unit.
        if (unit != null)
        {
            SelectUnit(unit);
            return;
        }

        // Clicked on the ground.
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

        Debug.Log("[PLAYER] Unit selected");
    }

    private void MoveSelectedUnit(Vector3 targetPosition)
    {
        if (playerController == null)
            return;

        targetPosition.y = 0.1f;

        if (selectionRing != null)
        {
            selectionRing.SetActive(true);
            selectionRing.transform.position = targetPosition;
        }

        playerController.MoveTo(targetPosition);

        Debug.Log($"[PLAYER] MOVE -> {targetPosition}");
    }
}