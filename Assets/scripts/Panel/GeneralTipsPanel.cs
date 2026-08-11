using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralTipsPanel : UIBase
{
    public Transform trans;
    public CanvasGroup canvasGroup;
    public Text tipText;
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        tipText.text = (string)data;

        canvasGroup.alpha = 1;
        trans.localScale = Vector3.one;

        DOTween.Kill(this);
        DOTween.Sequence()
            .Append(trans.DOScale(1.1f, 0.1f))
            .Append(trans.DOScale(1f, 0.1f))
            .AppendInterval(1.1f)
            .Append(canvasGroup.DOFade(0f, 0.2f))
            .AppendCallback(() =>
            {
                Hide();
            }).SetTarget(this);
    }
    public override void Hide()
    {
        base.Hide();
    }
}
