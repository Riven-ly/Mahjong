using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralRewardsPanel : UIBase
{
    public Transform root;
    public Transform itemRoot;
    public RewardAdButton rewardAdButton;
    public RewardAdButton otherAdBtn;
    public Button collectBtn;
    public Transform collectTrans;
    public Text ad_text;
    public Text c_text;

    private List<ItemData> itemDatas;
    private List<ItemBase> itemBase;
    private int ad_V;
    private int c_V;
    private TaskData taskData;
    private string page_id = "GeneralRewardsPanel";
    private void Awake()
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        float topBlockHeight = Screen.height - Screen.safeArea.yMax;
        rect.offsetMax = new Vector2(0, -topBlockHeight);
    }
    private void Start()
    {
        collectBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            if (GameManager.Instance.playerInfo.consecutiveNoAdCount >= 2)
            {
                AdManager.Instance.OnClickInterstitialAd(page_id);
            }
            CollectClick();
        });
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
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        taskData = data as TaskData;

        if (GameManager.Instance.playerInfo.level <= 2)
        {
            ad_V = UnityEngine.Random.Range(600, 901);
            c_V = (int)(ad_V / 10f);
        }
        else
        {
            ad_V = UnityEngine.Random.Range(2000, 3001);
            c_V = (int)(ad_V / 10f);
        }

        itemDatas = new List<ItemData>();
        itemDatas.Add(new ItemData(ItemType.GoldDui, ad_V));
        itemBase = GameManager.Instance.CreatItems(itemDatas, itemRoot);

        ad_text.text = GameManager.Instance.playerInfo.level > 2 ? $"100%{LanguageManager.Instance.GetText("CLAIM")}" : LanguageManager.Instance.GetText("CLAIM");
        c_text.text = $"{LanguageManager.Instance.GetText("ONLY")} {LanguageManager.Instance.GetText_Encrypt("Special_Diamond__unit")}{MathF.Round(c_V / (float)PlayerInfo.CurrencyUnitScale, 2)}";

        rewardAdButton.Init(AdsCallback, page_id, true);
        if (otherAdBtn != null)
        {
            otherAdBtn.Init(CollectAdCallback, page_id, true);
            bool needForce = GameManager.Instance.playerInfo.level > 2 &&
                             GameManager.Instance.playerInfo.consecutiveNoAdCount >= 2 &&
                             UnityEngine.Random.Range(0, 100) < 35;
            otherAdBtn.gameObject.SetActive(needForce);
        }
        AudioManager.Instance.PlaySceneSingleMusic("luckybox");
        OtherSdkManager.Instance.CustomEvent("rewards_show", "show", "");

        collectBtn.transform.parent.gameObject.SetActive(GameManager.Instance.playerInfo.level > 2);
    }

    /// <summary>
    /// 领取激励广告奖励并增加金币等级经验。
    /// </summary>
    private void AdsCallback()
    {
        GameManager.Instance.playerInfo.adShownThisLevel = true;
        GameManager.Instance.playerInfo.consecutiveNoAdCount = 0;
        OtherSdkManager.Instance.CustomEvent("rewards_click", "type", "claim_two");
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
        TaskManager.Instance.LiLuDailyEstimatedReward(ad_V, ad_V);
        GameManager.Instance.playerInfo.AddGoldExperience(10);
        //动画
        DOTween.Sequence().AppendInterval(awaitTime).AppendCallback(() =>
        {
            playerInfoUI.GoldCanvasRecover();
            playerInfoUI.DiamondCanvasRecover();

            Hide();
        });
    }

    /// <summary>
    /// 领取普通奖励并增加金币等级经验。
    /// </summary>
    private void CollectClick()
    {
        OtherSdkManager.Instance.CustomEvent("rewards_click", "type", "claim_one");
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
        TaskManager.Instance.LiLuDailyEstimatedReward(c_V, ad_V);
        GameManager.Instance.playerInfo.AddGoldExperience(1);
        //动画
        DOTween.Sequence().AppendInterval(awaitTime).AppendCallback(() =>
        {
            playerInfoUI.GoldCanvasRecover();
            playerInfoUI.DiamondCanvasRecover();

            Hide();              
        });
    }

    private void CollectAdCallback()
    {
        UIManager.Instance.HideUIMask();
        GameManager.Instance.playerInfo.adShownThisLevel = true;
        GameManager.Instance.playerInfo.consecutiveNoAdCount = 0;
        CollectClick();
    }

    public override void Hide()
    {
        GameManager.Instance.SavePlayerInfo();
        base.Hide();
   
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
