using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class SdkNoticeDisplay : MonoBehaviour
{
    // 基准可见公告数量
    const int BaseVisibleCount = 2;

    // 最大可见公告数量
    const int MaxVisibleCount = 3;

    // 浮点比较容差
    const float Epsilon = 0.5f;

    public RectTransform content;

    public SdkNoticeItem[] items;

    // 公告裁剪区域
    public RectTransform viewport;

    // 底部限制区域
    public RectTransform bottomLimit;

    // 与底部提示保持的间距
    public float bottomGap = 12f;

    public float showTime = 1f;

    public float moveTime = 0.35f;

    public float itemHeight = 186f;

    public int minAmount = 3000;

    public int maxAmount = 6999;

    public Sprite channel3Icon;

    public Sprite channel2Icon;

    public Sprite channel0Icon;

    public Sprite channel1Icon;

    readonly List<SdkNoticeItem> orderedItems = new List<SdkNoticeItem>(3);
    readonly Vector3[] viewportCorners = new Vector3[4];
    readonly Vector3[] bottomLimitCorners = new Vector3[4];
    Sequence scrollSequence;
    Coroutine fitRoutine;
    Vector2 startPosition;
    float baseBottom;
    bool initialized;
    bool fitPending;

    void Awake()
    {
        startPosition = content.anchoredPosition;
        orderedItems.AddRange(items);
        baseBottom = viewport.offsetMin.y;
        initialized = true;
    }

    void OnEnable()
    {
        if (Application.isPlaying && initialized)
            RequestFit();
    }

    void OnRectTransformDimensionsChange()
    {
        if (Application.isPlaying && initialized && isActiveAndEnabled)
            RequestFit();
    }

    void OnDisable() => Cleanup();

    void OnDestroy() => Cleanup();

    void RefreshAll()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[SDK公告] 请在 Play Mode 下刷新公告。");
            return;
        }

        for (int index = 0; index < orderedItems.Count; index++)
            orderedItems[index].Refresh(CreateData());
    }

    void PlayOnce()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[SDK公告] 请在 Play Mode 下播放滚动。");
            return;
        }

        KillLoop();
        PlayScroll(null);
    }

    // 请求下一帧完成尺寸适配
    void RequestFit()
    {
        if (!initialized || !isActiveAndEnabled || fitPending)
            return;

        fitPending = true;
        KillLoop();
        SetBottom(baseBottom);
        fitRoutine = StartCoroutine(FitNextFrame());
    }

    // 在布局稳定后适配可见公告数量
    IEnumerator FitNextFrame()
    {
        yield return null;

        fitRoutine = null;
        if (!isActiveAndEnabled)
        {
            fitPending = false;
            yield break;
        }

        SetBottom(baseBottom);
        Canvas.ForceUpdateCanvases();

        float extendedBottom = GetViewportBottomInPanel() - itemHeight;
        float requiredBottom = GetBottomLimitTopInPanel() + bottomGap;
        int visibleCount = extendedBottom + Epsilon >= requiredBottom ? MaxVisibleCount : BaseVisibleCount;
        float targetBottom = baseBottom - (visibleCount - BaseVisibleCount) * itemHeight;
        SetBottom(targetBottom);

        fitPending = false;
        StartLoop();
    }

    // 获取裁剪区域底边在面板坐标中的位置
    float GetViewportBottomInPanel()
    {
        viewport.GetWorldCorners(viewportCorners);
        RectTransform panel = (RectTransform)transform.parent;
        float leftBottom = panel.InverseTransformPoint(viewportCorners[0]).y;
        float rightBottom = panel.InverseTransformPoint(viewportCorners[3]).y;
        return Mathf.Min(leftBottom, rightBottom);
    }

    // 获取底部限制区域顶边在面板坐标中的位置
    float GetBottomLimitTopInPanel()
    {
        bottomLimit.GetWorldCorners(bottomLimitCorners);
        RectTransform panel = (RectTransform)transform.parent;
        float leftTop = panel.InverseTransformPoint(bottomLimitCorners[1]).y;
        float rightTop = panel.InverseTransformPoint(bottomLimitCorners[2]).y;
        return Mathf.Max(leftTop, rightTop);
    }

    // 绝对设置裁剪区域底边
    void SetBottom(float value)
    {
        if (Mathf.Abs(viewport.offsetMin.y - value) <= Epsilon)
            return;

        Vector2 offset = viewport.offsetMin;
        offset.y = value;
        viewport.offsetMin = offset;
    }

    // 停止适配与滚动并恢复基准状态
    void Cleanup()
    {
        if (fitRoutine != null)
            StopCoroutine(fitRoutine);

        fitRoutine = null;
        fitPending = false;
        KillLoop();
        if (initialized)
            SetBottom(baseBottom);
    }

    void StartLoop()
    {
        KillLoop();
        content.anchoredPosition = startPosition;
        for (int index = 0; index < orderedItems.Count; index++)
            orderedItems[index].transform.SetSiblingIndex(index);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        RefreshAll();

        scrollSequence = DOTween.Sequence()
            
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .AppendInterval(showTime)
            .Append(content.DOAnchorPosY(startPosition.y + itemHeight, moveTime).SetEase(Ease.InOutCubic))
            .AppendCallback(ReuseFirst)
            .SetLoops(-1, LoopType.Restart);
    }

    void PlayScroll(TweenCallback completed)
    {
        scrollSequence = DOTween.Sequence()
            
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Append(content.DOAnchorPosY(startPosition.y + itemHeight, moveTime).SetEase(Ease.InOutCubic))
            .AppendCallback(ReuseFirst)
            .OnComplete(() => completed?.Invoke());
    }

    void ReuseFirst()
    {
        SdkNoticeItem first = orderedItems[0];
        orderedItems.RemoveAt(0);
        orderedItems.Add(first);
        first.transform.SetAsLastSibling();
        first.Refresh(CreateData());
        content.anchoredPosition = startPosition;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    void KillLoop()
    {
        scrollSequence?.Kill(false);
        scrollSequence = null;
        if (initialized)
            content.anchoredPosition = startPosition;
    }

    SdkNoticeData CreateData()
    {
        int id = Random.Range(10000000, 100000000);
        string idText = id.ToString(CultureInfo.InvariantCulture);
        string user = "User" + idText.Substring(0, 2) + "***" + idText.Substring(idText.Length - 2);
        int amount = Random.Range(minAmount, maxAmount + 1);
        return new SdkNoticeData(
            user,
            GetRandomIcon(),
            TxGameBridge.GetMark() + amount.ToString("N0", CultureInfo.InvariantCulture),
            CreateDateText());
    }

    Sprite GetRandomIcon()
    {
        switch (Random.Range(0, 4))
        {
            case 0:
                return channel3Icon;
            case 1:
                return channel2Icon;
            case 2:
                return channel0Icon;
            default:
                return channel1Icon;
        }
    }

    static string CreateDateText()
    {
        int unit = Random.Range(0, 3);
        int value = unit == 2 ? Random.Range(1, 25) : Random.Range(1, 61);
        string label = unit == 0 ? "second" : unit == 1 ? "minute" : "hour";
        return value + " " + label + (value == 1 ? string.Empty : "s") + " ago";
    }

    void OnValidate()
    {
        if (maxAmount < minAmount)
            maxAmount = minAmount;
    }
}
