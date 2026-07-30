using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameSceneItem_Exchange : GameSceneItemBase
{
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

    }

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
            GameManager.Instance.playerInfo.Minus_item_exchange(1);
            //GameManager.Instance.SavePlayerInfo();
            Refresh();
        }
    }



    public bool TryExchangeAllPlayingCard()
    {
        return true;
    }

  
}
