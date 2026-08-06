using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private Vector2Int boardSize =
        new Vector2Int(8, 8);

    [SerializeField, Min(0.01f)]
    private float tileSize = 1f;

    [Header("Tile")]
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Transform tilesParent;

    [SerializeField] private Color baseColor =
        Color.white;

    [SerializeField] private Color offsetColor =
        Color.gray;

    [Header("Camera")]
    [SerializeField] private Camera boardCamera;

    [SerializeField]
    private bool autoFitCamera = false;

    [SerializeField, Min(0f)]
    private float cameraPadding = 0.5f;

    private readonly Dictionary<Vector2Int, Tile> tiles =
        new Dictionary<Vector2Int, Tile>();

    public Vector2Int BoardSize => boardSize;
    public float TileSize => tileSize;

    public BoardState State { get; private set; }

    private void Awake()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        if (tilePrefab == null)
        {
            Debug.LogError(
                "Tile Prefab is not assigned.",
                this
            );

            return;
        }

        State = new BoardState(
            boardSize.x,
            boardSize.y
        );

        GenerateBoard();
    }

    public void GenerateBoard()
    {
        ClearTileViews();

        for (int x = 0; x < boardSize.x; x++)
        {
            for (int y = 0; y < boardSize.y; y++)
            {
                CreateTile(new Vector2Int(x, y));
            }
        }

        PositionCamera();
    }

    private void CreateTile(Vector2Int coordinates)
    {
        Transform parent =
            tilesParent != null
                ? tilesParent
                : transform;

        Vector3 worldPosition =
            GetWorldPosition(coordinates);

        Tile tile = Instantiate(
            tilePrefab,
            worldPosition,
            transform.rotation,
            parent
        );

        tile.name =
            $"Tile [{coordinates.x}, {coordinates.y}]";

        bool isOffset =
            (coordinates.x + coordinates.y) % 2 != 0;

        Color tileColor =
            isOffset ? offsetColor : baseColor;

        tile.Initialize(
            coordinates,
            tileColor
        );

        tiles.Add(coordinates, tile);
    }

    public Vector3 GetWorldPosition(
        Vector2Int coordinates
    )
    {
        Vector3 localPosition = new Vector3(
            coordinates.x * tileSize,
            coordinates.y * tileSize,
            0f
        );

        return transform.TransformPoint(localPosition);
    }

    public Vector2Int WorldToCoordinates(
        Vector3 worldPosition
    )
    {
        Vector3 localPosition =
            transform.InverseTransformPoint(worldPosition);

        int x = Mathf.RoundToInt(
            localPosition.x / tileSize
        );

        int y = Mathf.RoundToInt(
            localPosition.y / tileSize
        );

        return new Vector2Int(x, y);
    }

    public bool IsInsideBoard(
        Vector2Int coordinates
    )
    {
        return State != null &&
               State.IsInside(coordinates);
    }

    public Tile GetTile(Vector2Int coordinates)
    {
        tiles.TryGetValue(
            coordinates,
            out Tile tile
        );

        return tile;
    }

    public bool TryGetTile(
        Vector2Int coordinates,
        out Tile tile
    )
    {
        return tiles.TryGetValue(
            coordinates,
            out tile
        );
    }

    private void ClearTileViews()
    {
        foreach (Tile tile in tiles.Values)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }

        tiles.Clear();
    }

    private void PositionCamera()
    {
        if (boardCamera == null)
        {
            return;
        }

        Vector3 localCenter = new Vector3(
            (boardSize.x - 1) * tileSize / 2f,
            (boardSize.y - 1) * tileSize / 2f,
            0f
        );

        Vector3 worldCenter =
            transform.TransformPoint(localCenter);

        boardCamera.transform.position = new Vector3(
            worldCenter.x,
            worldCenter.y,
            boardCamera.transform.position.z
        );

        // عند إغلاق Auto Fit لن يتغير Camera Size.
        if (!autoFitCamera ||
            !boardCamera.orthographic)
        {
            return;
        }

        float boardWidth =
            boardSize.x * tileSize;

        float boardHeight =
            boardSize.y * tileSize;

        float verticalSize =
            boardHeight / 2f;

        float horizontalSize =
            boardWidth /
            (2f * boardCamera.aspect);

        boardCamera.orthographicSize =
            Mathf.Max(verticalSize, horizontalSize)
            + cameraPadding;
    }

    private void OnValidate()
    {
        boardSize.x = Mathf.Max(1, boardSize.x);
        boardSize.y = Mathf.Max(1, boardSize.y);

        tileSize = Mathf.Max(
            0.01f,
            tileSize
        );
    }
}