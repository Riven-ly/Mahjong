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
        string firstTxStr = PlayerPrefs.GetString("GuidePanel_firstTx");
        if (string.IsNullOrEmpty(firstTxStr))
        {
            UIManager.Instance.OpenUI<GuidePanel_firstTx>();
        }
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
    }
    /// <summary>
    /// 领取激励广告通关奖励并进入下一关。
    /// </summary>
    private void AdsCallback()
    {
        PlayerInfoUI playerInfoUI = UIManager.Instance.GetUI<PlayerInfoUI>();
        UIManager.Instance.OpenUIMask();
        float awaitTime = 0.1f;
        foreach (var item in itemBase)
        {
            if (item.itemType == ItemType.Gold || item.itemType == ItemType.GoldDui)
            {
                awaitTime = 2f;
                playerInfoUI.GoldCanvasTop();
            }
            else if (item.itemType == ItemType.Diamond || item.itemType == ItemType.DiamondDui)
            {
                awaitTime = 2f;
                playerInfoUI.DiamondCanvasTop();
            }
            item.GetItemReward();
            item.PlayItemAnim();
        }
        GameManager.Instance.playerInfo.AddGoldExperience(10);
        //动画
        DOTween.Sequence().AppendInterval(awaitTime).AppendCallback(() =>
        {
            playerInfoUI.GoldCanvasRecover();
            playerInfoUI.DiamondCanvasRecover();
            OnNextLevelButtonClick();
        });
    }

    /// <summary>
    /// 领取普通通关奖励并进入下一关。
    /// </summary>
    private void CollectClick()
    {
        PlayerInfoUI playerInfoUI = UIManager.Instance.GetUI<PlayerInfoUI>();
        UIManager.Instance.OpenUIMask();
        float awaitTime = 0.1f;
        foreach (var item in itemBase)
        {
            if (item.itemType == ItemType.Gold || item.itemType == ItemType.GoldDui)
            {
                awaitTime = 2f;
                playerInfoUI.GoldCanvasTop();
            }
            else if (item.itemType == ItemType.Diamond || item.itemType == ItemType.DiamondDui)
            {
                awaitTime = 2f;
                playerInfoUI.DiamondCanvasTop();
            }
            item.count = c_V;
            item.GetItemReward();
            item.PlayItemAnim();
        }
        GameManager.Instance.playerInfo.AddGoldExperience(1);
        //动画
        DOTween.Sequence().AppendInterval(awaitTime).AppendCallback(() =>
        {
            playerInfoUI.GoldCanvasRecover();
            playerInfoUI.DiamondCanvasRecover();
            OnNextLevelButtonClick();
        });
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
        callback = () =>
        {
            GameManager.Instance.SavePlayerInfo();
            UIManager.Instance.GetUI<GameScenePanel>().ResetGame();
        };
        Hide();
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
