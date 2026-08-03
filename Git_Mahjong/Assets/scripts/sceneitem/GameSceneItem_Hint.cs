using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneItem_Hint : GameSceneItemBase
{

    /// <summary>
    /// 刷新提示道具的数量与解锁状态。
    /// </summary>
    public override void Refresh()
    {
        base.Refresh();
        cnt = GameManager.Instance.playerInfo.gameSceneItem_Hint;
        type = SceneItemType.item_hint;
        lockLv = 1;

        bool isLock = GameManager.Instance.playerInfo.level < lockLv;

        unLockTrans.gameObject.SetActive(!isLock);
        lockTrans.gameObject.SetActive(isLock);

        cntStr.text = cnt <= 0 ? "+" : GameManager.Instance.playerInfo.gameSceneItem_Hint.ToString();

    }

    /// <summary>
    /// 使用提示道具，显示一组可消除的牌面卡牌。
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
        bool isUseItemSucceed = TryHintAnim();
        if (isUseItemSucceed)
        {
            GameManager.Instance.playerInfo.Minus_item_hint(1);
            //GameManager.Instance.SavePlayerInfo();
            Refresh();
        }
    }

    /// <summary>
    /// 尝试显示一组可消除卡牌的提示特效。
    /// </summary>
    private bool TryHintAnim()
    {
        return UIManager.Instance.GetUI<GameScenePanel>().TryShowMahjongHint();
    }
}
