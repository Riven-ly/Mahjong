using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 金额面板，展示账户余额和当前金额档位的阶段任务。
/// </summary>
public class TxPanel : UIBase
{
    public Button closeButton;
    public Text amountBalanceText;
    public Button[] amountButtons;
    public Image[] amountButtonBackgrounds;
    public Text[] amountButtonTexts;
    public Image[] selectedMarks;
    public Text instructionText;
    public Image progressFill;
    public Text progressText;

    private void Start()
    {
        closeButton.onClick.AddListener(Hide);
        for (int i = 0; i < amountButtons.Length; i++)
        {
            int taskIndex = i;
            amountButtons[i].onClick.AddListener(() => TxManager.Instance.SelectTask(taskIndex));
        }
    }

    private void OnEnable()
    {
        if (TxManager.Instance != null)
        {
            TxManager.Instance.TasksChanged += RefreshDisplay;
        }
    }

    private void OnDisable()
    {
        if (TxManager.Instance != null)
        {
            TxManager.Instance.TasksChanged -= RefreshDisplay;
        }
    }

    /// <summary>
    /// 刷新面板数据。
    /// </summary>
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        RefreshDisplay();
    }

    /// <summary>
    /// 刷新余额、档位选择和当前阶段进度。
    /// </summary>
    private void RefreshDisplay()
    {
        if (TxManager.Instance == null)
        {
            return;
        }

        TxManager.Instance.RefreshAmountStages();
        amountBalanceText.text = $"$ {GameManager.Instance.playerInfo.Gold:0.##}";
        for (int i = 0; i < TxManager.Instance.Tasks.Count; i++)
        {
            TxTaskData task = TxManager.Instance.Tasks[i];
            bool isSelected = i == TxManager.Instance.SelectedTaskIndex;
            amountButtonTexts[i].text = $"$ {task.amount}";
            amountButtonBackgrounds[i].color = isSelected ? new Color(0.29f, 0.61f, 1f) : new Color(0.58f, 0.58f, 0.58f);
            selectedMarks[i].gameObject.SetActive(isSelected);
        }

        TxTaskData selectedTask = TxManager.Instance.Tasks[TxManager.Instance.SelectedTaskIndex];
        TxTaskStage stage = TxManager.Instance.GetTaskStage(selectedTask);
        int currentProgress;
        int targetProgress;
        if (stage == TxTaskStage.Amount)
        {
            currentProgress = Mathf.FloorToInt(GameManager.Instance.playerInfo.Gold);
            targetProgress = selectedTask.amount;
            instructionText.text = $"Reach $ {targetProgress} amount balance";
        }
        else if (stage == TxTaskStage.Win)
        {
            currentProgress = selectedTask.winProgress;
            targetProgress = selectedTask.winTarget;
            instructionText.text = $"Complete {targetProgress} levels";
        }
        else if (stage == TxTaskStage.Login)
        {
            currentProgress = TxManager.Instance.GetLoginDays();
            targetProgress = selectedTask.loginTarget;
            instructionText.text = $"Check in for {targetProgress} days";
        }
        else
        {
            currentProgress = selectedTask.loginTarget;
            targetProgress = selectedTask.loginTarget;
            instructionText.text = "Task completed";
        }

        progressText.text = $"{currentProgress}/{targetProgress}";
        progressFill.fillAmount = (float)currentProgress / targetProgress;
    }
}
