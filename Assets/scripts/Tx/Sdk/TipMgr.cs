using System.Collections;
using System.Globalization;
using DG.Tweening;
using UnityEngine;

public sealed class TipMgr : MonoBehaviour
{
    const float TopShownY = -128f;
    const float TopHiddenY = 200f;

    public RectTransform tipRoot;
    public SdkNoticeItem topNotice;
    public SdkNoticeItem bottomNotice;
    public Sprite channel3Icon;
    public Sprite channel2Icon;
    public Sprite channel0Icon;
    public Sprite channel1Icon;

    public float topMinInterval = 10f;
    public float topMaxInterval = 60f;
    public float topSlideInDuration = 0.3f;
    public float topStayDuration = 2f;
    public float topSlideOutDuration = 0.3f;

    public float bottomMinInterval = 10f;
    public float bottomMaxInterval = 60f;
    public float bottomTotalDuration = 2f;
    public int minAmount = 3000;
    public int maxAmount = 6999;

    Vector2 bottomVisiblePosition;
    Sequence topSequence;
    Sequence bottomSequence;
    bool notificationLoopsStarted;

    void Awake()
    {
        bottomVisiblePosition = (bottomNotice.transform as RectTransform).anchoredPosition;
        ResetNotices();
    }

    void ResetNotices()
    {
        RectTransform topRect = topNotice.transform as RectTransform;
        RectTransform bottomRect = bottomNotice.transform as RectTransform;
        topRect.anchoredPosition = new Vector2(0f, TopHiddenY);
        bottomRect.anchoredPosition = new Vector2(
            bottomVisiblePosition.x - tipRoot.rect.width - bottomRect.rect.width,
            bottomVisiblePosition.y);
        topNotice.gameObject.SetActive(false);
        bottomNotice.gameObject.SetActive(false);
    }

    void OnEnable() => BeginAfterStageGenerated();

    void OnDisable()
    {
        StopAllCoroutines();
        topSequence?.Kill(false);
        bottomSequence?.Kill(false);
        if (topNotice != null && bottomNotice != null)
            ResetNotices();
    }

    public void BeginAfterStageGenerated()
    {
        if (notificationLoopsStarted)
            return;

        notificationLoopsStarted = true;
        StartCoroutine(TopLoop());
        StartCoroutine(BottomLoop());
    }

    IEnumerator TopLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(topMinInterval, topMaxInterval));
            ShowTop();
            yield return new WaitForSecondsRealtime(topSlideInDuration + topStayDuration + topSlideOutDuration);
        }
    }

    IEnumerator BottomLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(bottomMinInterval, bottomMaxInterval));
            ShowBottom();
            yield return new WaitForSecondsRealtime(bottomTotalDuration);
        }
    }

    void ShowTop()
    {
        RectTransform rect = topNotice.transform as RectTransform;
        topSequence?.Kill(false);
        topNotice.Refresh(CreateTopData());
        rect.anchoredPosition = new Vector2(0f, TopHiddenY);
        topNotice.gameObject.SetActive(true);
        topSequence = DOTween.Sequence()
            .SetLink(topNotice.gameObject, LinkBehaviour.KillOnDestroy)
            .Append(rect.DOAnchorPosY(TopShownY, 0.5f).SetEase(Ease.OutCubic))
            .AppendInterval(topStayDuration)
            .Append(rect.DOAnchorPosY(TopHiddenY, 0.5f).SetEase(Ease.InOutCirc))
            .OnComplete(() => topNotice.gameObject.SetActive(false));
    }

    void ShowBottom()
    {
        RectTransform rect = bottomNotice.transform as RectTransform;
        bottomSequence?.Kill(false);
        bottomNotice.Refresh(CreateBottomData());
        bottomNotice.gameObject.SetActive(true);
        float distance = tipRoot.rect.width + rect.rect.width;
        rect.anchoredPosition = bottomVisiblePosition + Vector2.left * distance;
        bottomSequence = DOTween.Sequence()
            .SetLink(bottomNotice.gameObject, LinkBehaviour.KillOnDestroy)
            .Append(rect.DOAnchorPos(
                bottomVisiblePosition + Vector2.right * distance,
                bottomTotalDuration).SetEase(Ease.Linear))
            .OnComplete(() => bottomNotice.gameObject.SetActive(false));
    }

    SdkNoticeData CreateTopData()
    {
        int id = Random.Range(10000000, 100000000);
        string idText = id.ToString(CultureInfo.InvariantCulture);
        int amount = Random.Range(minAmount, maxAmount + 1);
        return new SdkNoticeData(
            "User" + idText.Substring(0, 2) + "***" + idText.Substring(idText.Length - 2),
            GetRandomIcon(),
            TxGameBridge.GetMark() + amount.ToString("N0", CultureInfo.InvariantCulture),
            CreateDateText());
    }

    SdkNoticeData CreateBottomData()
    {
        int id = Random.Range(1000000, 10000000);
        string idText = id.ToString(CultureInfo.InvariantCulture);
        int amount = Random.Range(minAmount, maxAmount + 1);
        string tip = "Congrats! User" + idText.Substring(0, 2) + "***" + idText.Substring(idText.Length - 2)
                     + " " + TxGameBridge.GetWithdraw() + " " + TxGameBridge.GetMark() + amount.ToString(CultureInfo.InvariantCulture);
        return new SdkNoticeData(tip, GetRandomIcon(), null, null);
    }

    Sprite GetRandomIcon()
    {
        switch (Random.Range(0, 4))
        {
            case 0: return channel3Icon;
            case 1: return channel2Icon;
            case 2: return channel0Icon;
            default: return channel1Icon;
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
        topMaxInterval = Mathf.Max(topMinInterval, topMaxInterval);
        bottomMaxInterval = Mathf.Max(bottomMinInterval, bottomMaxInterval);
        maxAmount = Mathf.Max(minAmount, maxAmount);
    }
}
