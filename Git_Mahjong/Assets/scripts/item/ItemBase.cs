using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ItemType
{
    Gold,
    GoldDui,
    Diamond,
    DiamondDui,
    Hint,// 提示
    Exchange,// 洗牌
    Extract,// 魔法棒
    Return,// 撤回
}

public class ItemData
{
    public ItemType itemType;
    public int count;
    public ItemData(ItemType _itemType, int _count)
    {
        itemType = _itemType;
        count = _count;
    }
}
public class ItemBase : MonoBehaviour
{
    public List<Sprite> icons;
    public List<float> iconScales;
    public Image icon;
    public Text cntText;
    public Transform effect;

    [HideInInspector] public ItemType itemType;
    [HideInInspector] public int count;

    public virtual void Init(ItemData _itemData)
    {
        itemType = _itemData.itemType;
        count = _itemData.count;

        icon.sprite = icons[(int)itemType];
        icon.SetNativeSize();
        icon.transform.localScale = Vector3.one * iconScales[(int)itemType];

        if(itemType == ItemType.Hint || itemType == ItemType.Exchange || itemType == ItemType.Extract || itemType == ItemType.Return)
        {
            cntText.text = "x" + count;
        }
        else
        {
            string unit = LanguageManager.Instance.GetText_Encrypt("Special_Diamond__unit");
            cntText.text = unit + MathF.Round(count / (float)PlayerInfo.CurrencyUnitScale, 2);
        }

        if (itemType == ItemType.Gold)
        {
            GameManager.Instance.UpdateAppATTToDiamond(icon);
        }
        else if(itemType == ItemType.GoldDui)
        {
            GameManager.Instance.UpdateAppATTToDiamondDui(icon);
        }
    }

    public  void GetItemReward()
    {
        switch (itemType)
        {
            case ItemType.Gold:
                GameManager.Instance.playerInfo.Add_gold(count);
                break;
            case ItemType.GoldDui:
                GameManager.Instance.playerInfo.Add_gold(count);
                break;
            case ItemType.Diamond:
                GameManager.Instance.playerInfo.Add_diamond(count);
                break;
            case ItemType.DiamondDui:
                GameManager.Instance.playerInfo.Add_diamond(count);
                break;
            case ItemType.Hint:
                GameManager.Instance.playerInfo.Add_item_hint(count);
                break;
            case ItemType.Exchange:
                GameManager.Instance.playerInfo.Add_item_exchange(count);
                break;
            case ItemType.Extract:
                GameManager.Instance.playerInfo.Add_item_extract(count);
                break;
            case ItemType.Return:
                GameManager.Instance.playerInfo.Add_item_return(count);
                break;
        }


    }

    public void PlayItemAnim()
    {
        switch (itemType)
        {
            case ItemType.Gold:
                UIManager.Instance.GetUI<PlayerInfoUI>().GoldFlyAnim(transform.position);
                break;
            case ItemType.GoldDui:
                UIManager.Instance.GetUI<PlayerInfoUI>().GoldFlyAnim(transform.position);
                break;
            case ItemType.Diamond:
                UIManager.Instance.GetUI<PlayerInfoUI>().DiamondFlyAnim(transform.position);
                break;
            case ItemType.DiamondDui:
                UIManager.Instance.GetUI<PlayerInfoUI>().DiamondFlyAnim(transform.position);
                break;
            case ItemType.Hint:
                UIManager.Instance.GetUI<GameScenePanel>()?.gameSceneItem_Hint.Refresh();
                break;
            case ItemType.Exchange:
                UIManager.Instance.GetUI<GameScenePanel>()?.gameSceneItem_Exchange.Refresh();
                break;
            case ItemType.Return:
                UIManager.Instance.GetUI<GameScenePanel>()?.gameSceneItem_Return.Refresh();
                break;
        }
    }
}
