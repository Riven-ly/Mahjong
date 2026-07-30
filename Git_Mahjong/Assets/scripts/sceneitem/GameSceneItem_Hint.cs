using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneItem_Hint : GameSceneItemBase
{

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
        else
        {
            string str = LanguageManager.Instance.GetText("NoItemHintTips");
            UIManager.Instance.OpenUI<GeneralTipsPanel>(str);
        }
    }

    private bool TryHintAnim()
    {
        return false;
    }
}
