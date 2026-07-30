using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneItem_Return : GameSceneItemBase
{
    public override void Refresh()
    {
        base.Refresh();
        cnt = GameManager.Instance.playerInfo.gameSceneItem_Return;
        type = SceneItemType.item_Return;
        lockLv = 1;

        bool isLock = GameManager.Instance.playerInfo.level < lockLv;

        unLockTrans.gameObject.SetActive(!isLock);
        lockTrans.gameObject.SetActive(isLock);

        cntStr.text = cnt <= 0 ? "+" : GameManager.Instance.playerInfo.gameSceneItem_Return.ToString();
        Update_ItemReturnInfo();
    }

    private void Update_ItemReturnInfo()
    {
        // clickBtn.interactable = GameStepRecord.Instance.steps.Count > 0;
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
        bool isUseItemSucceed = true;
        if (isUseItemSucceed)
        {
            GameManager.Instance.playerInfo.Minus_item_return(1);
            //GameManager.Instance.SavePlayerInfo();
            Refresh();
        }
    }
}
