using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏胜利结算面板。
/// </summary>
public class GameWinPanel : UIBase
{
    public Transform root;
    public Transform itemRoot;
    public RewardAdButton rewardAdButton;
    public Button collectBtn;
    public Text ad_text;
    public Text c_text;

    private List<ItemData> itemDatas;
    private List<ItemBase> itemBase;

    private int ad_V;
    private int c_V;
    private string page_id = "GameWinPanel";
    private void Awake()
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        float topBlockHeight = Screen.height - Screen.safeArea.yMax;
        rect.offsetMax = new Vector2(0, -topBlockHeight);
    }
    private void OnEnable()
    {
        isOpen = true;
    }
    private void OnDisable()
    {
        isOpen = false;
        ResetPanel();
    }
    /// <summary>
    /// 注册进入下一关按钮事件。
    /// </summary>
    private void Start()
    {
        collectBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            AdManager.Instance.OnClickInterstitialAd(page_id);
            CollectClick();
        });
    }

    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        ad_V = GameManager.Instance.GetReward();
        c_V = (int)(ad_V * (UnityEngine.Random.Range(30, 51) / 100f));

        itemDatas = new List<ItemData>();
        itemDatas.Add(new ItemData(ItemType.GoldDui, ad_V));
        itemBase = GameManager.Instance.CreatItems(itemDatas, itemRoot);

        ad_text.text = $"100%{LanguageManager.Instance.GetText("CLAIM")}";
        c_text.text = $"{LanguageManager.Instance.GetText("ONLY")} {LanguageManager.Instance.GetText_Encrypt("Special_Diamond__unit")}{MathF.Round(c_V / (float)PlayerInfo.CurrencyUnitScale, 2)}";

        rewardAdButton.Init(AdsCallback, page_id, true);
        AudioManager.Instance.PlaySceneSingleMusic("gamewin");
        OtherSdkManager.Instance.CustomEvent("level_complete", "level_id", GameManager.Instance.playerInfo.level);
        OtherSdkManager.Instance.CustomEvent("rewards_show", "show", "");
    }
    /// <summary>
    /// 领取激励广告通关奖励并进入下一关。
    /// </summary>
    private void AdsCallback()
    {
        OtherSdkManager.Instance.CustomEvent("rewards_click", "type", "claim_two");
        ShowRewardTip(ad_V, 10);
    }

    /// <summary>
    /// 领取普通通关奖励并进入下一关。
    /// </summary>
    private void CollectClick()
    {
        OtherSdkManager.Instance.CustomEvent("rewards_click", "type", "claim_one");
        ShowRewardTip(c_V, 1);
    }

    private void ShowRewardTip(int amount, int experience)
    {
        AddCallback(() =>
        {
            UIManager.Instance.OpenUI<RewardTip>(new RewardTipData(
                      amount / (float)PlayerInfo.CurrencyUnitScale,
                      () =>
                      {
                          GameManager.Instance.playerInfo.AddGoldExperience(experience);
                          OnNextLevelButtonClick();
                      }));
        });

        Hide();
    }
    public override void Hide()
    {
        base.Hide();
    }

    /// <summary>
    /// 处理进入下一关操作。
    /// </summary>
    private void OnNextLevelButtonClick()
    {
        GameManager.Instance.playerInfo.level++;
        PhaseTaskService.NotifyProgressRefresh();
        GameManager.Instance.SavePlayerInfo();
        UIManager.Instance.GetUI<GameScenePanel>().ResetGame();

        LevelTaskTipData tipData = new LevelTaskTipData(
            PhaseTaskService.GetLevelDisplayNumber(),
            PhaseTaskService.GetCurrentTaskText(),
            EnterNextLevel,
            false);

        if (tipData.CanOpen)
            UIManager.Instance.OpenUI<LevelTaskTip>(tipData);
        else
            EnterNextLevel();
    }

    private void EnterNextLevel()
    {
        TxGameHooks.TryStartPendingLevelFlow();
        GameManager.Instance.ShowEvaluationGame();
    }

    private void ResetPanel()
    {
        foreach (Transform item in itemRoot)
        {
            Destroy(item.gameObject);
        }
        itemDatas = null;
        itemBase = null;
    }
}
