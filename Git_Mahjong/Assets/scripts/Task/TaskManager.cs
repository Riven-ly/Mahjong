using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 任务分类。
/// </summary>
public enum TaskCategory
{
    Daily,
    Mainline,
}

/// <summary>
/// 任务进度统计类型。
/// </summary>
public enum TaskProgressType
{
    Login,
    MahjongGameWon,
    LevelProgress,
    PlayAds,
}

/// <summary>
/// 任务配置与运行状态数据。
/// </summary>
[Serializable]
public class TaskData
{
    public string id; // 任务唯一标识
    public string description; // 任务描述
    public TaskCategory category; // 任务分类
    public TaskProgressType progressType; // 进度统计类型
    public int targetProgress; // 目标进度
    public int currentProgress; // 当前进度
    public int goldReward; // 金币奖励
    public bool isClaimed; // 是否已领取
}

/// <summary>
/// 任务持久化数据。
/// </summary>
[Serializable]
public class TaskSaveData
{
    public string dailyDate; // 每日任务所属日期
    public int loginDays; // 累计登录天数
    public List<TaskData> dailyTasks; // 每日任务列表
    public List<TaskData> mainlineTasks; // 主线任务列表

    public int dailyEstimatedReward;//每日任务预估奖励
    public int dailyActualReward; //实际领取奖励
}

/// <summary>
/// 全局任务管理器，负责任务进度、领奖与本地存档。
/// </summary>
public class TaskManager : MonoBehaviour, IEventListener
{
    private const string SaveKey = "TaskManagerData";

    public static TaskManager Instance { get; private set; }
    public Action TasksChanged;

    public TaskSaveData saveData;

    /// <summary>
    /// 获取每日任务列表。
    /// </summary>
    public List<TaskData> DailyTasks => saveData.dailyTasks;

    /// <summary>
    /// 获取主线任务列表。
    /// </summary>
    public List<TaskData> MainlineTasks
    {
        get
        {
            RefreshLevelProgress();
            return saveData.mainlineTasks;
        }
    }

    /// <summary>
    /// 判断当前是否存在已完成且未领取的任务。
    /// </summary>
    public bool HasClaimableTasks()
    {
        if (saveData == null)
        {
            return false;
        }

        RefreshDailyTasks();
        return HasClaimableTask(saveData.dailyTasks) || HasClaimableTask(MainlineTasks);
    }

