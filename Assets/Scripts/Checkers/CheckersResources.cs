using UnityEngine;

public static class CheckersResources
{
    private const string PlayerOnePiecePath =
        "Pieces/PlayerOnePiece";

    private const string PlayerTwoPiecePath =
        "Pieces/PlayerTwoPiece";

    public static CheckersPieceView PlayerOnePrefab
    {
        get;
        private set;
    }

    public static CheckersPieceView PlayerTwoPrefab
    {
        get;
        private set;
    }

    public static bool IsLoaded
    {
        get;
        private set;
    }

    public static bool Load()
    {
        if (IsLoaded)
        {
            return true;
        }

        PlayerOnePrefab =
            LoadPiecePrefab(PlayerOnePiecePath);

        PlayerTwoPrefab =
            LoadPiecePrefab(PlayerTwoPiecePath);

        IsLoaded =
            PlayerOnePrefab != null &&
            PlayerTwoPrefab != null;

        if (!IsLoaded)
        {
            Debug.LogError(
                "Failed to load Checkers piece prefabs."
            );
        }

        return IsLoaded;
    }

    public static CheckersPieceView GetPrefab(
        CheckersPlayer player
    )
    {
        if (!IsLoaded)
        {
            Load();
        }

        return player == CheckersPlayer.PlayerOne
            ? PlayerOnePrefab
            : PlayerTwoPrefab;
    }

    private static CheckersPieceView LoadPiecePrefab(
        string resourcePath
    )
    {
        GameObject prefab =
            Resources.Load<GameObject>(
                resourcePath
            );

        if (prefab == null)
        {
            Debug.LogError(
                $"Piece prefab was not found at: " +
                $"Assets/Resources/{resourcePath}.prefab"
            );

            return null;
        }

        CheckersPieceView pieceView =
            prefab.GetComponent<CheckersPieceView>();

        if (pieceView == null)
        {
            Debug.LogError(
                $"{prefab.name} does not contain " +
                $"{nameof(CheckersPieceView)}."
            );

            return null;
        }

        return pieceView;
    }
}