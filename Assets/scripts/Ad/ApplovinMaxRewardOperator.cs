using AdjustSdk;
using SolarEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplovinMaxRewardOperator : MonoBehaviour
{
    private string _androidRewardedAdUnitId = "5c6c3571c535ffde";
    //private string _androidRewardedAdUnitId = "7853d0583015ae97"; // 安卓测试ID
    private string _iosRewardedAdUnitId = "";     // iOS测试ID
    int retryAttempt;
    //-------------------------------
    protected string RewardedAdUnitId = "";
    public Action RewardReceivedCallback = null;
    public Action RewardDisplayErrorCallback = null;
    public Action playRewardAdCompleteCallback = null;//关闭时在调奖励
    public static bool isPlayRewardAds = false;

    private bool isAdLoading;
    public virtual void Init()
    {
        isPlayRewardAds = false;
        InitializeRewardedAds();
    }
    public void InitializeRewardedAds()
    {
        RewardedAdUnitId = Application.platform == RuntimePlatform.IPhonePlayer ? _iosRewardedAdUnitId : _androidRewardedAdUnitId;

        // Attach callback
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;

        isAdLoading = false;
        // Load the first rewarded ad
        LoadRewardedAd();
    }

    protected void LoadRewardedAd()
    {
        Debug.Log("激励视频加载 :" + RewardedAdUnitId);

        MaxSdk.LoadRewardedAd(RewardedAdUnitId);
    }

    // 展示激励视频（调用此方法触发广告展示）
    public void ShowRewardedAd()
    {
        if (isAdLoading)
        {
            return;
        }
        isAdLoading = true;

        //防刷
        //if (!PRgameManager.PR_pass)
        //{
        //    ExecutionRewardDisplayErrorCallback();
        //    string str = LanguageManager.Instance.GetText("AdsNotReady");
        //    UIManager.Instance.OpenUI<GeneralTipsPanel>(str);
        //    return;
        //}

        if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
        {
            isPlayRewardAds = true;
            MaxSdk.ShowRewardedAd(RewardedAdUnitId);

        }
        else
        {
            isPlayRewardAds = false;
            Debug.Log("激励视频未加载完成，无法展示");
            ExecutionRewardDisplayErrorCallback();
            string str = LanguageManager.Instance.GetText("AdsNotReady");
            UIManager.Instance.OpenUI<GeneralTipsPanel>(str);
            LoadRewardedAd(); // 重新加载
        }
    }
    private void ExecutionRewardDisplayErrorCallback()
    {
        if (RewardDisplayErrorCallback != null)
        {
            RewardDisplayErrorCallback();
            RewardDisplayErrorCallback = null;
        }
    }
    private void ExecutionRewardReceivedCallback()
    {
        if (RewardReceivedCallback != null)
        {
            RewardReceivedCallback();
            RewardReceivedCallback = null;
        }
    }
    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        isAdLoading = false;
        // Rewarded ad is ready for you to show. MaxSdk.IsRewardedAdReady(adUnitId) now returns 'true'.
        // Reset retry attempt
        retryAttempt = 0;
        Debug.Log("OnRewardedAdLoadedEvent");
    }

    private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdk.ErrorInfo errorInfo)
    {
        // Rewarded ad failed to load
        // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds).
        isAdLoading = false;
        retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, retryAttempt));

        Debug.Log("OnRewardedAdLoadFailedEvent :" + errorInfo.Message);
        Invoke("LoadRewardedAd", (float)retryDelay);
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdk.AdInfo adInfo) 
    {
        Debug.Log("OnRewardedAdDisplayedEvent");
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
    {
        // Rewarded ad failed to display. AppLovin recommends that you load the next ad.

        isPlayRewardAds = false;
        Debug.Log("OnRewardedAdFailedToDisplayEvent");
        ExecutionRewardDisplayErrorCallback();
        LoadRewardedAd();


    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdk.AdInfo adInfo) 
    {
        Debug.Log("OnRewardedAdClickedEvent");
        if (!OtherSdkManager.IsInit)
        {
            return;
        }
        AdClickAttributes AdClickAttributes = new AdClickAttributes();
        AdClickAttributes.ad_platform = adInfo.NetworkName;
        AdClickAttributes.mediation_platform = "max";
        AdClickAttributes.ad_id = adInfo.AdUnitIdentifier;
        AdClickAttributes.ad_type = 1; 
        SolarEngine.Analytics.trackAdClick(AdClickAttributes);
    }

    private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        // Rewarded ad is hidden. Pre-load the next ad
        playRewardAdCompleteCallback?.Invoke();
        playRewardAdCompleteCallback = null;

        Debug.Log("OnRewardedAdHiddenEvent  ");
        isPlayRewardAds = false;
        ExecutionRewardDisplayErrorCallback();
        LoadRewardedAd();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdk.AdInfo adInfo)
    {
        // The rewarded ad displayed and the user should receive the reward.
        //获得奖励时还没走Close
        if (isPlayRewardAds)
        {
            //关闭时在调奖励
            playRewardAdCompleteCallback = () =>
            {
                Debug.Log("OnRewardedAdReceivedRewardEvent   ");
                EventManager.Instance.TriggerEvent(GameEvent.PlayAds);
                ExecutionRewardReceivedCallback();
            };
        }
        else  //获得奖励时已经调用过Close了
        {
            Debug.Log("OnRewardedAdReceivedRewardEvent  ");
            EventManager.Instance.TriggerEvent(GameEvent.PlayAds);
            ExecutionRewardReceivedCallback();
        }

    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnRewardedAdRevenuePaidEvent(adInfo);

    }
    //广告成功展示且产生有效收益
    private void OnRewardedAdRevenuePaidEvent(MaxSdk.AdInfo adInfo)
    {
        // Ad revenue paid. Use this callback to track user revenue.
        if (!OtherSdkManager.IsInit)
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
        impressionAttributes.ad_type = 1;
        impressionAttributes.ad_ecpm = adInfo.Revenue * 1000;
        impressionAttributes.currency_type = "USD";
        impressionAttributes.mediation_platform = "max";//填入您所使用的聚合平台
        impressionAttributes.is_rendered = true;
        SolarEngine.Analytics.trackAdImpression(impressionAttributes);
    }
}