    /// <summary>
    /// 判断指定任务列表中是否存在可领取任务。
    /// </summary>
    private static bool HasClaimableTask(List<TaskData> tasks)
    {
        foreach (TaskData task in tasks)
        {
            if (!task.isClaimed && task.currentProgress >= task.targetProgress)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 初始化单例引用。
    /// </summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 加载任务数据并记录当日登录。
    /// </summary>
    private void Start()
    {
        LoadTasks();
        RefreshDailyTasks();
        AddProgress(TaskProgressType.Login);
        EventManager.Instance.RegisterListener(GameEvent.MahjongGameWon, this);
        EventManager.Instance.RegisterListener(GameEvent.PlayAds, this);
    }

    /// <summary>
    /// 销毁时注销任务相关全局事件。
    /// </summary>
    private void OnDestroy()
    {
        EventManager.Instance.UnregisterListener(GameEvent.MahjongGameWon, this);
        EventManager.Instance.UnregisterListener(GameEvent.PlayAds, this);
    }

    /// <summary>
    /// 处理任务统计事件。
    /// </summary>
    public void OnEventTriggered(GameEvent eventType, object data = null)
    {
        switch (eventType)
        {
            case GameEvent.MahjongGameWon:
                AddProgress(TaskProgressType.MahjongGameWon);
                break;
            case GameEvent.PlayAds:
                AddProgress(TaskProgressType.PlayAds);
                break;
        }
    }

    /// <summary>
    /// 领取指定任务的奖励。
    /// </summary>
    public void ClaimReward(TaskData task)
    {
        if (task.isClaimed || task.currentProgress < task.targetProgress)
        {
            return;
        }

        task.isClaimed = true;
        SaveTasks();
        UIManager.Instance.OpenUI<GeneralRewardsPanel>(task, () =>
        {
            TasksChanged?.Invoke();
        });
    }

    public void LiLuDailyEstimatedReward(int _ActualReward,int _EstimatedReward)
    {
        saveData.dailyActualReward += _ActualReward;
        saveData.dailyEstimatedReward += _EstimatedReward;
        SaveTasks();
    }

    /// <summary>
    /// 按统计类型增加对应任务进度。
    /// </summary>
    private void AddProgress(TaskProgressType progressType)
    {
        bool changed = AddProgress(saveData.dailyTasks, progressType);
        changed |= AddProgress(saveData.mainlineTasks, progressType);
        if (!changed)
        {
            return;
        }

        SaveTasks();
        TasksChanged?.Invoke();
    }

    /// <summary>
    /// 为一组任务增加匹配的未完成进度。
    /// </summary>
    private bool AddProgress(List<TaskData> tasks, TaskProgressType progressType)
    {
        bool changed = false;
        foreach (TaskData task in tasks)
        {
            if (task.progressType != progressType || task.isClaimed || task.currentProgress >= task.targetProgress)
            {
                continue;
            }

            task.currentProgress++;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 使用当前关卡同步关卡进度任务。
    /// </summary>
    private void RefreshLevelProgress()
    {
        bool changed = false;
        int level = GameManager.Instance.playerInfo.level;
        foreach (TaskData task in saveData.mainlineTasks)
        {
            if (task.progressType != TaskProgressType.LevelProgress)
            {
                continue;
            }

            int progress = Mathf.Min(level, task.targetProgress);
            if (task.currentProgress == progress)
            {
                continue;
            }

            task.currentProgress = progress;
            changed = true;
        }

        if (changed)
        {
            SaveTasks();
        }
    }

    /// <summary>
    /// 加载本地任务数据。
    /// </summary>
    private void LoadTasks()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        saveData = string.IsNullOrEmpty(json) ? CreateDefaultData() : JsonConvert.DeserializeObject<TaskSaveData>(json);
        if (saveData == null || saveData.dailyTasks == null || saveData.mainlineTasks == null)
        {
            saveData = CreateDefaultData();
            return;
        }

        MergeMainlineTasks();
    }

    /// <summary>
    /// 合并新版主线任务配置并保留已有任务状态。
    /// </summary>
    private void MergeMainlineTasks()
    {
        List<TaskData> configuredTasks = CreateMainlineTasks();
        foreach (TaskData configuredTask in configuredTasks)
        {
            TaskData savedTask = saveData.mainlineTasks.Find(task => task.id == configuredTask.id);
            if (savedTask != null)
            {
                configuredTask.currentProgress = savedTask.currentProgress;
                configuredTask.isClaimed = savedTask.isClaimed;
            }
        }

        saveData.mainlineTasks = configuredTasks;
        SaveTasks();
    }

    /// <summary>
    /// 处理每日任务
    /// </summary>
    public void RefreshDailyTasks()
    {
        string today = GameManager.Instance.GetNowTime().ToString("yyyyMMdd");
        if (saveData.dailyDate == today)
        {
            return;
        }

        saveData.dailyEstimatedReward = 0;
        saveData.dailyActualReward = 0;

        saveData.dailyDate = today;
        saveData.loginDays++;
        saveData.dailyTasks = CreateDailyTasks();
        AddProgress(TaskProgressType.Login);
    }

    /// <summary>
    /// 创建首次使用时的任务数据。
    /// </summary>
    private TaskSaveData CreateDefaultData()
    {
        return new TaskSaveData
        {
            dailyDate = string.Empty,
            loginDays = 0,
            dailyTasks = CreateDailyTasks(),
            mainlineTasks = CreateMainlineTasks(),
        };
    }

    /// <summary>
    /// 创建每日任务配置。
    /// </summary>
    private List<TaskData> CreateDailyTasks()
    {
        return new List<TaskData>
        {
            new TaskData { id = "daily_login", description = "TaskDailyLogin", category = TaskCategory.Daily, progressType = TaskProgressType.Login, targetProgress = 1, goldReward = 50 },
            new TaskData { id = "daily_win_3", description = "TaskCompleteLevel", category = TaskCategory.Daily, progressType = TaskProgressType.MahjongGameWon, targetProgress = 3, goldReward = 200 },
            new TaskData { id = "daily_ad_3", description = "TaskPlayAds", category = TaskCategory.Daily, progressType = TaskProgressType.PlayAds, targetProgress = 3, goldReward = 150},
        };
    }

    /// <summary>
    /// 创建主线任务配置。
    /// </summary>
    private List<TaskData> CreateMainlineTasks()
    {
        return new List<TaskData>
        {
            new TaskData { id = "mainline_win_5", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 5, goldReward = 100 },
            new TaskData { id = "mainline_win_10", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 10, goldReward = 20 },
            new TaskData { id = "mainline_win_15", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 15, goldReward = 30 },
            new TaskData { id = "mainline_win_20", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 20, goldReward = 40 },
            new TaskData { id = "mainline_win_30", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 30, goldReward = 50 },
            new TaskData { id = "mainline_win_40", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 40, goldReward = 60 },
            new TaskData { id = "mainline_win_50", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 50, goldReward = 70 },
            new TaskData { id = "mainline_win_70", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 70, goldReward = 80 },
            new TaskData { id = "mainline_win_100", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 100, goldReward = 90 },
            new TaskData { id = "mainline_win_150", description = "TaskCompleteLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.MahjongGameWon, targetProgress = 150, goldReward = 33 },
            new TaskData { id = "mainline_level_5", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 5, goldReward = 300 },
            new TaskData { id = "mainline_level_10", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 10, goldReward = 450 },
            new TaskData { id = "mainline_level_15", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 15, goldReward = 600 },
            new TaskData { id = "mainline_level_20", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 20, goldReward = 750 },
            new TaskData { id = "mainline_level_30", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 30, goldReward = 1000 },
            new TaskData { id = "mainline_level_40", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 40, goldReward = 1250 },
            new TaskData { id = "mainline_level_50", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 50, goldReward = 1500 },
            new TaskData { id = "mainline_level_70", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 70, goldReward = 1800 },
            new TaskData { id = "mainline_level_100", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 100, goldReward = 2200 },
            new TaskData { id = "mainline_level_150", description = "TaskReachLevel", category = TaskCategory.Mainline, progressType = TaskProgressType.LevelProgress, targetProgress = 150, goldReward = 3000 },
        };
    }

    /// <summary>
    /// 保存任务状态到本地偏好设置。
    /// </summary>
    private void SaveTasks()
    {
        PlayerPrefs.SetString(SaveKey, JsonConvert.SerializeObject(saveData));
        PlayerPrefs.Save();
    }
}
