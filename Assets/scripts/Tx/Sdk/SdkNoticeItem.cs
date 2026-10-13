using UnityEngine;
using TMPro;
using UnityEngine.UI;

public readonly struct SdkNoticeData
{
    public string User { get; }
    public Sprite Icon { get; }
    public string Amount { get; }
    public string Date { get; }

    public SdkNoticeData(string user, Sprite icon, string amount, string date)
    {
        User = user;
        Icon = icon;
        Amount = amount;
        Date = date;
    }
}

public sealed class SdkNoticeItem : MonoBehaviour
{
    public TextMeshProUGUI userText;
    public Image accountIcon;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI dateText;

    public void Refresh(SdkNoticeData data)
    {
        userText.text = data.User;
        accountIcon.sprite = data.Icon;
        if (data.Amount != null)
        {
            amountText.text = data.Amount;
        }
        if (data.Date != null)
        {
            dateText.text = data.Date;
        }
    }
}
