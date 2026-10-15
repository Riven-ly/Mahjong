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
    private string page_id = "GameWinPanel";

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
    private void OnEnable()
    {
        isOpen = true;
    }
    private void OnDisable()
    {
        isOpen = false;
        if (!GameManager.Instance.playerInfo.adShownThisLevel && GameManager.Instance.playerInfo.level > 2)
        {
            GameManager.Instance.playerInfo.consecutiveNoAdCount++;
        }
        GameManager.Instance.playerInfo.adShownThisLevel = false;
        ResetShowContent();
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
            if (GameManager.Instance.playerInfo.consecutiveNoAdCount >= 2)
            {
                AdManager.Instance.ShowRewardedAd(page_id, null, null);
                GameManager.Instance.playerInfo.adShownThisLevel = true;
                GameManager.Instance.playerInfo.consecutiveNoAdCount = 0;
            }
            CollectClick();
        });
    }

    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        if(GameManager.Instance.playerInfo.level <= 2)
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
        AudioManager.Instance.PlaySceneSingleMusic("gamewin");
        OtherSdkManager.Instance.CustomEvent("level_complete", "level_id", GameManager.Instance.playerInfo.level);
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
        HideShowTarget(collectBtn != null ? collectBtn.transform.parent as RectTransform : null, collectScale);

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
            AppendShow(collectBtn.transform.parent as RectTransform, collectScale);
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

    private void HideShowTarget(RectTransform target, Vector3 targetScale)
    {
        if (target == null)
        {
            return;
        }

        target.localScale = targetScale;
        target.gameObject.SetActive(false);
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
        if (collectBtn != null)
        {
            collectScale = collectBtn.transform.parent.localScale;
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
        HideShowTarget(collectBtn != null ? collectBtn.transform.parent as RectTransform : null, collectScale);
        if (caidaiRoot != null)
        {
            caidaiRoot.gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// 领取激励广告通关奖励并进入下一关。
    /// </summary>
    private void AdsCallback()
    {
        GameManager.Instance.playerInfo.adShownThisLevel = true;
        GameManager.Instance.playerInfo.consecutiveNoAdCount = 0;
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

        UIManager.Instance.OpenUIMask();
       GameManager.Instance.PlayGuoChang(true);
        DOTween.Sequence()
            .AppendInterval(1f)
            .AppendCallback(() =>
            {

                UIManager.Instance.GetUI<GameScenePanel>().ResetGame();
            })
            .AppendInterval(1.8f)
            .AppendCallback(() =>
            {
                UIManager.Instance.HideUIMask();

                LevelTaskTipData tipData = new LevelTaskTipData(
                PhaseTaskService.GetLevelDisplayNumber(),
                PhaseTaskService.GetCurrentTaskText(),
                EnterNextLevel,
                false);
                
                 if (tipData.CanOpen)
                 {
                     UIManager.Instance.OpenUI<LevelTaskTip>(tipData);
                 }
                 else
                 {
                     EnterNextLevel();
                 }
            })
            .AppendInterval(2f)
            .AppendCallback(() =>
            {
                GameManager.Instance.PlayGuoChang(false);
            })
            ;
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
