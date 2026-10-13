using DG.Tweening;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class RewardTipPanel : UIBase
{
    public TextMeshProUGUI countText;
    public RectTransform icon;
    public Button confirmButton;
    public float iconGrowDuration = 0.25f;
    public float iconShrinkDuration = 0.2f;
    public float iconMaxScale = 1.35f;
    public Ease iconScaleEase = Ease.OutBack;

    RewardTipPanelData data;
    Tween iconTween;

    void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
    }

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        data = _data as RewardTipPanelData;
        if (data == null)
        {
            Debug.LogError("[Tx] 奖励提示弹窗缺少有效数据");
            return;
        }

        countText.text = TxGameBridge.GetMark() + data.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        iconTween?.Kill(false);
        icon.localScale = Vector3.zero;
        iconTween = DOTween.Sequence()
            .Append(icon.DOScale(Vector3.one * iconMaxScale, iconGrowDuration).SetEase(iconScaleEase))
            .Append(icon.DOScale(Vector3.one, iconShrinkDuration).SetEase(iconScaleEase))
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public override void Hide()
    {
        iconTween?.Kill(false);
        iconTween = null;
        base.Hide();
    }

    void Confirm()
    {
        Hide();
        data?.Completed?.Invoke();
    }
}
