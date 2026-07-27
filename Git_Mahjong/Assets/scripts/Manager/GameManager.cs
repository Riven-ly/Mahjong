using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


public enum GameType
{
    LobbyScene,
    MainGame,
    DailyGame,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static int appATTtype = 0;
    public static bool LoadABAsyncOK = false;

    public GameType gameType;
    public PlayerInfo playerInfo;
    public List<Sprite> specialDiamonds;
    public List<Sprite> specialRewardsDuis;
    public GameObject ItemPrefab;

    public Action EvaluationGameCallback;

    public int addDebug_hours = 0;
    private void Awake()
    {
        Instance = this;

        Application.targetFrameRate = 60;
        Input.multiTouchEnabled = false;
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        playerInfo = GetPlayerInfo();

        UIManager.Instance.OpenUI<LobbyScenePanel>(1);
    }

    public string GetTimeString(float _Seconds)
    {
        float safeSeconds = Mathf.Max(_Seconds, 0);
        int m = (int)(safeSeconds / 60);
        int s = (int)(safeSeconds % 60);
        return $"{m:D2}:{s:D2}";
    }

    public void UpdateAppATTToDiamond(Image image)
    {
        image.sprite = specialDiamonds[appATTtype];
        image.SetNativeSize();
    }
    public void UpdateAppATTToDiamondDui(Image image)
    {
        image.sprite = specialRewardsDuis[appATTtype];
        image.SetNativeSize();
    }

    public void UpdateAppATT()
    {
        int sAtt = PlayerPrefs.GetInt("UpdateAppATT", 0);
        if (sAtt == 1)
        {
            appATTtype = 1;
        }

        //LoadOtherPrefab();
        LoadABAsyncOK = true;
        if (appATTtype == 1)
        {
            if (sAtt != 1)
            {
                PlayerPrefs.SetInt("UpdateAppATT", appATTtype);
            }
            LoadAssetBundleAsync();
        }
    }

    private async void LoadAssetBundleAsync()
    {
        Debug.Log("开始异步加载");
        LoadABAsyncOK = false;
        // 异步加载 AB 包
        AssetBundle ab = await ABbuildManager.LoadABAsync();
        if (ab == null)
        {
            Debug.LogError("AB 包加载失败");
            LoadABAsyncOK = true;
            return;
        }

        if (appATTtype == 1)
        {
            AssetBundleRequest request2 = ab.LoadAssetAsync<GameObject>("assets/abres/abresmanager/abresmanager.prefab");
            await request2;

            GameObject prefab = request2.asset as GameObject;
            if (prefab != null)
            {
                Instantiate(prefab);
                Debug.Log("加载并生成：ABResManager");
            }
        }
        Debug.Log("异步加载完成");
        LoadABAsyncOK = true;
    }

    public static bool CheckSimpleEmail(string email)
    {
        // 空值/空字符串直接返回false
        if (string.IsNullOrEmpty(email)) return false;

        // 1. 必须包含且仅包含一个@
        int atIndex = email.IndexOf('@');
        if (atIndex == -1 || atIndex != email.LastIndexOf('@')) return false;

        // 2. @前后必须有内容
        string front = email.Substring(0, atIndex);
        string back = email.Substring(atIndex + 1);
        if (string.IsNullOrEmpty(front) || string.IsNullOrEmpty(back)) return false;

        // 3. @后面必须包含至少一个.（且.不能在开头/结尾）
        int dotIndex = back.IndexOf('.');
        if (dotIndex == -1 || dotIndex == 0 || dotIndex == back.Length - 1) return false;

        // 满足以上条件则认为格式有效
        return true;
    }

    public void TryEvaluationGame()
    {
        int evaluationGameStar = PlayerPrefs.GetInt("EvaluationGameStar", 0);
        // 已经五星，不再弹出
        if (evaluationGameStar == 5)
        {
            return;
        }

        // 存储键定义
        string keyShowCount = "EvalGame_DailyShowCount";
        string keyLastDate = "EvalGame_LastShowDate";

        // 获取今日时间戳（只保留年月日，重置每日计数）
        string todayDate = GetNowTime().ToString("yyyyMMdd");
        string saveDate = PlayerPrefs.GetString(keyLastDate, "");

        int todayShowCount = 0;
        if (saveDate == todayDate)
        {
            // 同一天，读取今日已弹出次数
            todayShowCount = PlayerPrefs.GetInt(keyShowCount, 0);
        }
        else
        {
            // 跨天，重置次数并更新日期
            PlayerPrefs.SetString(keyLastDate, todayDate);
            PlayerPrefs.SetInt(keyShowCount, 0);
            todayShowCount = 0;
        }

        // 每日最多弹出2次
        if (todayShowCount >= 2)
            return;

        // 打开评分面板
        //UIManager.Instance.OpenUI<EvaluationGamePanel>();

        // 次数+1并保存
        todayShowCount++;
        PlayerPrefs.SetInt(keyShowCount, todayShowCount);
    }

    /// <summary>
    /// 创建道具
    /// </summary>
    /// <param name="itemDatas"></param>
    /// <param name="_root"></param>
    /// <returns>道具对象ItemBase</returns>
    public List<ItemBase> CreatItems(List<ItemData> itemDatas, Transform _root)
    {
        if (itemDatas == null || _root == null) return null;
        List<ItemBase> itemObjs = new List<ItemBase>();
        foreach (var item in itemDatas)
        {
            var obj = Instantiate(ItemPrefab, _root);
            obj.transform.localPosition = Vector3.zero;
            obj.GetComponent<ItemBase>().Init(item);
            itemObjs.Add(obj.GetComponent<ItemBase>());
        }
        return itemObjs;
    }

