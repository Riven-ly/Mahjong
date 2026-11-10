using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelEntrancePanel : UIBase
{
    public sealed class CloseThenOpen
    {
        public Action OnClosed { get; }
        public Action OnOpened { get; }

        public CloseThenOpen(Action onClosed, Action onOpened)
        {
            OnClosed = onClosed;
            OnOpened = onOpened;
        }
    }

    [SerializeField] private Image leftDoor;
    [SerializeField] private Image rightDoor;
    [SerializeField] private CanvasGroup centerGlow;
    [SerializeField, Min(0.01f)] private float closeDuration = 0.40f;
    [SerializeField, Min(0f)] private float closedDuration = 0.53f;
    [SerializeField, Min(0.01f)] private float openDuration = 0.66f;
    [SerializeField, Min(0f)] private float boardRevealPause = 0.40f;

    private Sequence opening;

    public override void Refresh(object data = null)
    {
        isOpen = true;
        StopDoorSequence();
        var transition = data as CloseThenOpen;
        Action onOpened = transition?.OnOpened ?? data as Action;
        var panel = (RectTransform)transform;
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.localScale = Vector3.one;

        if (leftDoor.sprite != null)
            leftDoor.color = Color.white;
        if (rightDoor.sprite != null)
            rightDoor.color = Color.white;
        centerGlow.alpha = 0f;
        Canvas.ForceUpdateCanvases();

        float travel = panel.rect.width * 0.5f + 2f;
        float startOffset = transition != null ? travel : 0f;
        leftDoor.rectTransform.anchoredPosition = new Vector2(-startOffset, 0f);
        rightDoor.rectTransform.anchoredPosition = new Vector2(startOffset, 0f);

        var sequence = DOTween.Sequence().SetUpdate(true);
        opening = sequence;
        if (transition != null)
        {
            sequence.Append(leftDoor.rectTransform.DOAnchorPosX(0f, closeDuration).SetEase(Ease.InOutCubic));
            sequence.Join(rightDoor.rectTransform.DOAnchorPosX(0f, closeDuration).SetEase(Ease.InOutCubic));
            sequence.AppendCallback(() =>
            {
                if (opening == sequence)
                    transition.OnClosed?.Invoke();
            });
        }

        sequence.Append(centerGlow.DOFade(0.8f, closedDuration).SetEase(Ease.InQuad));
        sequence.Append(leftDoor.rectTransform.DOAnchorPosX(-travel, openDuration).SetEase(Ease.InQuad));
        sequence.Join(rightDoor.rectTransform.DOAnchorPosX(travel, openDuration).SetEase(Ease.InQuad));
        sequence.Join(centerGlow.DOFade(0f, openDuration * 0.5f));
        sequence.AppendInterval(boardRevealPause);
        sequence.OnComplete(() =>
        {
            if (opening != sequence)
                return;

            opening = null;
            onOpened?.Invoke();
        });
    }

    private void OnDisable()
    {
        isOpen = false;
        StopDoorSequence();
    }

    private void StopDoorSequence()
    {
        var sequence = opening;
        opening = null;
        sequence?.Pause();
        sequence?.Kill();
    }
}
