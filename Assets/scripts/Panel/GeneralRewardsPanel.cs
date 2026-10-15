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
    public Button collectBtn;
    public Transform collectTrans;
    public Text ad_text;
    public Text c_text;

    [Header("Show Animation")]
    public RectTransform titleRoot;
    public Transform caidaiRoot;
    [Min(0.01f)] public float scaleDuration = 0.35f;
    [Min(0f)] public float stepInterval = 0.25f;
    public Ease scaleEase = Ease.OutBack;
    [Min(1f)] public float adBtnBreathScale = 1.1f;
    [Min(0.01f)] public float adBtnBreathDuration = 0.5f;

    private List<ItemData> itemDatas;
    private List<ItemBase> itemBase;
    private int ad_V;
    private int c_V;
    private TaskData taskData;
    private string page_id = "GeneralRewardsPanel";

    private Sequence showSequence;
    private Tween adBtnBreathTween;
    private Vector3 titleScale;
    private Vector3 itemScale;
    private Vector3 adBtnScale;
    private Vector3 collectScale;
    private bool showScalesCached;
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
                GameManager.Instance.playerInfo.adShownThisLevel = true;
                GameManager.Instance.playerInfo.consecutiveNoAdCount = 0;

                if(UnityEngine.Random.Range(0,101) > 35)
                {
                    AdManager.Instance.OnClickInterstitialAd("");
                }
                else
                {
                    AdManager.Instance.ShowRewardedAd(page_id, null, null);
                }
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
        ResetShowContent();
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
        AudioManager.Instance.PlaySceneSingleMusic("luckybox");
        OtherSdkManager.Instance.CustomEvent("rewards_show", "show", "");

        collectBtn.transform.parent.gameObject.SetActive(GameManager.Instance.playerInfo.level > 2);

        PlayShowAnimation();
    }

    /// <summary>
    /// 面板内容依次弹出：标题 → 奖励 → 激励按钮 → 普通按钮。
    /// </summary>
    private void PlayShowAnimation()
    {
        CacheShowScales();
        showSequence?.Kill(false);

        HideShowTarget(titleRoot, titleScale);
        HideShowTarget(itemRoot as RectTransform, itemScale);
        HideShowTarget(rewardAdButton != null ? rewardAdButton.transform as RectTransform : null, adBtnScale);
        HideShowTarget(collectTrans as RectTransform, collectScale);

        showSequence = DOTween.Sequence().SetTarget(this);

        if (caidaiRoot != null)
        {
            caidaiRoot.gameObject.SetActive(false);
            caidaiRoot.gameObject.SetActive(true);
        }
        AudioManager.Instance.PlayMahjongCellMusic2("caidai");

        AppendShow(titleRoot, titleScale);
        AppendShow(itemRoot as RectTransform, itemScale);
        AppendShow(rewardAdButton != null ? rewardAdButton.transform as RectTransform : null, adBtnScale);
        if (GameManager.Instance.playerInfo.level > 2)
        {
            AppendShow(collectTrans as RectTransform, collectScale);
        }

        showSequence.OnComplete(() =>
        {
            showSequence = null;
            StartAdButtonBreath();
        });
    }

    /// <summary>
    /// 激励按钮呼吸动画：在原尺寸与放大尺寸之间往复缩放。
    /// </summary>
    private void StartAdButtonBreath()
    {
        StopAdButtonBreath();
        if (rewardAdButton == null)
        {
            return;
        }

        Transform target = rewardAdButton.transform;
        target.localScale = adBtnScale;
        adBtnBreathTween = target.DOScale(adBtnScale * adBtnBreathScale, adBtnBreathDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(this);
    }

    private void StopAdButtonBreath()
    {
        adBtnBreathTween?.Kill(false);
        adBtnBreathTween = null;
        if (rewardAdButton != null)
        {
            rewardAdButton.transform.localScale = adBtnScale;
        }
    }

    private void AppendShow(RectTransform target, Vector3 targetScale)
    {
        if (target == null)
        {
            return;
        }

        showSequence.AppendCallback(() =>
        {
            target.gameObject.SetActive(true);
            target.localScale = Vector3.zero;
        });
        showSequence.Append(target.DOScale(targetScale, scaleDuration).SetEase(scaleEase));
        if (stepInterval > 0f)
        {
            showSequence.AppendInterval(stepInterval);
        }
    }

    private void HideShowTarget(RectTransform target, Vector3 targetScale)
    {
        if (target == null)
        {
            return;
        }

        target.localScale = targetScale;
        target.gameObject.SetActive(false);
    }

    private void CacheShowScales()
    {
        if (showScalesCached)
        {
            return;
        }

        showScalesCached = true;
        if (titleRoot != null)
        {
            titleScale = titleRoot.localScale;
        }
        if (itemRoot is RectTransform itemRect)
        {
            itemScale = itemRect.localScale;
        }
        if (rewardAdButton != null)
        {
            adBtnScale = rewardAdButton.transform.localScale;
        }
        if (collectTrans != null)
        {
            collectScale = collectTrans.localScale;
        }
    }

    private void ResetShowContent()
    {
        showSequence?.Kill(false);
        showSequence = null;
        StopAdButtonBreath();
        HideShowTarget(titleRoot, titleScale);
        HideShowTarget(itemRoot as RectTransform, itemScale);
        HideShowTarget(rewardAdButton != null ? rewardAdButton.transform as RectTransform : null, adBtnScale);
        HideShowTarget(collectTrans as RectTransform, collectScale);
        if (caidaiRoot != null)
        {
            caidaiRoot.gameObject.SetActive(false);
        }
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
