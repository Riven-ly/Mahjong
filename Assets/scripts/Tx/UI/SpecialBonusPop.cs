using System;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpecialBonusPopData
{
    public float DisplayReward { get; }
    public Action Claimed { get; }

    public SpecialBonusPopData(float displayReward, Action claimed)
    {
        DisplayReward = displayReward;
        Claimed = claimed;
    }
}

public sealed class SpecialBonusPop : UIBase
{
    public Text rewardAmountText;
    public Button claimButton;
    public float valueAnimDuration = 0.5f;

    SpecialBonusPopData data;
    Action claimedCallback;
    bool claimed;
    Tween amountTween;

    void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(Claim);
    }

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        data = _data as SpecialBonusPopData;
        claimed = false;
        StopAmountAnimation();
        if (data == null)
        {
            rewardAmountText.text = string.Empty;
            claimButton.interactable = false;
            Debug.LogError("[Tx] 特殊奖励弹窗缺少有效数据");
            return;
        }

        RefreshAmount(0f);
        claimButton.interactable = true;
        PlayAmountAnimation();
    }

    void Claim()
    {
        if (claimed)
            return;

        TxGameBridge.PlaySfx();
        claimed = true;
        claimButton.interactable = false;
        claimedCallback = data.Claimed;
        Hide();
    }

    public override void Hide()
    {
        StopAmountAnimation();
        Action callback = claimedCallback;
        claimedCallback = null;
        base.Hide();
        callback?.Invoke();
    }

    void OnDisable()
    {
        isOpen = false;
        StopAmountAnimation();
    }

    void PlayAmountAnimation()
    {
        StopAmountAnimation();
        RefreshAmount(0f);
        amountTween = DOVirtual.Float(0f, data.DisplayReward, valueAnimDuration, RefreshAmount)
            .SetEase(Ease.Linear)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    void StopAmountAnimation()
    {
        amountTween?.Kill(false);
        amountTween = null;
    }

    void RefreshAmount(float amount)
    {
        bool integer = data != null && data.DisplayReward % 1f == 0f;
        rewardAmountText.text = TxGameBridge.GetMark() + amount.ToString(integer ? "N0" : "N2", CultureInfo.InvariantCulture);
    }
}
