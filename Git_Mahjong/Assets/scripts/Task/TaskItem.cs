using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单条任务的显示与领奖交互组件。
/// </summary>
public class TaskItem : MonoBehaviour
{
    public Text descriptionText;
    public Text rewardText;
    public Text progressText;
    public Image progressFill;
    public Button actionButton;
    public Text actionText;

    private TaskData taskData;

    /// <summary>
    /// 注册任务操作按钮事件。
    /// </summary>
    private void Awake()
    {
        actionButton.onClick.AddListener(OnActionButtonClick);
    }

    /// <summary>
    /// 根据任务数据刷新卡片显示。
    /// </summary>
    public void SetData(TaskData data)
    {
        taskData = data;
        descriptionText.text = string.Format(LanguageManager.Instance.GetText(taskData.description), taskData.targetProgress);
        rewardText.text = $"+{taskData.goldReward:0}";
        progressText.text = $"{taskData.currentProgress}/{taskData.targetProgress}";
        progressFill.fillAmount = Mathf.Clamp01((float)taskData.currentProgress / taskData.targetProgress);

        if (taskData.isClaimed)
        {
            actionText.text = "已领取";
            actionButton.interactable = false;
            return;
        }

        bool isCompleted = taskData.currentProgress >= taskData.targetProgress;
        actionText.text = isCompleted ? "领取" : "前往";
        actionButton.interactable = true;
    }

    /// <summary>
    /// 领取已完成任务的奖励。
    /// </summary>
    private void OnActionButtonClick()
    {
        if (taskData.currentProgress < taskData.targetProgress)
        {
            UIManager.Instance.GetUI<TaskPanel>().Hide();
            return;
        }

        TaskManager.Instance.ClaimReward(taskData);
    }
}
