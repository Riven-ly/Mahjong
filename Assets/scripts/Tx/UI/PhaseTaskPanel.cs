using UnityEngine;
using UnityEngine.UI;

public sealed class PhaseTaskPanel : UIBase
{
    public Button closeButton;
    public Text titleLabel;
    public Text descriptionLabel;
    public Text progressLabel;
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
