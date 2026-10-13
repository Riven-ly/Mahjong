using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UiJellyClickAnimation : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    public float deformation = 0.12f;
    public float squashDuration = 0.07f;
    public float stretchDuration = 0.09f;
    public float settleDuration = 0.22f;
    public bool useUnscaledTime = true;
    public Transform animationTarget;

    Button button;
    Toggle toggle;
    Sequence jellySequence;
    Vector3 baseScale;

    void Awake()
    {
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
        ResolveTarget();
        CaptureBaseScale();
    }

    void OnEnable()
    {
        ResolveTarget();
        CaptureBaseScale();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
            Play();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (IsInteractable())
            Play();
    }

    public void Play()
    {
        ResolveTarget();
        if (animationTarget == null)
            return;

        bool wasPlaying = jellySequence != null && jellySequence.IsActive();
        if (wasPlaying)
        {
            jellySequence.Kill(false);
            animationTarget.localScale = baseScale;
        }
        else
        {
            CaptureBaseScale();
        }

        float amount = Mathf.Clamp(deformation, 0f, 0.5f);
        Vector3 squashScale = Vector3.Scale(baseScale, new Vector3(1f + amount, 1f - amount * 0.75f, 1f));
        Vector3 stretchScale = Vector3.Scale(baseScale, new Vector3(1f - amount * 0.55f, 1f + amount * 0.65f, 1f));

        jellySequence = DOTween.Sequence();
        jellySequence.SetUpdate(useUnscaledTime);
        jellySequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        jellySequence.Append(animationTarget.DOScale(squashScale, squashDuration).SetEase(Ease.OutQuad));
        jellySequence.Append(animationTarget.DOScale(stretchScale, stretchDuration).SetEase(Ease.OutQuad));
        jellySequence.Append(animationTarget.DOScale(baseScale, settleDuration).SetEase(Ease.OutElastic));
        jellySequence.OnComplete(() =>
        {
            animationTarget.localScale = baseScale;
            jellySequence = null;
        });
    }

    bool IsInteractable()
    {
        bool buttonCanClick = button != null && button.enabled && button.interactable;
        bool toggleCanClick = toggle != null && toggle.enabled && toggle.interactable;
        return isActiveAndEnabled && (buttonCanClick || toggleCanClick);
    }

    void ResolveTarget()
    {
        if (animationTarget == null)
            animationTarget = transform;
    }

    void CaptureBaseScale()
    {
        if (animationTarget != null)
            baseScale = animationTarget.localScale;
    }

    void StopAndRestore()
    {
        jellySequence?.Kill(false);
        jellySequence = null;

        if (animationTarget != null)
            animationTarget.localScale = baseScale;
    }

    void OnDisable()
    {
        StopAndRestore();
    }

    void OnDestroy()
    {
        jellySequence?.Kill(false);
        jellySequence = null;
    }

    void Reset()
    {
        animationTarget = transform;
    }
}
