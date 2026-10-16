using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardTip : UIBase
{
    public Text countText;
    public RectTransform flightOrigin;
    public Image icon;
    public CanvasGroup background;
    public float backgroundFadeDuration = 0.15f;
    public float stayDuration = 0.8f;
    public float iconGrowDuration = 0.4f;
    public float iconShrinkDuration = 0.2f;
    public float iconMaxScale = 1.35f;
    public Ease iconScaleEase = Ease.OutBack;
    public float goldRecoverDelay = 2f;

    RewardTipData data;
    Sequence sequence;

    private void Start()
    {
        GameManager.Instance.UpdateAppATTToDiamond(icon,false);
    }
    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        data = _data as RewardTipData;
        StopSequence();

        if (data == null)
        {
            Debug.LogError("[Tx] 奖励提示弹窗缺少有效数据");
            return;
        }

        countText.text = TxGameBridge.GetMark() + data.Amount.ToString("0.00", CultureInfo.InvariantCulture);

        if (background != null)
            background.alpha = 0f;
        if (icon != null)
            icon.transform.localScale = Vector3.zero;

        sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        if (background != null)
            sequence.Append(background.DOFade(1f, backgroundFadeDuration));
        if (icon != null)
        {
            sequence.Append(icon.transform.DOScale(Vector3.one * iconMaxScale, iconGrowDuration).SetEase(iconScaleEase));
            sequence.Append(icon.transform.DOScale(Vector3.one, iconShrinkDuration).SetEase(iconScaleEase));
        }
        sequence.AppendInterval(stayDuration);
        sequence.OnComplete(() => Hide());
    }

    public override void Hide()
    {
        StopSequence();

        RewardTipData finished = data;
        data = null;

        base.Hide();

        if (finished != null)
            ApplyReward(finished);
    }

    void ApplyReward(RewardTipData tip)
    {
        PlayerInfoUI playerInfoUI = tip.Amount > 0f ? UIManager.Instance.GetUI<PlayerInfoUI>() : null;

        if (tip.ApplyReward && tip.Amount > 0f)
            TxGameBridge.AddGold(tip.Amount);

        if (playerInfoUI == null)
        {
            tip.Completed?.Invoke();
            return;
        }

        playerInfoUI.GoldCanvasTop();
        playerInfoUI.GoldFlyAnim(flightOrigin != null ? flightOrigin.position : transform.position);

        DOTween.Sequence()
            .AppendInterval(goldRecoverDelay)
            .AppendCallback(() =>
            {
                playerInfoUI.GoldCanvasRecover();
                tip.Completed?.Invoke();
            })
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    void StopSequence()
    {
        if (sequence == null)
            return;

        sequence.Kill();
        sequence = null;
    }
}
