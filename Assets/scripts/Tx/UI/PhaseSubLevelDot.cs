using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class PhaseSubLevelDot : MonoBehaviour
{
    public RectTransform root;
    public Image completeProgress;
    public float currentScale = 1.2f;
    public float scaleDuration = 0.2f;

    Tween scaleTween;

    public void Bind(bool completed, bool current, bool showPercent, float percent)
    {
        completeProgress.fillAmount = current && showPercent
            ? Mathf.Clamp01(percent)
            : completed || current ? 1f : 0f;

        if (current)
            PlayScale();
        else
            StopScale();
    }

    public void Reset()
    {
        StopScale();
        completeProgress.fillAmount = 0f;
    }

    void PlayScale()
    {
        if (scaleTween != null && scaleTween.IsActive())
            return;

        root.localScale = Vector3.one;
        scaleTween = root.DOScale(Vector3.one * currentScale, scaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    void StopScale()
    {
        scaleTween?.Kill(false);
        scaleTween = null;
        root.localScale = Vector3.one;
    }

    void OnDisable()
    {
        StopScale();
    }
}
