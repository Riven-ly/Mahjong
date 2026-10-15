using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class GuidePop : UIBase
{
    [System.Serializable]
    public sealed class GuideDot
    {
        public GameObject noComplete;
        public RectTransform complete;
        [HideInInspector] public Vector3 originalScale;
    }

    [System.Serializable]
    public struct ScaleAnimation
    {
        public float duration;
        public float interval;
        public Vector3 startScale;
        public Ease ease;
    }

    public RectTransform titleIco;
    public RectTransform tipIco;
    public RectTransform tipIco2;
    public RectTransform task1Slider;
    public RectTransform ico8;
    public Button startButton;
    public RectTransform startButtonTransform;
    public GuideDot[] dots;

    public ScaleAnimation titleAnimation = new ScaleAnimation { duration = 0.25f, startScale = Vector3.zero, ease = Ease.OutBack };
    public ScaleAnimation tipAnimation = new ScaleAnimation { duration = 0.2f, startScale = Vector3.zero, ease = Ease.OutBack };
    public ScaleAnimation sliderAnimation = new ScaleAnimation { duration = 0.2f, startScale = Vector3.zero, ease = Ease.OutBack };
    public ScaleAnimation ico8Animation = new ScaleAnimation { duration = 0.2f, startScale = Vector3.zero, ease = Ease.OutBack };
    public ScaleAnimation startAnimation = new ScaleAnimation { duration = 0.2f, startScale = Vector3.zero, ease = Ease.OutBack };
    public float dotDuration = 0.3f;
    public Vector3 dotStartScale = Vector3.zero;
    public Ease dotEase = Ease.OutBack;

    Sequence guideSequence;
    GuidePopData data;
    Vector3 titleOriginalScale;
    Vector3 tipOriginalScale;
    Vector3 tip2OriginalScale;
    Vector3 sliderOriginalScale;
    Vector3 ico8OriginalScale;
    Vector3 startOriginalScale;
    bool clicked;
    bool completed;

    void Awake()
    {
        titleOriginalScale = titleIco.localScale;
        tipOriginalScale = tipIco.localScale;
        tip2OriginalScale = tipIco2.localScale;
        sliderOriginalScale = task1Slider.localScale;
        ico8OriginalScale = ico8.localScale;
        startOriginalScale = startButtonTransform.localScale;
        for (int index = 0; index < dots.Length; index++)
            dots[index].originalScale = dots[index].complete.localScale;
        startButton.onClick.AddListener(OnStartClicked);
    }

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        data = _data as GuidePopData;
        completed = false;
        guideSequence?.Kill(false);
        guideSequence = null;
        clicked = false;
        ResetGuide();
        GuideIntro.MarkShown();
        TxGameBridge.PlaySfx();
        PlayGuide();
    }

    public override void Hide()
    {
        AddCallback(() =>
        {
            guideSequence?.Kill(false);
            guideSequence = null;
            ResetGuide();
            if (completed)
                return;

            completed = true;
            data?.Completed?.Invoke();
        });
        base.Hide();

    }

    void OnDisable()
    {
        isOpen = false;
        guideSequence?.Kill(false);
        guideSequence = null;
    }

    void OnDestroy()
    {
        guideSequence?.Kill(false);
        guideSequence = null;
    }

    void PlayGuide()
    {
        guideSequence?.Kill(false);
        guideSequence = DOTween.Sequence()
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        AppendScale(titleIco, titleOriginalScale, titleAnimation);
        AppendScale(tipIco, tipOriginalScale, tipAnimation);
        AppendScale(tipIco2, tip2OriginalScale, tipAnimation);
        AppendScale(task1Slider, sliderOriginalScale, sliderAnimation);
        for (int index = 0; index < dots.Length; index++)
        {
            GuideDot dot = dots[index];
            guideSequence.AppendCallback(() =>
            {
                dot.noComplete.SetActive(false);
                dot.complete.gameObject.SetActive(true);
                dot.complete.localScale = dotStartScale;
            });
            guideSequence.Append(dot.complete.DOScale(dot.originalScale, dotDuration).SetEase(dotEase));
        }
        AppendScale(ico8, ico8OriginalScale, ico8Animation);
        AppendScale(startButtonTransform, startOriginalScale, startAnimation);
        guideSequence.OnComplete(() => startButton.interactable = true);
    }

    void ResetGuide()
    {
        titleIco.gameObject.SetActive(false);
        tipIco.gameObject.SetActive(false);
        tipIco2.gameObject.SetActive(false);
        task1Slider.gameObject.SetActive(false);
        ico8.gameObject.SetActive(false);
        startButtonTransform.gameObject.SetActive(false);
        titleIco.localScale = titleOriginalScale;
        tipIco.localScale = tipOriginalScale;
        tipIco2.localScale = tip2OriginalScale;
        task1Slider.localScale = sliderOriginalScale;
        ico8.localScale = ico8OriginalScale;
        startButtonTransform.localScale = startOriginalScale;
        startButton.interactable = false;
        for (int index = 0; index < dots.Length; index++)
        {
            GuideDot dot = dots[index];
            dot.noComplete.SetActive(true);
            dot.complete.gameObject.SetActive(false);
            dot.complete.localScale = dot.originalScale;
        }
    }

    void AppendScale(RectTransform target, Vector3 originalScale, ScaleAnimation animation)
    {
        if (animation.interval > 0f)
            guideSequence.AppendInterval(animation.interval);
        guideSequence.AppendCallback(() =>
        {
            target.gameObject.SetActive(true);
            target.localScale = animation.startScale;
        });
        guideSequence.Append(target.DOScale(originalScale, animation.duration).SetEase(animation.ease));
    }

    void OnStartClicked()
    {
        if (clicked || !startButton.interactable)
            return;

        TxGameBridge.PlaySfx();
        clicked = true;
        startButton.interactable = false;
        TxGameBridge.Track(SolarEventNames.GuideClickStart);
        Hide();
    }
}
