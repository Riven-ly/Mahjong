using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务界面，仅负责切换分类与展示任务列表。
/// </summary>
public class TaskPanel : UIBase
{
    public Button closeButton;
    public Button dailyTabButton;
    public Button mainlineTabButton;
    public Button dailyTabButton_h;
    public Button mainlineTabButton_h;
    public Transform contentRoot;
    public TaskItem taskItemPrefab;

    private readonly List<TaskItem> taskItems = new List<TaskItem>();
    private TaskCategory currentCategory;

    /// <summary>
    /// 注册界面交互。
    /// </summary>
    private void Start()
    {
        closeButton.onClick.AddListener(Hide);
        dailyTabButton.onClick.AddListener(() => SwitchCategory(TaskCategory.Daily));
        mainlineTabButton.onClick.AddListener(() => SwitchCategory(TaskCategory.Mainline));
        dailyTabButton_h.onClick.AddListener(() => SwitchCategory(TaskCategory.Daily));
        mainlineTabButton_h.onClick.AddListener(() => SwitchCategory(TaskCategory.Mainline));
    }

    /// <summary>
    /// 面板启用时订阅任务刷新通知。
    /// </summary>
    private void OnEnable()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.TasksChanged += RefreshTasks;
        }
    }

    /// <summary>
    /// 面板禁用时取消订阅任务刷新通知。
    /// </summary>
    private void OnDisable()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.TasksChanged -= RefreshTasks;
        }
    }

    /// <summary>
    /// 打开时默认展示每日任务。
    /// </summary>
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        TaskManager.Instance.RefreshDailyTasks();
        currentCategory = TaskCategory.Daily;
        RefreshTasks();
    }

    public override void Hide()
    {
        base.Hide();
    }

    /// <summary>
    /// 切换当前展示的任务分类。
    /// </summary>
    private void SwitchCategory(TaskCategory category)
    {
        if (currentCategory == category)
        {
            return;
        }

        currentCategory = category;
        RefreshTasks();
    }

    /// <summary>
    /// 重建当前分类的任务列表。
    /// </summary>
    private void RefreshTasks()
    {
        if (TaskManager.Instance == null)
        {
            return;
        }

        for (int i = taskItems.Count - 1; i >= 0; i--)
        {
            Destroy(taskItems[i].gameObject);
        }
        taskItems.Clear();

        List<TaskData> tasks = currentCategory == TaskCategory.Daily ? TaskManager.Instance.DailyTasks : TaskManager.Instance.MainlineTasks;
        dailyTabButton_h.gameObject.SetActive(currentCategory == TaskCategory.Mainline);
        dailyTabButton.gameObject.SetActive(currentCategory == TaskCategory.Daily);
        mainlineTabButton_h.gameObject.SetActive(currentCategory == TaskCategory.Daily);
        mainlineTabButton.gameObject.SetActive(currentCategory == TaskCategory.Mainline);

        CreateTaskItems(tasks, 0);
        CreateTaskItems(tasks, 1);
        CreateTaskItems(tasks, 2);
    }

    /// <summary>
    /// 按任务状态创建任务卡片，已完成待领取优先，已领取任务固定显示在末尾。
    /// </summary>
    private void CreateTaskItems(List<TaskData> tasks, int state)
    {
        foreach (TaskData task in tasks)
        {
            bool isCompleted = task.currentProgress >= task.targetProgress;
            if ((state == 0 && (!isCompleted || task.isClaimed)) ||
                (state == 1 && (isCompleted || task.isClaimed)) ||
                (state == 2 && !task.isClaimed))
            {
                continue;
            }

            TaskItem item = Instantiate(taskItemPrefab, contentRoot);
            item.SetData(task);
            taskItems.Add(item);
        }
    }
}
