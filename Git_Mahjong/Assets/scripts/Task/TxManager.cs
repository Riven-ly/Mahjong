using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 金额任务的阶段类型。
/// </summary>
public enum TxTaskStage
{
    Amount,
    Win,
    Login,
    Completed,
}

/// <summary>
/// 单个金额档位的任务数据。
/// </summary>
[Serializable]
public class TxTaskData
{
    public int amount; // 目标金额
    public int winTarget; // 通关目标
    public int loginTarget; // 签到目标
    public bool amountReached; // 余额阶段是否已完成
    public int winProgress; // 当前通关进度
}

/// <summary>
/// 金额任务的本地存档数据。
/// </summary>
[Serializable]
public class TxSaveData
{
    public List<TxTaskData> tasks; // 金额任务列表
}

/// <summary>
/// 金额任务管理器，负责独立记录各金额档位的通关阶段进度。
/// </summary>
public class TxManager : MonoBehaviour, IEventListener
{
    private const string SaveKey = "TxManagerData";

    public static TxManager Instance { get; private set; }
    public Action TasksChanged;

    private TxSaveData saveData;

    /// <summary>
    /// 获取金额任务列表。
    /// </summary>
    public IReadOnlyList<TxTaskData> Tasks => saveData.tasks;

    /// <summary>
    /// 获取当前选中的金额任务索引。
    /// </summary>
    public int SelectedTaskIndex => selectedTaskIndex;
    private int selectedTaskIndex;
    /// <summary>
    /// 初始化单例引用。
    /// </summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 加载任务存档并监听通关事件。
    /// </summary>
    private void Start()
    {
        LoadTasks();
        RefreshAmountStages();
        EventManager.Instance.RegisterListener(GameEvent.MahjongGameWon, this);
        TaskManager.Instance.TasksChanged += NotifyTasksChanged;
    }

    /// <summary>
    /// 注销通关事件监听。
    /// </summary>
    private void OnDestroy()
    {
        EventManager.Instance.UnregisterListener(GameEvent.MahjongGameWon, this);
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.TasksChanged -= NotifyTasksChanged;
        }
    }

    /// <summary>
    /// 转发签到天数变化后的任务刷新通知。
    /// </summary>
    private void NotifyTasksChanged()
    {
        RefreshAmountStages();
        TasksChanged?.Invoke();
    }

    /// <summary>
    /// 将首次达到余额条件的金额任务标记为已完成。
    /// </summary>
    public void RefreshAmountStages()
    {
        bool changed = false;
        foreach (TxTaskData task in saveData.tasks)
        {
            if (task.amountReached || GameManager.Instance.playerInfo.Gold < task.amount)
            {
                continue;
            }

            task.amountReached = true;
            changed = true;
        }

        if (changed)
        {
            SaveTasks();
        }
    }

    /// <summary>
    /// 接收通关事件并推进已达到余额阶段的金额任务。
    /// </summary>
    public void OnEventTriggered(GameEvent eventType, object data = null)
    {
        if (eventType != GameEvent.MahjongGameWon)
        {
            return;
        }

        RefreshAmountStages();

        bool changed = false;
        foreach (TxTaskData task in saveData.tasks)
        {
            if (GetTaskStage(task) != TxTaskStage.Win || task.winProgress >= task.winTarget)
            {
                continue;
            }

            task.winProgress++;
            changed = true;
        }

        if (changed)
        {
            SaveTasks();
            TasksChanged?.Invoke();
        }
    }

    /// <summary>
    /// 选择指定的金额档位。
    /// </summary>
    public void SelectTask(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= saveData.tasks.Count || selectedTaskIndex == taskIndex)
        {
            return;
        }

        selectedTaskIndex = taskIndex;
        SaveTasks();
        TasksChanged?.Invoke();
    }

    /// <summary>
    /// 获取指定任务当前所处的阶段。
    /// </summary>
    public TxTaskStage GetTaskStage(TxTaskData task)
    {
        if (!task.amountReached)
        {
            return TxTaskStage.Amount;
        }

        if (task.winProgress < task.winTarget)
        {
            return TxTaskStage.Win;
        }

        if (GetLoginDays() < task.loginTarget)
        {
            return TxTaskStage.Login;
        }

        return TxTaskStage.Completed;
    }

    /// <summary>
    /// 获取 TaskManager 维护的累计签到天数。
    /// </summary>
    public int GetLoginDays()
    {
        return TaskManager.Instance == null || TaskManager.Instance.saveData == null ? 0 : TaskManager.Instance.saveData.loginDays;
    }

    /// <summary>
    /// 加载本地任务数据。
    /// </summary>
    private void LoadTasks()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        saveData = string.IsNullOrEmpty(json) ? CreateDefaultData() : JsonConvert.DeserializeObject<TxSaveData>(json);
        if (saveData == null || saveData.tasks == null)
        {
            saveData = CreateDefaultData();
        }

        SaveTasks();
    }

    /// <summary>
    /// 创建默认的四个金额档位。
    /// </summary>
    private TxSaveData CreateDefaultData()
    {
        return new TxSaveData
        {
            tasks = new List<TxTaskData>
            {
                new TxTaskData { amount = 100, winTarget = 15, loginTarget = 7 },
                new TxTaskData { amount = 200, winTarget = 30, loginTarget = 10 },
                new TxTaskData { amount = 500, winTarget = 75, loginTarget = 15 },
                new TxTaskData { amount = 1000, winTarget = 150, loginTarget = 20 },
            },
        };
    }

    /// <summary>
    /// 保存金额任务状态到本地偏好设置。
    /// </summary>
    private void SaveTasks()
    {
        PlayerPrefs.SetString(SaveKey, JsonConvert.SerializeObject(saveData));
        PlayerPrefs.Save();
    }
}
