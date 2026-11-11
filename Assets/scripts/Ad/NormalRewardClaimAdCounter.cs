using UnityEngine;

/// <summary>
/// 按普通领取按钮的点击次数触发广告，与关卡进入、完成或面板关闭次数无关。
/// </summary>
public static class NormalRewardClaimAdCounter
{
    private const string UiClickCounterInitializedKey = "NormalRewardClaimAdCounter.UiClickModeInitialized";
    private const int TriggerClaimCount = 3;
    private const int InterstitialChancePercent = 65;

    /// <summary>
    /// 通用奖励或通关奖励每点击一次普通领取调用一次；第3次触发广告并重新计数。
    /// </summary>
    public static void RegisterNormalClaim(string pageId)
    {
        PlayerInfo playerInfo = GameManager.Instance.playerInfo;
        EnsureUiClickCounterInitialized(playerInfo);

        playerInfo.consecutiveNoAdCount++;
        bool shouldShowAd = playerInfo.consecutiveNoAdCount >= TriggerClaimCount;
        Debug.Log($"[AdCount] 普通领取计数={playerInfo.consecutiveNoAdCount}/{TriggerClaimCount}，页面={pageId}");

        if (shouldShowAd)
        {
            playerInfo.consecutiveNoAdCount = 0;
        }
        GameManager.Instance.SavePlayerInfo();

        if (!shouldShowAd)
        {
            return;
        }

        int randomValue = Random.Range(0, 100);
        if (randomValue < InterstitialChancePercent)
        {
            Debug.Log($"[AdCount] 第3次普通领取，随机值={randomValue}，触发插屏广告");
            AdManager.Instance.OnClickInterstitialAd(pageId);
        }
        else
        {
            Debug.Log($"[AdCount] 第3次普通领取，随机值={randomValue}，触发激励广告");
            AdManager.Instance.ShowRewardedAd(pageId, null, null);
        }
    }

    /// <summary>
    /// 首次切换到界面点击计数时，清除旧版本按关卡累计留下的计数。
    /// </summary>
    private static void EnsureUiClickCounterInitialized(PlayerInfo playerInfo)
    {
        if (PlayerPrefs.GetInt(UiClickCounterInitializedKey, 0) != 0)
        {
            return;
        }

        playerInfo.consecutiveNoAdCount = 0;
        PlayerPrefs.SetInt(UiClickCounterInitializedKey, 1);
        PlayerPrefs.Save();
    }
}
