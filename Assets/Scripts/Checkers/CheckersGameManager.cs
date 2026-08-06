using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CheckersGameManager : MonoBehaviour
{
    [Header("Board References")]
    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private BoardInputController inputController;

    [Header("Pieces")]
    [SerializeField]
    private Transform piecesParent;

    [SerializeField, Range(0.1f, 1.5f)]
    private float pieceScale = 1f;

    [SerializeField]
    private float pieceWorldZ = -0.1f;

    private readonly Dictionary<int, CheckersPiece>
        pieces =
            new Dictionary<int, CheckersPiece>();

    private readonly Dictionary<int, CheckersPieceView>
        pieceViews =
            new Dictionary<int, CheckersPieceView>();

    // جميع الأماكن التي يمكن للقطعة المحددة الذهاب إليها.
    private readonly HashSet<Vector2Int>
        legalMoves =
            new HashSet<Vector2Int>();

    /*
     * مفتاح القاموس:
     * مكان الهبوط بعد القفز.
     *
     * القيمة:
     * ID القطعة التي سيتم أكلها.
     */
    private readonly Dictionary<Vector2Int, int>
        captureTargets =
            new Dictionary<Vector2Int, int>();

    private int nextPieceId = 1;

    private CheckersPiece selectedPiece;

    private bool isAnimating;

    public CheckersPlayer CurrentPlayer
    {
        get;
        private set;
    }

    private void Awake()
    {
        bool loaded =
            CheckersResources.Load();

        if (!loaded)
        {
            Debug.LogError(
                "Checkers Game Manager was disabled " +
                "because piece resources could not be loaded.",
                this
            );

            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (inputController != null)
        {
            inputController.TileSelected +=
                OnTileSelected;
        }
    }

    private void OnDisable()
    {
        if (inputController != null)
        {
            inputController.TileSelected -=
                OnTileSelected;
        }
    }

    private void Start()
    {
        SetupGame();
    }

    public void SetupGame()
    {
        if (boardManager == null)
        {
            Debug.LogError(
                "Board Manager is not assigned.",
                this
            );

            return;
        }

        if (inputController == null)
        {
            Debug.LogError(
                "Board Input Controller is not assigned.",
                this
            );

            return;
        }

        if (boardManager.State == null)
        {
            Debug.LogError(
                "Board State is not initialized.",
                this
            );

            return;
        }

        ClearPieces();

        boardManager.State.Clear();

        nextPieceId = 1;
        selectedPiece = null;
        isAnimating = false;

        CurrentPlayer =
            CheckersPlayer.PlayerOne;

        SpawnStartingPieces();

        Debug.Log(
            "Checkers game initialized. " +
            "Player One starts."
        );
    }

    private void SpawnStartingPieces()
    {
        int boardWidth =
            boardManager.BoardSize.x;

        int boardHeight =
            boardManager.BoardSize.y;

        // Player One: الصفوف الثلاثة السفلية.
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);

                if (!IsPlayableSquare(position))
                {
                    continue;
                }

                SpawnPiece(
                    CheckersPlayer.PlayerOne,
                    position
                );
            }
        }

        // Player Two: الصفوف الثلاثة العلوية.
        for (
            int y = boardHeight - 3;
            y < boardHeight;
            y++
        )
        {
            for (int x = 0; x < boardWidth; x++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);

                if (!IsPlayableSquare(position))
                {
                    continue;
                }

                SpawnPiece(
                    CheckersPlayer.PlayerTwo,
                    position
                );
            }
        }
    }

    private void SpawnPiece(
        CheckersPlayer owner,
        Vector2Int position
    )
    {
        CheckersPieceView prefab =
            CheckersResources.GetPrefab(owner);

        if (prefab == null)
        {
            Debug.LogError(
                $"No prefab was found for {owner}.",
                this
            );

            return;
        }

        int pieceId = nextPieceId;

        bool placed =
            boardManager.State.TryPlacePiece(
                position,
                pieceId
            );

        if (!placed)
        {
            Debug.LogWarning(
                $"Could not place piece at {position}.",
                this
            );

            return;
        }

        nextPieceId++;

        CheckersPiece piece =
            new CheckersPiece(
                pieceId,
                owner,
                position
            );

        Transform parent =
            piecesParent != null
                ? piecesParent
                : transform;

        Vector3 worldPosition =
            GetPieceWorldPosition(position);

        CheckersPieceView pieceView =
            Instantiate(
                prefab,
                worldPosition,
                Quaternion.identity,
                parent
            );

        pieceView.Initialize(
            pieceId,
            pieceScale
        );

        pieceView.name =
            $"{owner} Piece [{pieceId}]";

        pieces.Add(
            pieceId,
            piece
        );

        pieceViews.Add(
            pieceId,
            pieceView
        );
    }

    private void OnTileSelected(
        Vector2Int coordinates
    )
    {
        if (isAnimating)
        {
            return;
        }

        int pieceId =
            boardManager.State.GetPieceId(
                coordinates
            );

        // تم الضغط على مربع يحتوي قطعة.
        if (pieceId != 0)
        {
            HandlePieceSelection(pieceId);
            return;
        }

        // لا توجد قطعة محددة.
        if (selectedPiece == null)
        {
            return;
        }

        // تحريك أو أكل حسب المربع المختار.
        if (legalMoves.Contains(coordinates))
        {
            MoveSelectedPiece(coordinates);
            return;
        }

        DeselectCurrentPiece();
    }

    private void HandlePieceSelection(
        int pieceId
    )
    {
        if (!pieces.TryGetValue(
                pieceId,
                out CheckersPiece piece))
        {
            return;
        }

        if (piece.Owner != CurrentPlayer)
        {
            Debug.Log(
                $"It is {CurrentPlayer}'s turn."
            );

            return;
        }

        /*
         * إذا كان لدى اللاعب أي أكلة،
         * لا نسمح باختيار قطعة لا تستطيع الأكل.
         */
        if (PlayerHasAnyCapture(CurrentPlayer) &&
            !PieceHasCapture(piece))
        {
            Debug.Log(
                "Capture is available. " +
                "You must select a piece that can capture."
            );

            return;
        }

        SelectPiece(piece);
    }

    private void SelectPiece(
        CheckersPiece piece
    )
    {
        if (selectedPiece != null &&
            selectedPiece.Id == piece.Id)
        {
            DeselectCurrentPiece();
            return;
        }

        DeselectCurrentPiece();

        selectedPiece = piece;

        if (pieceViews.TryGetValue(
                piece.Id,
                out CheckersPieceView pieceView))
        {
            pieceView.SetSelected(true);
        }

        ShowLegalMoves(piece);

        Debug.Log(
            $"Selected {piece.Owner} Piece. " +
            $"Position: {piece.Position}, " +
            $"King: {piece.IsKing}"
        );
    }

    private void ShowLegalMoves(
        CheckersPiece piece
    )
    {
        ClearMoveHighlights();

        /*
         * إذا كان هناك أكل لأي قطعة عند اللاعب،
         * نظهر الأكل فقط ولا نظهر الحركة العادية.
         */
        if (PlayerHasAnyCapture(piece.Owner))
        {
            AddCaptureMoves(piece);
            return;
        }

        AddNormalMoves(piece);
    }

    private void AddNormalMoves(
        CheckersPiece piece
    )
    {
        int forwardDirection =
            piece.Owner ==
            CheckersPlayer.PlayerOne
                ? 1
                : -1;

        TryAddNormalMove(
            piece.Position +
            new Vector2Int(
                -1,
                forwardDirection
            )
        );

        TryAddNormalMove(
            piece.Position +
            new Vector2Int(
                1,
                forwardDirection
            )
        );

        if (!piece.IsKing)
        {
            return;
        }

        int backwardDirection =
            -forwardDirection;

        TryAddNormalMove(
            piece.Position +
            new Vector2Int(
                -1,
                backwardDirection
            )
        );

        TryAddNormalMove(
            piece.Position +
            new Vector2Int(
                1,
                backwardDirection
            )
        );
    }

    private void TryAddNormalMove(
        Vector2Int destination
    )
    {
        if (!boardManager.IsInsideBoard(
                destination))
        {
            return;
        }

        if (!IsPlayableSquare(destination))
        {
            return;
        }

        if (!boardManager.State.IsEmpty(
                destination))
        {
            return;
        }

        AddLegalMoveHighlight(destination);
    }

    private void AddCaptureMoves(
        CheckersPiece piece
    )
    {
        int forwardDirection =
            piece.Owner ==
            CheckersPlayer.PlayerOne
                ? 1
                : -1;

        TryAddCaptureMove(
            piece,
            -1,
            forwardDirection
        );

        TryAddCaptureMove(
            piece,
            1,
            forwardDirection
        );

        if (!piece.IsKing)
        {
            return;
        }

        int backwardDirection =
            -forwardDirection;

        TryAddCaptureMove(
            piece,
            -1,
            backwardDirection
        );

        TryAddCaptureMove(
            piece,
            1,
            backwardDirection
        );
    }

    private void TryAddCaptureMove(
        CheckersPiece movingPiece,
        int directionX,
        int directionY
    )
    {
        Vector2Int enemyPosition =
            movingPiece.Position +
            new Vector2Int(
                directionX,
                directionY
            );

        Vector2Int landingPosition =
            movingPiece.Position +
            new Vector2Int(
                directionX * 2,
                directionY * 2
            );

        if (!boardManager.IsInsideBoard(
                enemyPosition) ||
            !boardManager.IsInsideBoard(
                landingPosition))
        {
            return;
        }

        if (!boardManager.State.IsEmpty(
                landingPosition))
        {
            return;
        }

        int enemyPieceId =
            boardManager.State.GetPieceId(
                enemyPosition
            );

        if (enemyPieceId == 0)
        {
            return;
        }

        if (!pieces.TryGetValue(
                enemyPieceId,
                out CheckersPiece enemyPiece))
        {
            return;
        }

        // لا يمكن أكل قطعة من نفس اللاعب.
        if (enemyPiece.Owner == movingPiece.Owner)
        {
            return;
        }

        legalMoves.Add(landingPosition);

        captureTargets[landingPosition] =
            enemyPieceId;

        Tile tile =
            boardManager.GetTile(
                landingPosition
            );

        if (tile != null)
        {
            tile.SetMoveHighlight(true);
        }
    }

    private void AddLegalMoveHighlight(
        Vector2Int coordinates
    )
    {
        legalMoves.Add(coordinates);

        Tile tile =
            boardManager.GetTile(coordinates);

        if (tile != null)
        {
            tile.SetMoveHighlight(true);
        }
    }

    private void MoveSelectedPiece(
        Vector2Int destination
    )
    {
        if (selectedPiece == null)
        {
            return;
        }

        CheckersPiece movingPiece =
            selectedPiece;

        Vector2Int oldPosition =
            movingPiece.Position;

        bool isCapture =
            captureTargets.TryGetValue(
                destination,
                out int capturedPieceId
            );

        bool moved =
            boardManager.State.TryMovePiece(
                oldPosition,
                destination
            );

        if (!moved)
        {
            Debug.LogWarning(
                $"Could not move piece from " +
                $"{oldPosition} to {destination}."
            );

            return;
        }

        movingPiece.MoveTo(destination);

        if (isCapture)
        {
            CapturePiece(capturedPieceId);
        }

        if (!pieceViews.TryGetValue(
                movingPiece.Id,
                out CheckersPieceView movingView))
        {
            selectedPiece = null;
            ClearMoveHighlights();

            FinishMove(
                movingPiece,
                isCapture
            );

            return;
        }

        movingView.SetSelected(false);

        selectedPiece = null;

        ClearMoveHighlights();

        isAnimating = true;

        Vector3 destinationWorldPosition =
            GetPieceWorldPosition(destination);

        movingView.AnimateMove(
            destinationWorldPosition,
            () =>
            {
                FinishMove(
                    movingPiece,
                    isCapture
                );
            }
        );
    }

    private void CapturePiece(
        int capturedPieceId
    )
    {
        if (!pieces.TryGetValue(
                capturedPieceId,
                out CheckersPiece capturedPiece))
        {
            return;
        }

        boardManager.State.TryRemovePiece(
            capturedPiece.Position
        );

        pieces.Remove(capturedPieceId);

        if (!pieceViews.TryGetValue(
                capturedPieceId,
                out CheckersPieceView capturedView))
        {
            return;
        }

        pieceViews.Remove(capturedPieceId);

        capturedView.AnimateCapture(
            () =>
            {
                if (capturedView != null)
                {
                    Destroy(
                        capturedView.gameObject
                    );
                }
            }
        );

        Debug.Log(
            $"{capturedPiece.Owner} piece was captured."
        );
    }

    private void FinishMove(
        CheckersPiece movedPiece,
        bool wasCapture
    )
    {
        bool wasPromoted =
            CheckPromotion(movedPiece);

        isAnimating = false;

        /*
         * إذا أكلت القطعة ويوجد أمامها أكل آخر،
         * يبقى الدور على نفس القطعة.
         *
         * إذا أصبحت King ننهي الدور.
         */
        if (wasCapture &&
            !wasPromoted &&
            PieceHasCapture(movedPiece))
        {
            selectedPiece = movedPiece;

            if (pieceViews.TryGetValue(
                    movedPiece.Id,
                    out CheckersPieceView pieceView))
            {
                pieceView.SetSelected(true);
            }

            ShowLegalMoves(movedPiece);

            Debug.Log(
                "Another capture is available. " +
                "Continue with the same piece."
            );

            return;
        }

        selectedPiece = null;

        SwitchTurn();
    }

    private bool CheckPromotion(
        CheckersPiece piece
    )
    {
        if (piece.IsKing)
        {
            return false;
        }

        int promotionRow =
            piece.Owner ==
            CheckersPlayer.PlayerOne
                ? boardManager.BoardSize.y - 1
                : 0;

        if (piece.Position.y != promotionRow)
        {
            return false;
        }

        piece.Promote();

        if (pieceViews.TryGetValue(
                piece.Id,
                out CheckersPieceView pieceView))
        {
            pieceView.AnimatePromotion();
        }

        Debug.Log(
            $"{piece.Owner} piece became King."
        );

        return true;
    }

    private bool PlayerHasAnyCapture(
        CheckersPlayer player
    )
    {
        foreach (
            CheckersPiece piece
            in pieces.Values
        )
        {
            if (piece.Owner != player)
            {
                continue;
            }

            if (PieceHasCapture(piece))
            {
                return true;
            }
        }

        return false;
    }

    private bool PieceHasCapture(
        CheckersPiece piece
    )
    {
        int forwardDirection =
            piece.Owner ==
            CheckersPlayer.PlayerOne
                ? 1
                : -1;

        if (HasCaptureInDirection(
                piece,
                -1,
                forwardDirection))
        {
            return true;
        }

        if (HasCaptureInDirection(
                piece,
                1,
                forwardDirection))
        {
            return true;
        }

        if (!piece.IsKing)
        {
            return false;
        }

        int backwardDirection =
            -forwardDirection;

        if (HasCaptureInDirection(
                piece,
                -1,
                backwardDirection))
        {
            return true;
        }

        return HasCaptureInDirection(
            piece,
            1,
            backwardDirection
        );
    }

    private bool HasCaptureInDirection(
        CheckersPiece movingPiece,
        int directionX,
        int directionY
    )
    {
        Vector2Int enemyPosition =
            movingPiece.Position +
            new Vector2Int(
                directionX,
                directionY
            );

        Vector2Int landingPosition =
            movingPiece.Position +
            new Vector2Int(
                directionX * 2,
                directionY * 2
            );

        if (!boardManager.IsInsideBoard(
                enemyPosition) ||
            !boardManager.IsInsideBoard(
                landingPosition))
        {
            return false;
        }

        if (!boardManager.State.IsEmpty(
                landingPosition))
        {
            return false;
        }

        int enemyPieceId =
            boardManager.State.GetPieceId(
                enemyPosition
            );

        if (enemyPieceId == 0)
        {
            return false;
        }

        if (!pieces.TryGetValue(
                enemyPieceId,
                out CheckersPiece enemyPiece))
        {
            return false;
        }

        return enemyPiece.Owner !=
               movingPiece.Owner;
    }

    private void SwitchTurn()
    {
        CurrentPlayer =
            CurrentPlayer ==
            CheckersPlayer.PlayerOne

                ? CheckersPlayer.PlayerTwo
                : CheckersPlayer.PlayerOne;

        Debug.Log(
            $"Current Turn: {CurrentPlayer}"
        );
    }

    private void DeselectCurrentPiece()
    {
        if (selectedPiece != null &&
            pieceViews.TryGetValue(
                selectedPiece.Id,
                out CheckersPieceView pieceView))
        {
            pieceView.SetSelected(false);
        }

        selectedPiece = null;

        ClearMoveHighlights();
    }

    private void ClearMoveHighlights()
    {
        foreach (
            Vector2Int coordinates
            in legalMoves
        )
        {
            Tile tile =
                boardManager.GetTile(coordinates);

            if (tile != null)
            {
                tile.SetMoveHighlight(false);
            }
        }

        legalMoves.Clear();
        captureTargets.Clear();
    }

    private Vector3 GetPieceWorldPosition(
        Vector2Int coordinates
    )
    {
        Vector3 worldPosition =
            boardManager.GetWorldPosition(
                coordinates
            );

        worldPosition.z += pieceWorldZ;

        return worldPosition;
    }

    private bool IsPlayableSquare(
        Vector2Int coordinates
    )
    {
        return (
            coordinates.x +
            coordinates.y
        ) % 2 != 0;
    }

    private void ClearPieces()
    {
        DeselectCurrentPiece();

        isAnimating = false;

        foreach (
            CheckersPieceView pieceView
            in pieceViews.Values
        )
        {
            if (pieceView != null)
            {
                Destroy(
                    pieceView.gameObject
                );
            }
        }

        pieces.Clear();
        pieceViews.Clear();
    }
}