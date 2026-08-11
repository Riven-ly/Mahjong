using AdjustSdk;
using SolarEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherSdkManager : MonoBehaviour
{
    public static OtherSdkManager Instance;

    public static bool IsInit = false;
    private void Awake()
    {
        Instance = this;
        Init();
    }


    public void Init()
    {
        Debug.Log("Other SDK初始化");

        SolarEngineInit();
    }

    private void AdjustInit()
    {
        string adjust_AppToken = "r46mpxhxlwqo";
        AdjustConfig adjustConfig = new AdjustConfig(adjust_AppToken, AdjustEnvironment.Production);
        // ...
        Adjust.InitSdk(adjustConfig);
    }

    private void SolarEngineInit()
    {
        string AppKey = "b7c8e6dce069ef47";
        SolarEngine.Analytics.preInitSeSdk(AppKey);

        SESDKRemoteConfig remoteConfig = new SESDKRemoteConfig();

        #region 远程参数默认兜底配置
        // att_code：归因标识字符串，用于区分渠道/归因类型，后台下发不同渠道标识，空字符串为默认
        SESDKRemoteConfig.Item item_att_code = remoteConfig.stringItem("att_code", "F");
        // mau_inter：手动插屏强制展示开关；true=忽略插屏冷却间隔，100%展示手动插屏；false=遵循冷却限制
        SESDKRemoteConfig.Item item_mau_inter = remoteConfig.boolItem("mau_inter", false);
        // inter_show_space：全局插屏公共冷却时长(秒)；所有插屏（手动/自动）共用的冷却间隔
        SESDKRemoteConfig.Item item_inter_show_space = remoteConfig.intItem("inter_show_space", 45);
        // auto_inter_space：自动插屏触发间隔(秒)；控制自动弹出类插屏广告最低触发间隔
        SESDKRemoteConfig.Item item_auto_inter_space = remoteConfig.intItem("auto_inter_space", 60);
        // inter_enable：插屏广告总开关；true=允许展示插屏广告；false=全局关闭所有插屏
        SESDKRemoteConfig.Item item_inter_enable = remoteConfig.boolItem("inter_enable", true);
        // start_inter_sec：启动首次插屏延迟(秒)；游戏启动后，最少等待多少秒才允许弹出第一个插屏
        SESDKRemoteConfig.Item item_start_inter_sec = remoteConfig.intItem("start_inter_sec", 120);
        #endregion

        // 组装默认配置数组，当远程参数拉取失败/无网络时，自动使用下面兜底数值
        SESDKRemoteConfig.Item[] defaultConfigArray = new SESDKRemoteConfig.Item[]
        {
                item_att_code,
                item_mau_inter,
                item_inter_show_space,
                item_auto_inter_space,
                item_inter_enable,
                item_start_inter_sec
        };
        // 向热力引擎RC参数下发模块注册兜底默认配置（重要：必须在 initSeSdk 之前调用）
        remoteConfig.SetRemoteDefaultConfig(defaultConfigArray);

        SEConfig seConfig = new SEConfig();
        seConfig.initCompletedCallback = (e) =>
        {
            IsInit = e == 0;
            Debug.Log("SolarEngineInit :" + e);
            ReadRemoteConfigAfterInit2();
        };

        RCConfig rcConfig = new RCConfig();
        rcConfig.enable = true;
        rcConfig.mergeType = RCMergeType.ByDefault;

        SolarEngine.Analytics.initSeSdk(AppKey, seConfig, rcConfig);
    }

    /// <summary>
    /// 一次性拉取全部远程配置，减少多次异步请求开销
    /// </summary>
    private void ReadRemoteConfigAfterInit2()
    {
        SESDKRemoteConfig remoteConfig = new SESDKRemoteConfig();

        Debug.Log("一次性拉取全部远程配置 ");
        // 一次性获取所有远程下发参数
        remoteConfig.AsyncFetchRemoteConfig((allConfig) =>
        {
            Debug.Log("远程配置回调");
            if (allConfig == null)
                return;

            // inter_enable - 插屏广告总开关；true开启插屏，false全局关闭插屏广告
            if (allConfig.TryGetValue("inter_enable", out var interEnableObj))
            {
                string result = interEnableObj.ToString();
                Debug.Log("远程参数 inter_enable = " + result);
                if (bool.TryParse(result, out bool interEnable))
                {
                    ApplovinMaxInterstitialOperator.inter_enable = interEnable;
                }
            }

            // mau_inter - 手动插屏是否强制展示；true忽略冷却间隔，100%展示手动插屏
            if (allConfig.TryGetValue("mau_inter", out var mauInterObj))
            {
                string result = mauInterObj.ToString();
                Debug.Log("远程参数 mau_inter = " + result);
                if (bool.TryParse(result, out bool mauInter))
                {
                    ApplovinMaxInterstitialOperator.mau_inter = mauInter;
                }
            }

            // inter_show_space - 全局插屏公共冷却间隔（秒），所有插屏共用冷却
            if (allConfig.TryGetValue("inter_show_space", out var interSpaceObj))
            {
                string result = interSpaceObj.ToString();
                Debug.Log("远程参数 inter_show_space = " + result);
                if (int.TryParse(result, out int interSpace))
                {
                    ApplovinMaxInterstitialOperator.ad_mau_inter_time = interSpace;
                }
            }

            // auto_inter_space - 自动插屏触发间隔（秒）
            if (allConfig.TryGetValue("auto_inter_space", out var autoInterSpaceObj))
            {
                string result = autoInterSpaceObj.ToString();
                Debug.Log("远程参数 auto_inter_space = " + result);
                if (int.TryParse(result, out int autoInterSpace))
                {
                    ApplovinMaxInterstitialOperator.insertTime = autoInterSpace;
                }
            }

            // start_inter_sec - 启动后首次插屏延迟秒数
            if (allConfig.TryGetValue("start_inter_sec", out var startInterSecObj))
            {
                string result = startInterSecObj.ToString();
                Debug.Log("远程参数 start_inter_sec = " + result);
                if (int.TryParse(result, out int startInterSec))
                {
                    ApplovinMaxInterstitialOperator.startInsertTime = startInterSec;
                    if (AdManager.Instance != null)
                    {
                        AdManager.Instance.applovinMaxInterstitialOperator.insertTimer = startInterSec;
                    }
                }
            }

            // att_code - 归因标识，渠道区分字符串
            if (allConfig.TryGetValue("att_code", out var attCodeObj))
            {
                string result = attCodeObj.ToString();
                Debug.Log("远程参数 att_code = " + result);
                if (!string.IsNullOrEmpty(result) && result.Contains("T"))
                {
                    GameManager.appATTtype = 0;
                }
                else
                {
                    GameManager.appATTtype = 1;
                }
                GameManager.Instance.UpdateAppATT();
            }
        });
    }

    public void CustomEvent(string eName,string _key, object _value)
    {
        if (!IsInit)
            return;

        Dictionary<string, object> customAttributes = new Dictionary<string, object>();
        customAttributes.Add(_key, _value);
        SolarEngine.Analytics.track(eName, customAttributes);
    }
    public void CustomEvent(string eName, string _key, object _value, string _key2, object _value2)
    {
        if (!IsInit)
            return;

        Dictionary<string, object> customAttributes = new Dictionary<string, object>();
        customAttributes.Add(_key, _value);
        customAttributes.Add(_key2, _value2);
        SolarEngine.Analytics.track(eName, customAttributes);
    }
    public void CustomEvent(string eName, string _key, object _value, string _key2, object _value2, string _key3, object _value3)
    {
        if (!IsInit)
            return;

        Dictionary<string, object> customAttributes = new Dictionary<string, object>();
        customAttributes.Add(_key, _value);
        customAttributes.Add(_key2, _value2);
        customAttributes.Add(_key3, _value3);
        SolarEngine.Analytics.track(eName, customAttributes);
    }
}