    /// <summary>
    /// 获取当前时间
    /// </summary>
    /// <returns></returns>
    public DateTime GetNowTime()
    {
        DateTime newNow = DateTime.UtcNow;
        //if (TimeManager.Instance != null)
        //{
        //    newNow = TimeManager.Instance.Now;
        //    //Debug.Log($" 当前时间：{newNow:yyyy-MM-dd HH:mm:ss}");
        //}
        newNow = newNow.AddHours(addDebug_hours);
        return newNow;
    }
    public static DateTime TimeStampToDateTime(long timestamp)
    {
        // Unix时间戳的基准时间（UTC）
        DateTime utcEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return utcEpoch.AddSeconds(timestamp);
    }
    /// <summary>
    /// 获取秒级时间戳
    /// </summary>
    public static long DateTimeToTimeStamp(DateTime nowUtc)
    {
        // 基准时间：1970-01-01 00:00:00 UTC
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan timeSpan = nowUtc - epoch;
        return (long)timeSpan.TotalSeconds;
    }
    /// <summary>
    /// 获取微秒级时间戳
    /// </summary>
    public static long GetMicrosecondTimestamp(DateTime nowUtc)
    {
        // 基准时间：1970-01-01 00:00:00 UTC
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan timeSpan = nowUtc - epoch;
        long microseconds = (long)(timeSpan.TotalMilliseconds * 1000);
        return microseconds;
    }

    public static int GetWeightIndex(List<ItemData> datas, List<int> weights)
    {
        var newWeights = weights.ToList();
        //int targetLv = GameManager.Instance.playerInfo.level;
        //for (int i = 0; i < datas.Count; i++)
        //{
        //    switch (datas[i].itemType)
        //    {
        //        case ItemType.Hint:
        //            if (targetLv < 2)
        //            {
        //                newWeights[i] = 0;
        //            }
        //            break;
        //        case ItemType.RowClear:
        //            if (targetLv < 5)
        //            {
        //                newWeights[i] = 0;
        //            }
        //            break;
        //        case ItemType.IceBlock:
        //            if (targetLv < 10)
        //            {
        //                newWeights[i] = 0;
        //            }
        //            break;

        //    }
        //}


        int targetIndex = 0;
        int totalWeight = 0;
        foreach (int weight in newWeights)
        {
            totalWeight += weight;
        }

        // 生成 0 ~ 总权重 之间的随机数
        int randomValue = UnityEngine.Random.Range(0, totalWeight);

        // 遍历权重，判断随机数落在哪个区间
        int currentWeight = 0;
        for (int i = 0; i < newWeights.Count; i++)
        {
            if (newWeights[i] <= 0)
            {
                continue;
            }
            currentWeight += newWeights[i];
            // 随机数小于当前累计权重，说明抽到了这个道具
            if (randomValue < currentWeight)
            {
                // 返回随机到的道具
                targetIndex = i;
                break;
            }
        }

        return targetIndex;
    }


    /// <summary>
    /// 1个参数：名称 + 值
    /// </summary>
    public static string CreateJson(string key1, string value1)
    {
        Dictionary<string, string> data = new Dictionary<string, string>
        {
            { key1, value1 }
        };

        string strTem = JsonConvert.SerializeObject(data);
        Debug.Log(strTem);
        return strTem;
    }

    /// <summary>
    /// 2个参数：名称1 + 值1，名称2 + 值2
    /// </summary>
    public static string CreateJson(string key1, string value1, string key2, string value2)
    {
        Dictionary<string, string> data = new Dictionary<string, string>
        {
            { key1, value1 },
            { key2, value2 }
        };
        string strTem = JsonConvert.SerializeObject(data);
        Debug.Log(strTem);
        return strTem;
    }

    /// <summary>
    /// 3个参数：名称1 + 值1，名称2 + 值2，名称3 + 值3
    /// </summary>
    public static string CreateJson(string key1, string value1, string key2, string value2, string key3, string value3)
    {
        Dictionary<string, string> data = new Dictionary<string, string>
        {
            { key1, value1 },
            { key2, value2 },
            { key3, value3 }
        };
        string strTem = JsonConvert.SerializeObject(data);
        Debug.Log(strTem);
        return strTem;
    }

    public PlayerInfo GetPlayerInfo()
    {
        string jsonStr = PlayerPrefs.GetString("PlayerInfo", "");

        if (string.IsNullOrEmpty(jsonStr))
        {
            PlayerInfo temp = new PlayerInfo();
            return temp;
        }
        return JsonConvert.DeserializeObject<PlayerInfo>(jsonStr);
    }

    public void SavePlayerInfo()
    {
        string jsonStr = JsonConvert.SerializeObject(playerInfo, Formatting.Indented);

        Debug.Log("Player :\n" + jsonStr);
        PlayerPrefs.SetString("PlayerInfo", jsonStr);
        PlayerPrefs.Save();
    }
}
public static class AssetBundleRequestExtensions
{
    public static Awaiter GetAwaiter(this AssetBundleRequest request)
    {
        return new Awaiter(request);
    }

    public struct Awaiter : INotifyCompletion
    {
        private readonly AssetBundleRequest _request;

        public Awaiter(AssetBundleRequest request)
        {
            _request = request;
        }

        public bool IsCompleted => _request.isDone;

        public void OnCompleted(Action continuation)
        {
            _request.completed += _ => continuation();
        }

        public void GetResult() { }
    }
}