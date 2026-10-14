using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class PhaseTaskPanel : UIBase
{
    public Button closeButton;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI descriptionLabel;
    public TextMeshProUGUI progressLabel;
    public PhaseTaskItem[] items;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClick);
    }

    void OnCloseClick()
    {
        TxGameBridge.PlaySfx();
        Hide();
    }
}
