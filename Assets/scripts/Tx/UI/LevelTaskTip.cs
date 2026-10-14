using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class LevelTaskTip : UIBase
{
    public CanvasGroup background;
    public RectTransform panelRoot;
    public TMP_Text description1;
    public TMP_Text description2;
    [Min(0.01f)]
    public float panelDuration = 1f;
    [Min(0.01f)]
    public float firstDuration = 0.35f;
    [Min(0.01f)]
    public float secondDuration = 0.35f;
    [Min(0.01f)]
    public float scaleDuration = 0.2f;
    [Min(0f)]
    public float stayDuration = 0.5f;
    [Min(0f)]
    public float offscreenPadding = 40f;

    LevelTaskTipData data;
    Sequence sequence;
    Vector2 panelCenter;
    Vector2 description1Center;
    Vector2 description2Center;
    bool positionsCaptured;

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        data = _data as LevelTaskTipData;
        StopSequence();

        if (data == null)
        {
            Debug.LogError("[Tx] 关卡任务提示弹窗缺少有效数据");
            return;
        }

        Canvas.ForceUpdateCanvases();
        CapturePositions();
        RestoreLayout();

        description1.text = $"Level {data.NextLevel}";
        description2.text = string.IsNullOrEmpty(data.TaskText) ? "Complete the current task" : data.TaskText;

        PlaySequence();
    }

    public override void Hide()
    {
        StopSequence();

        LevelTaskTipData finished = data;
        data = null;

        AddCallback(() =>
        {
            RestoreLayout();
            finished?.Complete();
        });

        base.Hide();
    }

    void PlaySequence()
    {
        if (background != null)
            background.alpha = 0f;
        if (panelRoot != null)
            panelRoot.anchoredPosition = panelCenter + Vector2.down * OffscreenDistance(panelRoot);

        sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (background != null)
            sequence.Append(background.DOFade(1f, panelDuration));
        if (panelRoot != null)
            sequence.Join(panelRoot.DOAnchorPos(panelCenter, panelDuration).SetEase(Ease.OutCubic));

        sequence.AppendCallback(() =>
        {
            description1.rectTransform.anchoredPosition = description1Center + Vector2.left * OffscreenDistance(description1.rectTransform);
            description1.rectTransform.localScale = Vector3.one * 1.25f;
            description1.gameObject.SetActive(true);
        });
        sequence.Append(description1.rectTransform.DOAnchorPos(description1Center, firstDuration).SetEase(Ease.OutCubic));
        sequence.Append(description1.rectTransform.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutCubic));

        sequence.AppendCallback(() =>
        {
            description2.rectTransform.anchoredPosition = description2Center + Vector2.right * OffscreenDistance(description2.rectTransform);
            description2.rectTransform.localScale = Vector3.one * 1.25f;
            description2.gameObject.SetActive(true);
        });
        sequence.Append(description2.rectTransform.DOAnchorPos(description2Center, secondDuration).SetEase(Ease.OutCubic));
        sequence.Append(description2.rectTransform.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutCubic));

        sequence.AppendInterval(stayDuration);
        sequence.OnComplete(() => Hide());
    }

    void CapturePositions()
    {
        if (panelRoot != null)
            panelCenter = panelRoot.anchoredPosition;

        description1Center = description1.rectTransform.anchoredPosition;
        description2Center = description2.rectTransform.anchoredPosition;
        positionsCaptured = true;
    }

    void RestoreLayout()
    {
        if (!positionsCaptured)
            return;

        if (panelRoot != null)
            panelRoot.anchoredPosition = panelCenter;

        description1.rectTransform.anchoredPosition = description1Center;
        description2.rectTransform.anchoredPosition = description2Center;
        description1.rectTransform.localScale = Vector3.one;
        description2.rectTransform.localScale = Vector3.one;
        description1.gameObject.SetActive(false);
        description2.gameObject.SetActive(false);

        if (background != null)
            background.alpha = 1f;
    }

    void StopSequence()
    {
        if (sequence == null)
            return;

        sequence.Kill();
        sequence = null;
    }

    float OffscreenDistance(RectTransform target)
    {
        RectTransform parent = target.parent as RectTransform;
        if (parent == null)
            return target.rect.width + offscreenPadding;

        return parent.rect.width + target.rect.width + offscreenPadding;
    }
}
