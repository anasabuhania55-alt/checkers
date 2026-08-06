using UnityEngine;

public enum CheckersPlayer
{
    PlayerOne,
    PlayerTwo
}

public sealed class CheckersPiece
{
    public int Id { get; }

    public CheckersPlayer Owner { get; }

    public Vector2Int Position { get; private set; }

    public bool IsKing { get; private set; }

    public CheckersPiece(
        int id,
        CheckersPlayer owner,
        Vector2Int position
    )
    {
        Id = id;
        Owner = owner;
        Position = position;
        IsKing = false;
    }

    public void MoveTo(Vector2Int newPosition)
    {
        Position = newPosition;
    }

    public void Promote()
    {
        IsKing = true;
    }
}