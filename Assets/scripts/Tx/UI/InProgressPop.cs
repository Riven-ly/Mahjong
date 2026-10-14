using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class InProgressPopData
{
    public PhaseId PhaseId { get; }
    public Action Continued { get; }

    public InProgressPopData(PhaseId phaseId, Action continued)
    {
        PhaseId = phaseId;
        Continued = continued;
    }
}

public sealed class InProgressPop : UIBase
{
    public Image progressFill;
    public Toggle toggle1;
    public Toggle toggle2;
    public GameObject[] step1Titles;
    public GameObject[] step2Titles;
    public GameObject[] displayTitles;
    public Button continueButton;
    public float fillTime = 1f;
    public float step2Delay = 0.15f;

    InProgressPopData data;
    Sequence sequence;
    Action continuedCallback;
    bool continued;

    void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(Continue);
    }

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        data = _data as InProgressPopData;
        continued = false;
        sequence?.Kill(false);
        if (data == null)
        {
            Debug.LogError("[Tx] 进度弹窗缺少有效数据");
            Hide();
            return;
        }

        int index = Mathf.Clamp((int)data.PhaseId - (int)PhaseId.Level8, 0, step1Titles.Length - 1);
        progressFill.fillAmount = 0f;
        toggle1.gameObject.SetActive(true);
        toggle1.isOn = true;
        toggle2.gameObject.SetActive(false);
        toggle2.isOn = false;
        SetActive(step1Titles, index);
        SetActive(step2Titles, -1);
        SetActive(displayTitles, index);
        continueButton.gameObject.SetActive(false);
        continueButton.interactable = false;
        sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        sequence.Append(progressFill.DOFillAmount(1f, fillTime).SetEase(Ease.InOutCubic));
        sequence.AppendCallback(() => toggle2.gameObject.SetActive(true));
        sequence.AppendInterval(step2Delay);
        sequence.AppendCallback(() =>
        {
            SetActive(step2Titles, index);
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        });
    }

    public override void Hide()
    {
        AddCallback(() =>
        {
            sequence?.Kill(false);
            sequence = null;
            Action callback = continuedCallback;
            continuedCallback = null;
            callback?.Invoke();
        });
        base.Hide();
    }

    void Continue()
    {
        AddCallback(() =>
        {
            if (continued)
                return;

            continued = true;
            continueButton.interactable = false;
            continuedCallback = data.Continued;
        });
        Hide();
    }

    void OnDisable()
    {
        sequence?.Kill(false);
        sequence = null;
    }

    static void SetActive(GameObject[] objects, int activeIndex)
    {
        for (int index = 0; index < objects.Length; index++)
            objects[index].SetActive(index == activeIndex);
    }
}
