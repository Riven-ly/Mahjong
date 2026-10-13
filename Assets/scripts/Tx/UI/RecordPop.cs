using UnityEngine.UI;

public sealed class RecordPop : UIBase
{
    public Button closeButton;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    void Close() => Hide();
}
