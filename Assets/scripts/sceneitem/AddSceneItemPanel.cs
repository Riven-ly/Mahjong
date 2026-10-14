using UnityEngine;
using UnityEngine.UI;

public class AddSceneItemPanel : UIBase
{
    public Image icon;
    public Sprite hintIcon;
    public Sprite exchangeIcon;
    public Sprite returnIcon;
    public RewardAdButton rewardAdButton;
    public Button hideBtn;

    GameSceneItemBase sourceItem;

    private void Start()
    {
        hideBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            Hide();
        });
    }
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        sourceItem = data as GameSceneItemBase;
        ApplyIcon();
        rewardAdButton.Init(AdsCallback, "", false);
    }

    void ApplyIcon()
    {
        SceneItemType type = sourceItem == null ? SceneItemType.item_Exchange : sourceItem.type;
        Sprite sprite = exchangeIcon;
        if (type == SceneItemType.item_hint)
            sprite = hintIcon;
        else if (type == SceneItemType.item_Return)
            sprite = returnIcon;
        if (sprite != null)
            icon.sprite = sprite;

        icon.SetNativeSize();
    }

    void AdsCallback()
    {
        if (sourceItem == null)
            return;

        PlayerInfo playerInfo = GameManager.Instance.playerInfo;
        if (sourceItem.type == SceneItemType.item_hint)
            playerInfo.Add_item_hint(3);
        else if (sourceItem.type == SceneItemType.item_Return)
            playerInfo.Add_item_return(3);
        else
            playerInfo.Add_item_exchange(3);

        GameManager.Instance.SavePlayerInfo();
        sourceItem.Refresh();
        Hide();
    }
}
