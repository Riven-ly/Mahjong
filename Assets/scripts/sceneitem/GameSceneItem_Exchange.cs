using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameSceneItem_Exchange : GameSceneItemBase
{
    /// <summary>
    /// 刷新洗牌道具的数量与解锁状态。
    /// </summary>
    public override void Refresh()
    {
        base.Refresh();
        cnt = GameManager.Instance.playerInfo.gameSceneItem_Exchange;
        type = SceneItemType.item_Exchange;
        lockLv = 1;

        bool isLock = GameManager.Instance.playerInfo.level < lockLv;

        unLockTrans.gameObject.SetActive(!isLock);
        lockTrans.gameObject.SetActive(isLock);

        cntStr.text = cnt <= 0 ? "+" : GameManager.Instance.playerInfo.gameSceneItem_Exchange.ToString();

        clickBtn.gameObject.SetActive(cnt > 0);
        cntStr.gameObject.SetActive(cnt > 0);
        rewardAdButton.gameObject.SetActive(cnt <= 0);
        rewardAdButton.Init(AdsCallback, "", false);
    }

    public override void AdsCallback()
    {
        base.AdsCallback();

        GameManager.Instance.playerInfo.Add_item_exchange(1);
        GameManager.Instance.SavePlayerInfo();
        DOTween.Sequence().AppendInterval(0.1f).AppendCallback(() =>
        {
            Refresh();
        });
    }

    /// <summary>
    /// 使用洗牌道具，随机交换游戏区域内卡牌的位置。
    /// </summary>
    public override void OnClick()
    {
        base.OnClick();
        if (cnt <= 0)
        {
            //UIManager.Instance.OpenUI<AddSceneItemPanel>(this);
            return;
        }

        EventManager.Instance.TriggerEvent(GameEvent.StopHintAnim);
        bool isUseItemSucceed = TryExchangeAllPlayingCard();
        if (isUseItemSucceed)
        {
            OtherSdkManager.Instance.CustomEvent("prop_use", "level_id", GameManager.Instance.playerInfo.level, "prop_id_number", 2);
            GameManager.Instance.playerInfo.Minus_item_exchange(1);
            //GameManager.Instance.SavePlayerInfo();
            Refresh();
        }
    }



    /// <summary>
    /// 尝试随机交换游戏区域内全部卡牌的位置。
    /// </summary>
    public bool TryExchangeAllPlayingCard()
    {
        return UIManager.Instance.GetUI<GameScenePanel>().TryShuffleMahjongCards();
    }

  
}
