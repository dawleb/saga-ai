using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;

    [Header("Selection")]
    public LayerMask selectableLayer;

    [Header("Box Selection")]
    public Color selectionBorderColor =
        new Color(0.4f, 1f, 0.4f, 0.9f);

    private readonly List<PlayerClickController> selectedUnits =
        new List<PlayerClickController>();

    private Vector2 dragStartScreen;
    private bool isDragging;

    private GameObject selectionBox;
    private LineRenderer selectionBoxLine;

    // ====================================
    // START
    // ====================================

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        CreateSelectionBox();
    }

    // ====================================
    // UPDATE
    // ====================================

    private void Update()
    {
        if (Mouse.current == null ||
            mainCamera == null)
        {
            return;
        }

        HandleMouseInput();
    }

    // ====================================
    // MOUSE INPUT
    // ====================================

    private void HandleMouseInput()
    {
        // ====================================
        // LEFT MOUSE BUTTON
        // ====================================

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            dragStartScreen =
                Mouse.current.position.ReadValue();

            isDragging = false;
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 currentPosition =
                Mouse.current.position.ReadValue();

            float distance =
                Vector2.Distance(
                    dragStartScreen,
                    currentPosition
                );

            if (distance > 10f)
            {
                isDragging = true;

                UpdateSelectionBox(
                    dragStartScreen,
                    currentPosition
                );
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            if (isDragging)
            {
                SelectUnitsInBox(
                    dragStartScreen,
                    mousePosition
                );
            }
            else
            {
                HandleLeftClick(
                    mousePosition
                );
            }

            HideSelectionBox();
        }

        // ====================================
        // RIGHT MOUSE BUTTON
        // ====================================

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    // ====================================
    // LEFT CLICK
    // ====================================
    // LPM na soldierze = zaznaczenie.
    // LPM na żywym wrogu = atak dystansowy.
    // LPM na trupie = NIC.
    // ====================================

    private void HandleLeftClick(
        Vector2 screenPosition)
    {
        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                500f))
        {
            return;
        }

        // ====================================
        // OWN UNIT
        // ====================================

        PlayerClickController clickedUnit =
            hit.collider.GetComponentInParent<
                PlayerClickController
            >();

        if (clickedUnit != null)
        {
            SelectSingleUnit(
                clickedUnit
            );

            return;
        }

        // ====================================
        // ENEMY
        // ====================================

        Health enemy =
            hit.collider.GetComponentInParent<Health>();

        if (enemy != null)
        {
            // MARTWY WRÓG = IGNORUJEMY KLIKNIĘCIE
            if (!IsEnemyAlive(enemy))
            {
                return;
            }

            HandleEnemyLeftClick(
                enemy
            );

            return;
        }

        // ====================================
        // GROUND
        // ====================================

        // LPM na ziemi nic nie robi.
    }

    // ====================================
    // RIGHT CLICK
    // ====================================
    // RMB na ziemi = ruch.
    // RMB na żywym wrogu = ruch do niego.
    // RMB na trupie = NIC.
    // ====================================

    private void HandleRightClick()
    {
        RemoveInvalidSelectedUnits();

        if (selectedUnits.Count == 0)
        {
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            mainCamera.ScreenPointToRay(
                mousePosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                500f))
        {
            return;
        }

        // ====================================
        // ENEMY
        // ====================================

        Health enemy =
            hit.collider.GetComponentInParent<Health>();

        if (enemy != null)
        {
            // MARTWY WRÓG = NIC NIE ROBIMY
            if (!IsEnemyAlive(enemy))
            {
                return;
            }

            HandleEnemyRightClick(
                enemy
            );

            return;
        }

        // ====================================
        // GROUND
        // ====================================

        HandleGroundRightClick(
            hit.point
        );
    }

    // ====================================
    // ENEMY LEFT CLICK
    // ====================================
    // LPM na żywego zombie = atak.
    // NIE WOLNO tutaj wywołać MoveToEnemy().
    // ====================================

    private void HandleEnemyLeftClick(
        Health enemy)
    {
        if (!IsEnemyAlive(enemy))
        {
            return;
        }

        RemoveInvalidSelectedUnits();

        foreach (
            PlayerClickController unit
            in selectedUnits)
        {
            if (unit == null)
            {
                continue;
            }

            if (enemy.gameObject == unit.gameObject)
            {
                continue;
            }

            unit.AttackEnemyAtRange(
                enemy
            );
        }
    }

    // ====================================
    // ENEMY RIGHT CLICK
    // ====================================
    // RMB na żywego zombie = podejście.
    // ====================================

    private void HandleEnemyRightClick(
        Health enemy)
    {
        if (!IsEnemyAlive(enemy))
        {
            return;
        }

        RemoveInvalidSelectedUnits();

        foreach (
            PlayerClickController unit
            in selectedUnits)
        {
            if (unit == null)
            {
                continue;
            }

            if (enemy.gameObject == unit.gameObject)
            {
                continue;
            }

            unit.MoveToEnemy(
                enemy
            );
        }
    }

    // ====================================
    // GROUND RIGHT CLICK
    // ====================================

    private void HandleGroundRightClick(
        Vector3 targetPosition)
    {
        RemoveInvalidSelectedUnits();

        foreach (
            PlayerClickController unit
            in selectedUnits)
        {
            if (unit == null)
            {
                continue;
            }

            unit.MoveToGround(
                targetPosition
            );
        }
    }

    // ====================================
    // SINGLE UNIT SELECTION
    // ====================================

    private void SelectSingleUnit(
        PlayerClickController unit)
    {
        if (unit == null)
        {
            return;
        }

        ClearSelection();

        SelectUnit(
            unit
        );
    }

    // ====================================
    // BOX SELECTION
    // ====================================

    private void SelectUnitsInBox(
        Vector2 start,
        Vector2 end)
    {
        ClearSelection();

        Rect selectionRect =
            GetScreenRect(
                start,
                end
            );

        PlayerClickController[] units =
            FindObjectsOfType<
                PlayerClickController
            >();

        foreach (
            PlayerClickController unit
            in units)
        {
            if (unit == null ||
                !unit.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 screenPosition =
                mainCamera.WorldToScreenPoint(
                    unit.transform.position
                );

            if (screenPosition.z < 0f)
            {
                continue;
            }

            Vector2 unitScreenPosition =
                new Vector2(
                    screenPosition.x,
                    screenPosition.y
                );

            if (selectionRect.Contains(
                unitScreenPosition))
            {
                SelectUnit(
                    unit
                );
            }
        }
    }

    // ====================================
    // SELECT UNIT
    // ====================================

    private void SelectUnit(
        PlayerClickController unit)
    {
        if (unit == null)
        {
            return;
        }

        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
        }

        unit.SetSelected(
            true
        );
    }

    // ====================================
    // CLEAR SELECTION
    // ====================================

    public void ClearSelection()
    {
        foreach (
            PlayerClickController unit
            in selectedUnits)
        {
            if (unit != null)
            {
                unit.SetSelected(
                    false
                );
            }
        }

        selectedUnits.Clear();
    }

    // ====================================
    // REMOVE INVALID UNITS
    // ====================================

    private void RemoveInvalidSelectedUnits()
    {
        for (
            int i = selectedUnits.Count - 1;
            i >= 0;
            i--)
        {
            PlayerClickController unit =
                selectedUnits[i];

            if (unit == null ||
                !unit.gameObject.activeInHierarchy)
            {
                if (unit != null)
                {
                    unit.SetSelected(false);
                }

                selectedUnits.RemoveAt(i);
            }
        }
    }

    // ====================================
    // CHECK ENEMY ALIVE
    // ====================================

    private bool IsEnemyAlive(
        Health enemy)
    {
        if (enemy == null)
        {
            return false;
        }

        // GameObject nieaktywny = brak możliwości celu.
        if (!enemy.gameObject.activeInHierarchy)
        {
            return false;
        }

        // Health <= 0 = trup.
        if (enemy.IsDead())
        {
            return false;
        }

        return true;
    }

    // ====================================
    // SCREEN RECT
    // ====================================

    private Rect GetScreenRect(
        Vector2 start,
        Vector2 end)
    {
        Vector2 min =
            Vector2.Min(
                start,
                end
            );

        Vector2 max =
            Vector2.Max(
                start,
                end
            );

        return Rect.MinMaxRect(
            min.x,
            min.y,
            max.x,
            max.y
        );
    }

    // ====================================
    // CREATE SELECTION BOX
    // ====================================

    private void CreateSelectionBox()
    {
        selectionBox =
            new GameObject(
                "RTS Selection Box"
            );

        selectionBox.transform.SetParent(
            transform
        );

        selectionBoxLine =
            selectionBox.AddComponent<
                LineRenderer
            >();

        selectionBoxLine.useWorldSpace =
            true;

        selectionBoxLine.loop =
            true;

        selectionBoxLine.positionCount =
            4;

        selectionBoxLine.startWidth =
            0.015f;

        selectionBoxLine.endWidth =
            0.015f;

        selectionBoxLine.material =
            CreateSelectionMaterial();

        selectionBoxLine.startColor =
            selectionBorderColor;

        selectionBoxLine.endColor =
            selectionBorderColor;

        HideSelectionBox();
    }

    // ====================================
    // CREATE SELECTION MATERIAL
    // ====================================

    private Material CreateSelectionMaterial()
    {
        Shader shader =
            Shader.Find(
                "Sprites/Default"
            );

        Material material =
            new Material(shader);

        material.color =
            selectionBorderColor;

        return material;
    }

    // ====================================
    // UPDATE SELECTION BOX
    // ====================================

    private void UpdateSelectionBox(
        Vector2 start,
        Vector2 end)
    {
        if (selectionBox == null)
        {
            return;
        }

        selectionBox.SetActive(
            true
        );

        Vector2 min =
            Vector2.Min(
                start,
                end
            );

        Vector2 max =
            Vector2.Max(
                start,
                end
            );

        float distance =
            5f;

        Vector3 bottomLeft =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    min.x,
                    min.y,
                    distance
                )
            );

        Vector3 topLeft =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    min.x,
                    max.y,
                    distance
                )
            );

        Vector3 topRight =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    max.x,
                    max.y,
                    distance
                )
            );

        Vector3 bottomRight =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    max.x,
                    min.y,
                    distance
                )
            );

        selectionBoxLine.SetPosition(
            0,
            bottomLeft
        );

        selectionBoxLine.SetPosition(
            1,
            topLeft
        );

        selectionBoxLine.SetPosition(
            2,
            topRight
        );

        selectionBoxLine.SetPosition(
            3,
            bottomRight
        );
    }

    // ====================================
    // HIDE SELECTION BOX
    // ====================================

    private void HideSelectionBox()
    {
        if (selectionBox != null)
        {
            selectionBox.SetActive(
                false
            );
        }
    }
}