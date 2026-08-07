using AdjustSdk;
using SolarEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ApplovinMaxInterstitialOperator : MonoBehaviour
{
    private string _androidInterstitialAdUnitId = "9c9dce817740a47c";
    //private string _androidInterstitialAdUnitId = "075d7d1b3dcbd8e1"; // 安卓测试ID
    private string _iosInterstitialAdUnitId = "";     // iOS测试ID
    int retryAttempt;
    //-------------------------------
    string InsertAdUnitId = "";
    public static bool inter_enable = true;// 插屏广告总开关
    public static bool mau_inter = false;// 手动插屏是否强制展示；true忽略冷却间隔，100%展示手动插屏
    public static float startInsertTime = 120f;//第一次打开app插屏时间
    public static float insertTime = 60f;//插屏时间
    public static float ad_mau_inter_time = 45f;//插屏冷却时间
    public float insertTimer = 0;
    private float insertClickCoolingTime_P;
    public float insertClickCoolingTime
    {
        get
        {
            if (mau_inter)
            {
                return 0f;
            }
            else
            {
                return insertClickCoolingTime_P;
            }
        }
        set
        {
            insertClickCoolingTime_P = value;
        }
    }
    private bool isPlayInsertAds = false;

    private bool isInit = false;
    private string admobNetworkName;
    private bool isAdLoading;
	public void Init()
    {
        insertTimer = startInsertTime;
        isPlayInsertAds = false;

        InitializeInterstitialAds();
        isInit = true;
    }
    private void Update()
    {
        if (!isInit) return;
        if (GameLoadingPanel.isOpenStatic) return;
        if (ApplovinMaxRewardOperator.isPlayRewardAds) return;

        if (!isPlayInsertAds)
        {
            insertTimer -= Time.deltaTime;
            if (insertTimer <= 0 && insertClickCoolingTime <= 0)
            {
                insertTimer = insertTime;
                OnClickInterstitialAd(false);

            }
            if (insertClickCoolingTime > 0)
            {
                insertClickCoolingTime -= Time.deltaTime;
            }
        }
    }

    public void InitializeInterstitialAds()
    {
        InsertAdUnitId = Application.platform == RuntimePlatform.IPhonePlayer ? _iosInterstitialAdUnitId : _androidInterstitialAdUnitId;

        // Attach callback
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialAdRevenuePaidEvent;

        isAdLoading = false;
        // Load the first interstitial
        LoadInterstitial();
    }

    /// <summary>
    /// 展示插屏广告
    /// </summary>
    private void ShowInterstitialAd()
    {

        if (MaxSdk.IsInterstitialReady(InsertAdUnitId))
        {
            if (!string.IsNullOrEmpty(admobNetworkName) && admobNetworkName.ToLower().Contains("admob"))
            {
                // 包含admob的逻辑
                isPlayInsertAds = true;
                MaxSdk.ShowInterstitial(InsertAdUnitId);
                //UIManager.Instance.OpenUI<AdBreakPanel>(null, () =>
                //{
                //    MaxSdk.ShowInterstitial(InsertAdUnitId);
                //});
            }
            else
            {
                isPlayInsertAds = true;
                MaxSdk.ShowInterstitial(InsertAdUnitId);
            }
        }
        else
        {
            isPlayInsertAds = false;
            Debug.Log("插屏广告未加载完成，无法展示");
            string str = LanguageManager.Instance.GetText("AdsNotReady");
            UIManager.Instance.OpenUI<GeneralTipsPanel>(str);
            // 展示失败时重新加载
            LoadInterstitial();
        }
    }

    public void OnClickInterstitialAd(bool isClick = true)
    {
        if (!inter_enable) return;
        if (isPlayInsertAds) return;
        if (insertClickCoolingTime > 0) return;

        insertClickCoolingTime = ad_mau_inter_time;

        //防刷
        //if (!PRgameManager.PR_pass)
        //{
        //    return;
        //}
        //没评分之前
        string str = PlayerPrefs.GetString("EvaluationGame", "");
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        ShowInterstitialAd();
    }
    private void LoadInterstitial()
    {
        if (isAdLoading)
        {
            return;
        }
        isAdLoading = true;
        Debug.Log("开始加载插屏广告");

        MaxSdk.LoadInterstitial(InsertAdUnitId);


    }

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        // Interstitial ad is ready for you to show. MaxSdk.IsInterstitialReady(adUnitId) now returns 'true'

        // Reset retry attempt
        isAdLoading = false;
        retryAttempt = 0;
        admobNetworkName = adInfo.NetworkName;
        Debug.Log("插屏广告加载成功 ");
    }

    private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdk.ErrorInfo errorInfo)
    {
        // Interstitial ad failed to load
        // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds)
        isAdLoading = false;
        retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, retryAttempt));
        Debug.Log($"插屏广告加载失败 :" + errorInfo.Message);
        Invoke("LoadInterstitial", (float)retryDelay);
    }

    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        Debug.Log("插屏广告展示成功");
    }

    private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
    {
        // Interstitial ad failed to display. AppLovin recommends that you load the next ad.
        Debug.LogError($"插屏广告展示失败 ");
        isPlayInsertAds = false;
        LoadInterstitial();
    }

    private void OnInterstitialClickedEvent(string adUnitId, MaxSdk.AdInfo adInfo) 
    {
        Debug.Log("插屏广告被点击");
        if (!OtherSdkManager.IsInit)
        {
            return;
        }
        AdClickAttributes AdClickAttributes = new AdClickAttributes();
        AdClickAttributes.ad_platform = adInfo.NetworkName;
        AdClickAttributes.mediation_platform = "max"; 
        AdClickAttributes.ad_id = adInfo.AdUnitIdentifier;
        AdClickAttributes.ad_type = 3;
        SolarEngine.Analytics.trackAdClick(AdClickAttributes);
        Debug.Log("上报广告被点击");
    }

    private void OnInterstitialHiddenEvent(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        // Interstitial ad is hidden. Pre-load the next ad.
        EventManager.Instance.TriggerEvent(GameEvent.PlayAds);
        Debug.Log("插屏广告已关闭");
        isPlayInsertAds = false;
        LoadInterstitial();
    }

    private void OnInterstitialAdRevenuePaidEvent(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnInterstitialAdRevenuePaidEvent(adInfo);
    }

    //广告成功展示且产生有效收益
    private void OnInterstitialAdRevenuePaidEvent(MaxSdk.AdInfo adInfo)
    {
        // Ad revenue paid. Use this callback to track user revenue.
        if(!OtherSdkManager.IsInit)
        {
            return;
        }
        //----------------Adjust------------------------------
        var adRevenue = new AdjustAdRevenue("applovin_max_sdk");
        adRevenue.SetRevenue(adInfo.Revenue, "USD");
        adRevenue.AdRevenueNetwork = adInfo.NetworkName;
        adRevenue.AdRevenueUnit = adInfo.AdUnitIdentifier;
        adRevenue.AdRevenuePlacement = adInfo.Placement;
        Adjust.TrackAdRevenue(adRevenue);
        //----------------热力---------------------------
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();
        impressionAttributes.ad_platform = adInfo.NetworkName;
        impressionAttributes.ad_id = adInfo.AdUnitIdentifier;
        impressionAttributes.ad_type = 3;
        impressionAttributes.ad_ecpm = adInfo.Revenue * 1000;
        impressionAttributes.currency_type = "USD";
        impressionAttributes.mediation_platform = "max";//填入您所使用的聚合平台
        impressionAttributes.is_rendered = true;
        SolarEngine.Analytics.trackAdImpression(impressionAttributes);
    }
}
