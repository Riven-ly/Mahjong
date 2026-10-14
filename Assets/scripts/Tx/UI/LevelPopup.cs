using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class LevelPopup : UIBase
{
    public TextMeshProUGUI percentText;
    public TextMeshProUGUI levelText;
    public Image progressFill;
    public Button closeButton;
    public Button continueButton;
    public float valueAnimDuration = 0.5f;

    Tween progressTween;
    float targetFill;

    void Awake()
    {
        closeButton.onClick.AddListener(() => Hide());
        continueButton.onClick.AddListener(ReturnToSettings);
    }

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        StopProgressAnimation();
        PlayerLevelProgress progress = PhaseTaskService.GetPlayerLevelProgress();
        levelText.text = "Player Level:" + progress.Level;
        targetFill = progress.IsMaxLevel ? 1f : progress.InLevelNormalized;
        RefreshProgress(0f);
        PlayProgressAnimation();
    }

    public override void Hide()
    {
        StopProgressAnimation();
        base.Hide();
    }

    void OnDisable()
    {
        isOpen = false;
        StopProgressAnimation();
    }

    void PlayProgressAnimation()
    {
        StopProgressAnimation();
        RefreshProgress(0f);
        progressTween = DOVirtual.Float(0f, targetFill, valueAnimDuration, RefreshProgress)
            .SetEase(Ease.Linear)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    void StopProgressAnimation()
    {
        progressTween?.Kill(false);
        progressTween = null;
    }

    void RefreshProgress(float fill)
    {
        progressFill.fillAmount = fill;
        percentText.text = Mathf.RoundToInt(fill * 100f) + "%";
    }

    void ReturnToSettings()
    {
        Hide();
    }
}
