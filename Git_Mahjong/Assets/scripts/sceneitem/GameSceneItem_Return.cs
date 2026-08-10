using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneItem_Return : GameSceneItemBase
{
    /// <summary>
    /// 刷新撤销道具的数量与解锁状态。
    /// </summary>
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

    /// <summary>
    /// 使用返回道具，连续自动消除五组可操作同类型牌。
    /// </summary>
    public override void OnClick()
    {
        base.OnClick();
        if (cnt <= 0)
        {
            return;
        }

        bool isUseItemSucceed = UIManager.Instance.GetUI<GameScenePanel>().TryAutoEliminateMahjongPairs();
        if (isUseItemSucceed)
        {
            OtherSdkManager.Instance.CustomEvent("prop_use", "level_id", GameManager.Instance.playerInfo.level, "prop_id_number", 3);
            GameManager.Instance.playerInfo.Minus_item_return(1);
            Refresh();
        }
    }
}
