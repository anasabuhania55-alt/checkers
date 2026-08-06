using System;
using UnityEngine;

public sealed class BoardState
{
    private readonly int[] cells;

    public int Width { get; }
    public int Height { get; }

    public BoardState(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException(
                "Board width and height must be greater than zero."
            );
        }

        Width = width;
        Height = height;

        cells = new int[width * height];
    }

    public bool IsInside(Vector2Int coordinates)
    {
        return coordinates.x >= 0 &&
               coordinates.x < Width &&
               coordinates.y >= 0 &&
               coordinates.y < Height;
    }

    public int GetPieceId(Vector2Int coordinates)
    {
        if (!IsInside(coordinates))
        {
            return 0;
        }

        return cells[GetIndex(coordinates)];
    }

    public bool IsEmpty(Vector2Int coordinates)
    {
        return GetPieceId(coordinates) == 0;
    }

    public bool TryPlacePiece(
        Vector2Int coordinates,
        int pieceId
    )
    {
        if (!IsInside(coordinates) ||
            !IsEmpty(coordinates) ||
            pieceId <= 0)
        {
            return false;
        }

        cells[GetIndex(coordinates)] = pieceId;
        return true;
    }

    public bool TryMovePiece(
        Vector2Int from,
        Vector2Int to
    )
    {
        if (!IsInside(from) ||
            !IsInside(to) ||
            from == to)
        {
            return false;
        }

        int pieceId = GetPieceId(from);

        if (pieceId == 0 || !IsEmpty(to))
        {
            return false;
        }

        cells[GetIndex(from)] = 0;
        cells[GetIndex(to)] = pieceId;

        return true;
    }

    public bool TryRemovePiece(Vector2Int coordinates)
    {
        if (!IsInside(coordinates) ||
            IsEmpty(coordinates))
        {
            return false;
        }

        cells[GetIndex(coordinates)] = 0;
        return true;
    }

    public void Clear()
    {
        Array.Clear(cells, 0, cells.Length);
    }

    private int GetIndex(Vector2Int coordinates)
    {
        return coordinates.y * Width + coordinates.x;
    }
}