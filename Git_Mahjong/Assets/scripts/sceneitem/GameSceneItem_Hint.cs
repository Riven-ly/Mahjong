using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneItem_Hint : GameSceneItemBase
{
    public AudioSource audioSource;
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

        clickBtn.gameObject.SetActive(cnt > 0);
        cntStr.gameObject.SetActive(cnt > 0);
        rewardAdButton.gameObject.SetActive(cnt <= 0);
        rewardAdButton.Init(AdsCallback, "", false);
    }

    public override void AdsCallback()
    {
        base.AdsCallback();

        GameManager.Instance.playerInfo.Add_item_hint(1);
        GameManager.Instance.SavePlayerInfo();
        DOTween.Sequence().AppendInterval(0.1f).AppendCallback(() =>
        {
            Refresh();
        });
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
            AudioManager.Instance.SetAudioSource(audioSource, "hintitem");
            OtherSdkManager.Instance.CustomEvent("prop_use", "level_id", GameManager.Instance.playerInfo.level, "prop_id_number", 1);
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
