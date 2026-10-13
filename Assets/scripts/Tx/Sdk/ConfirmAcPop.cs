using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class ConfirmAcPop : UIBase
{
    public TextMeshProUGUI accountText;
    public TextMeshProUGUI nameText;
    public Image accountTypeIcon;
    public Sprite channel0Icon;
    public Sprite channel1Icon;
    public Sprite channel2Icon;
    public Sprite channel3Icon;
    public Button submitButton;
    public Button closeButton;

    AccountProfileData data;

    void Awake()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);
        if (closeButton != null)
            closeButton.onClick.AddListener(ReturnToEdit);
    }

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        data = _data as AccountProfileData;
        if (data == null)
        {
            Debug.LogError("[Tx] 账号确认弹窗缺少资料数据");
            return;
        }

        accountText.text = data.Account;
        nameText.text = data.Name;
        accountTypeIcon.sprite = GetAccountTypeIcon(data.Type);
    }

    Sprite GetAccountTypeIcon(AcType type) => type switch
    {
        AcType.Channel0 => channel0Icon,
        AcType.Channel1 => channel1Icon,
        AcType.Channel2 => channel2Icon,
        AcType.Channel3 => channel3Icon,
        _ => channel0Icon,
    };

    void Submit()
    {
        if (data == null)
            return;

        AccountProfileStore.Save(data);
        SdkCandyEvents.RaiseAccountProfileUpdated(data);
        Hide();
    }

    void ReturnToEdit()
    {
        if (data == null)
            return;

        Hide();
        UIManager.Instance.OpenUI<AcCandyPop>(data);
    }
}
