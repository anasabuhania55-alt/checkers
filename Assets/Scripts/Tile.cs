using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Tile : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SpriteRenderer tileRenderer;

    [Header("Highlights")]
    [SerializeField]
    private GameObject highlight;

    [SerializeField]
    private GameObject moveHighlight;

    public Vector2Int Coordinates
    {
        get;
        private set;
    }

    public void Initialize(
        Vector2Int coordinates,
        Color tileColor
    )
    {
        Coordinates = coordinates;

        if (tileRenderer != null)
        {
            tileRenderer.color = tileColor;
        }

        SetHighlight(false);
        SetMoveHighlight(false);
    }

    // Hover Highlight.
    public void SetHighlight(bool active)
    {
        if (highlight != null)
        {
            highlight.SetActive(active);
        }
    }

    // Legal Move Highlight.
    public void SetMoveHighlight(bool active)
    {
        if (moveHighlight != null)
        {
            moveHighlight.SetActive(active);
        }
    }
}