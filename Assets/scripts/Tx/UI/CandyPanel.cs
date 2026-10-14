using System;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class CandyPanel : UIBase
{
    public static CandyPanel Instance { get; private set; }

    public Button closeButton;
    public Button reviseButton;
    public Button editButton;
    public GameObject login;
    public GameObject noLogin;
    public TextMeshProUGUI accountText;
    public TextMeshProUGUI nameText;
    public Image accountIcon;
    public Sprite channel0Icon;
    public Sprite channel1Icon;
    public Sprite channel2Icon;
    public Sprite channel3Icon;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI taskText;
    public Image taskProgressFill;
    public TextMeshProUGUI taskProgressText;
    public Image taskIcon;
    public Sprite task2Icon;
    public Sprite task3Icon;
    public Sprite task4Icon;
    public Sprite task5Icon;
    public Button recordsButton;
    public Button actionButton;
    public RectTransform actionPulseTarget;
    public float actionPulseScale = 1.08f;
    public float actionPulseGrow = 0.12f;
    public float actionPulseShrink = 0.1f;
    public float actionPulsePause = 0.4f;
    public float valueAnimDuration = 0.5f;
    public float amountGrowDuration = 0.1f;
    public float amountShrinkDuration = 0.1f;
    public float amountMaxScale = 1.2f;

    Vector3 actionPulseBaseScale;
    Vector3 amountBaseScale;
    Tween actionPulse;
    Tween amountTween;
    Tween amountScaleTween;
    Tween taskTween;
    float openingAmount;
    SdkTaskProgress openingTaskProgress;

    enum CloseIntent { None, PlayerClose, StartFlow }

    CloseIntent closeIntent;
    string closeFlowId;

    void Awake()
    {
        Instance = this;
        closeButton.onClick.AddListener(Close);
        reviseButton.onClick.AddListener(OpenAccount);
        editButton.onClick.AddListener(OpenAccount);
        recordsButton.onClick.AddListener(OpenRecords);
        if (actionButton == null)
            actionButton = transform.Find("AmountBar/ActionButton")?.GetComponent<Button>();
        if (actionPulseTarget == null && actionButton != null)
            actionPulseTarget = actionButton.transform as RectTransform;
        if (actionPulseTarget != null)
            actionPulseBaseScale = actionPulseTarget.localScale;
        amountBaseScale = amountText.rectTransform.localScale;
        if (actionButton != null)
            actionButton.onClick.AddListener(OnAction);
        SdkCandyEvents.AccountProfileUpdated += OnAccountProfileUpdated;
        PhaseTaskService.GoldChanged += OnAmountChanged;
        PhaseTaskEvents.CurrentPhaseChanged += OnCurrentPhaseChanged;
        PhaseTaskEvents.PhaseCompleted += OnPhaseCompleted;
        PhaseTaskEvents.ProgressChanged += RefreshTask;
    }

    void OnDestroy()
    {
        SdkCandyEvents.AccountProfileUpdated -= OnAccountProfileUpdated;
        PhaseTaskService.GoldChanged -= OnAmountChanged;
        PhaseTaskEvents.CurrentPhaseChanged -= OnCurrentPhaseChanged;
        PhaseTaskEvents.PhaseCompleted -= OnPhaseCompleted;
        PhaseTaskEvents.ProgressChanged -= RefreshTask;
        StopActionPulse();
        StopValueAnimation();
        if (Instance == this)
            Instance = null;
    }

    public override void Open(object data = null, Action _callback = null)
    {
        bool wasOpen = isOpen;
        base.Open(data, _callback);
        if (!wasOpen)
            TxGameBridge.Track(SolarEventNames.PageEnter);
    }

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        if (closeIntent == CloseIntent.PlayerClose)
            PhaseFlowCoordinator.Instance?.CancelDefer(closeFlowId);
        ClearCloseContext();
        RefreshAccount();
        openingAmount = TxGameBridge.GoldAmount;
        openingTaskProgress = PhaseTaskService.GetCurrentSdkProgress();
        StopValueAnimation();
        amountText.rectTransform.localScale = amountBaseScale;
        RefreshAmount(0f);
        RefreshTaskProgress(0f, openingTaskProgress);
        RefreshTaskIcon(openingTaskProgress.PhaseId);
        taskText.text = PhaseTaskService.GetCurrentTaskText();
        RefreshAction();
        PlayValueAnimation();
    }

    public override void Hide()
    {
        CloseIntent intent = closeIntent;
        string flowId = closeFlowId;
        ClearCloseContext();
        StopActionPulse();
        StopValueAnimation();
        base.Hide();
        if (intent == CloseIntent.PlayerClose)
            PhaseFlowCoordinator.Instance?.CompleteDefer(flowId);
    }

    void OpenAccount()
    {
        TxGameBridge.PlaySfx();
        UIManager.Instance.OpenUI<AcCandyPop>(AccountProfileStore.Load());
    }

    void OpenRecords()
    {
        TxGameBridge.PlaySfx();
        UIManager.Instance.OpenUI<RecordPop>();
    }

    void Close()
    {
        TxGameBridge.PlaySfx();
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (flow != null
            && PhaseFlowCoordinator.Instance != null
            && PhaseFlowCoordinator.Instance.BeginDefer(flow.flowId))
        {
            closeIntent = CloseIntent.PlayerClose;
            closeFlowId = flow.flowId;
        }

        Hide();
    }

    void ClearCloseContext()
    {
        closeIntent = CloseIntent.None;
        closeFlowId = string.Empty;
    }

    void OnAccountProfileUpdated(AccountProfileData data) => RefreshAccount();

    void OnAmountChanged(float amount)
    {
        if (IsValueAnimating())
            return;

        RefreshAmount(amount);
        RefreshTask();
    }

    void OnCurrentPhaseChanged(PhaseId phaseId)
    {
        RefreshTask();
        RefreshAction();
    }

    void OnPhaseCompleted(PhaseId phaseId) => RefreshAction();

    void OnAction()
    {
        TxGameBridge.PlaySfx();
        if (!AccountProfileStore.HasCompleteProfile())
        {
            UIManager.Instance.OpenUI<AcCandyPop>(AccountProfileStore.Load());
            return;
        }

        if (!PhaseTaskService.IsCurrentPhaseCompleted)
        {
            TxGameBridge.Toast(PhaseTaskService.GetCurrentTaskText());
            return;
        }

        closeIntent = CloseIntent.StartFlow;
        PhaseFlowCoordinator.Instance.RunFromSdkPanel();
    }

    void RefreshAction()
    {
        if (actionPulseTarget == null)
            return;

        if (PhaseTaskService.IsCurrentPhaseCompleted)
        {
            if (actionPulse == null || !actionPulse.IsActive())
                PlayActionBeat();
            return;
        }

        StopActionPulse();
    }

    void PlayActionBeat()
    {
        StopActionPulse();
        Vector3 peak = actionPulseBaseScale * actionPulseScale;
        Sequence beat = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        beat.Append(actionPulseTarget.DOScale(peak, actionPulseGrow).SetEase(Ease.OutQuad));
        beat.Append(actionPulseTarget.DOScale(actionPulseBaseScale, actionPulseShrink).SetEase(Ease.InQuad));
        beat.Append(actionPulseTarget.DOScale(peak, actionPulseGrow).SetEase(Ease.OutQuad));
        beat.Append(actionPulseTarget.DOScale(actionPulseBaseScale, actionPulseShrink).SetEase(Ease.InQuad));
        beat.AppendInterval(actionPulsePause);
        beat.SetLoops(-1);
        actionPulse = beat;
    }

    void StopActionPulse()
    {
        actionPulse?.Kill(false);
        actionPulse = null;
        if (actionPulseTarget != null)
            actionPulseTarget.localScale = actionPulseBaseScale;
    }

    void RefreshAccount()
    {
        AccountProfileData profile = AccountProfileStore.Load();
        bool hasProfile = AccountProfileStore.HasCompleteProfile();
        login.SetActive(hasProfile);
        noLogin.SetActive(!hasProfile);
        if (!hasProfile)
            return;

        accountText.text = profile.Account;
        nameText.text = profile.Name;
        accountIcon.sprite = GetAccountIcon(profile.Type);
    }

    Sprite GetAccountIcon(AcType type) => type switch
    {
        AcType.Channel0 => channel0Icon,
        AcType.Channel1 => channel1Icon,
        AcType.Channel2 => channel2Icon,
        AcType.Channel3 => channel3Icon,
        _ => channel0Icon,
    };

    void PlayValueAnimation()
    {
        StopValueAnimation();
        amountText.rectTransform.localScale = amountBaseScale;
        RefreshAmount(0f);
        RefreshTaskProgress(0f, openingTaskProgress);

        amountTween = DOVirtual.Float(0f, openingAmount, valueAnimDuration, RefreshAmount)
            .SetEase(Ease.Linear)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        taskTween = DOVirtual.Float(0f, openingTaskProgress.Current, valueAnimDuration, value => RefreshTaskProgress(value, openingTaskProgress))
            .SetEase(Ease.Linear)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        Sequence scaleSequence = DOTween.Sequence();
        scaleSequence.Append(amountText.rectTransform.DOScale(amountBaseScale * amountMaxScale, amountGrowDuration).SetEase(Ease.OutBack));
        scaleSequence.AppendInterval(Mathf.Max(0f, valueAnimDuration - amountGrowDuration));
        scaleSequence.Append(amountText.rectTransform.DOScale(amountBaseScale, amountShrinkDuration).SetEase(Ease.OutBack));
        scaleSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        amountScaleTween = scaleSequence;
    }

    void StopValueAnimation()
    {
        amountTween?.Kill(false);
        amountScaleTween?.Kill(false);
        taskTween?.Kill(false);
        amountTween = null;
        amountScaleTween = null;
        taskTween = null;
        if (amountText != null)
            amountText.rectTransform.localScale = amountBaseScale;
    }

    bool IsValueAnimating() =>
        (amountTween != null && amountTween.IsActive()) || (taskTween != null && taskTween.IsActive());

    void RefreshAmount(float amount) => amountText.text = TxGameBridge.GetMark() + amount.ToString("F2", CultureInfo.InvariantCulture);

    void RefreshTask()
    {
        taskText.text = PhaseTaskService.GetCurrentTaskText();
        SdkTaskProgress progress = PhaseTaskService.GetCurrentSdkProgress();
        if (!IsValueAnimating())
            RefreshTaskProgress(progress.Current, progress);
        RefreshTaskIcon(progress.PhaseId);
    }

    void RefreshTaskProgress(float current, SdkTaskProgress progress)
    {
        taskProgressFill.fillAmount = progress.Target <= 0f ? 0f : Mathf.Clamp01(current / progress.Target);
        taskProgressText.text = FormatProgress(current, progress.Target);
    }

    static string FormatProgress(float current, float target) =>
        target <= 0f
            ? string.Empty
            : current.ToString(current % 1f == 0f ? "0" : "0.##", CultureInfo.InvariantCulture)
              + "/"
              + target.ToString(target % 1f == 0f ? "0" : "0.##", CultureInfo.InvariantCulture);

    void RefreshTaskIcon(PhaseId phaseId)
    {
        Sprite icon = phaseId switch
        {
            PhaseId.ActiveMinutes20 => task2Icon,
            PhaseId.Coin2000 => task3Icon,
            PhaseId.ActiveDays7 => task4Icon,
            PhaseId.PlayerLevel30 => task5Icon,
            _ => null,
        };
        taskIcon.gameObject.SetActive(icon != null);
        if (icon != null)
            taskIcon.sprite = icon;
    }
}
