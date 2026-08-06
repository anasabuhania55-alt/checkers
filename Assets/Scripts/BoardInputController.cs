using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class BoardInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera boardCamera;

    [Header("Tile Detection")]
    [SerializeField] private LayerMask tileLayer;

    [SerializeField] private float boardPlaneZ = 0f;

    private InputAction pointAction;
    private InputAction selectAction;

    private Tile hoveredTile;

    public Tile HoveredTile => hoveredTile;

    public event Action<Vector2Int> TileSelected;

    private void Awake()
    {
        if (boardCamera == null)
        {
            boardCamera = Camera.main;
        }

        pointAction = new InputAction(
            name: "Point",
            type: InputActionType.PassThrough,
            binding: "<Pointer>/position"
        );

        selectAction = new InputAction(
            name: "Select",
            type: InputActionType.Button,
            binding: "<Pointer>/press"
        );
    }

    private void OnEnable()
    {
        pointAction.Enable();
        selectAction.Enable();

        selectAction.performed += OnSelect;
    }

    private void OnDisable()
    {
        selectAction.performed -= OnSelect;

        pointAction.Disable();
        selectAction.Disable();

        SetHoveredTile(null);
    }

    private void Update()
    {
        UpdateHoveredTile();
    }

    private void UpdateHoveredTile()
    {
        if (boardCamera == null)
        {
            return;
        }

        Vector2 screenPosition =
            pointAction.ReadValue<Vector2>();

        float distanceFromCamera =
            Mathf.Abs(
                boardPlaneZ -
                boardCamera.transform.position.z
            );

        Vector3 screenPoint = new Vector3(
            screenPosition.x,
            screenPosition.y,
            distanceFromCamera
        );

        Vector3 worldPosition =
            boardCamera.ScreenToWorldPoint(screenPoint);

        Collider2D hit = Physics2D.OverlapPoint(
            worldPosition,
            tileLayer
        );

        Tile newHoveredTile = null;

        if (hit != null)
        {
            newHoveredTile =
                hit.GetComponentInParent<Tile>();
        }

        SetHoveredTile(newHoveredTile);
    }

    private void SetHoveredTile(Tile newTile)
    {
        if (hoveredTile == newTile)
        {
            return;
        }

        if (hoveredTile != null)
        {
            hoveredTile.SetHighlight(false);
        }

        hoveredTile = newTile;

        if (hoveredTile != null)
        {
            hoveredTile.SetHighlight(true);
        }
    }

    private void OnSelect(
        InputAction.CallbackContext context
    )
    {
        UpdateHoveredTile();

        if (hoveredTile == null)
        {
            return;
        }

        Vector2Int coordinates =
            hoveredTile.Coordinates;

        Debug.Log(
            $"Selected Tile: {coordinates}"
        );

        TileSelected?.Invoke(coordinates);
    }

    private void OnDestroy()
    {
        pointAction?.Dispose();
        selectAction?.Dispose();
    }
}