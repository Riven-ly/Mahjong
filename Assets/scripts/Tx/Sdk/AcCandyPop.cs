using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class AcCandyPop : UIBase
{
    public Button closeButton;
    public Button submitButton;
    public TMP_InputField candyAcInputField;
    public TMP_InputField nameInputField;
    public AcSelectContainer[] typeSelectors;
    public AcType selectedAcType;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClick);
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitButtonClick);
        SdkCandyEvents.SelectedAcChanged += ApplySelectedAc;
    }

    void OnDestroy() => SdkCandyEvents.SelectedAcChanged -= ApplySelectedAc;

    public override void Refresh(object _data = null)
    {
        base.Refresh(_data);
        if (_data is AccountProfileData draft)
        {
            candyAcInputField.text = draft.Account;
            nameInputField.text = draft.Name;
            SelectType(draft.Type);
            return;
        }

        AccountProfileData profile = AccountProfileStore.Load();
        candyAcInputField.text = profile.Account;
        nameInputField.text = profile.Name;
        SelectType(profile.Type);
    }

    void ApplySelectedAc(Sprite sprite, AcType acType) => selectedAcType = acType;

    void SelectType(AcType type)
    {
        for (int index = 0; index < typeSelectors.Length; index++)
        {
            if (typeSelectors[index].Type == type)
                typeSelectors[index].Select();
        }

        for (int index = 0; index < typeSelectors.Length; index++)
            typeSelectors[index].SyncVisual();
    }

    void OnSubmitButtonClick()
    {
        string account = candyAcInputField.text.Trim();
        string name = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(account))
        {
            ShowErrorPop("Please enter your account");
            return;
        }

        if (!IsValidEmailOrPhone(account))
        {
            ShowErrorPop("Invalid email or phone number");
            return;
        }

        if (string.IsNullOrEmpty(name))
        {
            ShowErrorPop("Please enter your name");
            return;
        }

        Hide();
        UIManager.Instance.OpenUI<ConfirmAcPop>(new AccountProfileData(account, name, selectedAcType));
    }

    public void OnCloseButtonClick() => Hide();

    static bool IsValidEmailOrPhone(string input)
    {
        return Regex.IsMatch(input, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
               || Regex.IsMatch(input, @"^\d{8,}$");
    }

    void ShowErrorPop(string text) => TxGameBridge.Toast(text);
}
