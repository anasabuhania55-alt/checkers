using System;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class CheckersPieceView : MonoBehaviour
{
    [Header("King")]
    [SerializeField]
    private GameObject kingMark;

    [Header("Spawn Animation")]
    [SerializeField, Min(0.01f)]
    private float spawnDuration = 0.25f;

    [Header("Move Animation")]
    [SerializeField, Min(0.01f)]
    private float moveDuration = 0.3f;

    [Header("Selection Animation")]
    [SerializeField, Min(0.01f)]
    private float selectionDuration = 0.15f;

    [SerializeField, Range(1f, 1.5f)]
    private float selectedScaleMultiplier = 1.15f;

    [Header("Capture Animation")]
    [SerializeField, Min(0.01f)]
    private float captureDuration = 0.2f;

    public int PieceId { get; private set; }

    private Vector3 normalScale;

    private Tween moveTween;
    private Tween scaleTween;
    private Tween promotionTween;

    public void Initialize(
        int pieceId,
        float scaleMultiplier = 1f
    )
    {
        PieceId = pieceId;

        KillTweens();

        normalScale =
            transform.localScale * scaleMultiplier;

        transform.localScale = Vector3.zero;

        SetKing(false);

        scaleTween = transform
            .DOScale(normalScale, spawnDuration)
            .SetEase(Ease.OutBack);
    }

    public void SetSelected(bool selected)
    {
        KillScaleTween();

        Vector3 targetScale =
            selected
                ? normalScale * selectedScaleMultiplier
                : normalScale;

        scaleTween = transform
            .DOScale(targetScale, selectionDuration)
            .SetEase(Ease.OutQuad);
    }

    public void AnimateMove(
        Vector3 targetPosition,
        Action onComplete = null
    )
    {
        KillMoveTween();

        moveTween = transform
            .DOMove(targetPosition, moveDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
    }

    public void AnimateCapture(
        Action onComplete = null
    )
    {
        KillMoveTween();
        KillScaleTween();

        scaleTween = transform
            .DOScale(Vector3.zero, captureDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
    }

    public void SetKing(bool isKing)
    {
        if (kingMark != null)
        {
            kingMark.SetActive(isKing);
        }
    }

    public void AnimatePromotion()
    {
        SetKing(true);

        KillPromotionTween();

        promotionTween = transform
            .DOPunchScale(
                normalScale * 0.2f,
                0.4f,
                6,
                0.6f
            );
    }

    private void KillMoveTween()
    {
        if (moveTween != null &&
            moveTween.IsActive())
        {
            moveTween.Kill();
        }

        moveTween = null;
    }

    private void KillScaleTween()
    {
        if (scaleTween != null &&
            scaleTween.IsActive())
        {
            scaleTween.Kill();
        }

        scaleTween = null;
    }

    private void KillPromotionTween()
    {
        if (promotionTween != null &&
            promotionTween.IsActive())
        {
            promotionTween.Kill();
        }

        promotionTween = null;
    }

    private void KillTweens()
    {
        KillMoveTween();
        KillScaleTween();
        KillPromotionTween();
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}