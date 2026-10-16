using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ProcessCompletePopData
{
    public Action Continued { get; }

    public ProcessCompletePopData(Action continued) => Continued = continued;
}

public sealed class ProcessCompletePop : UIBase
{
    public Text displayText;
    public Button continueButton;

    ProcessCompletePopData data;
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
        data = _data as ProcessCompletePopData;
        continued = false;
        displayText.text = "Process complete!";
        continueButton.interactable = data != null;
        if (data == null)
            Debug.LogError("[Tx] 完成弹窗缺少有效数据");
    }

    void Continue()
    {
        TxGameBridge.PlaySfx();
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

    public override void Hide()
    {
        AddCallback(() =>
        {
            Action callback = continuedCallback;
            continuedCallback = null;
            callback?.Invoke();
        });
    
        base.Hide();
    }
}
