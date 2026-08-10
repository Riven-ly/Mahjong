using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 金额面板，展示账户余额和当前金额档位的阶段任务。
/// </summary>
public class TxPanel : UIBase
{
    public Transform root;
    public Button closeButton;
    public Button changeBtn;
    public Button c_Btn;
    public Text c_Btn_text;
    public Text goldLvText;

    public Text t1;
    public Text accountText;
    public Text amountBalanceText;
    public Button[] amountButtons;
    public Image[] amountButtonBackgrounds;
    public Text[] amountButtonTexts;
    public Text[] amountButtonTexts2;
    public Image[] selectedMarks;
    public Text instructionText;
    public Image progressFill;
    public Text progressText;

    public Sprite[] amountButtonSps;
    private void Awake()
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        float topBlockHeight = Screen.height - Screen.safeArea.yMax;
        rect.offsetMax = new Vector2(0, -topBlockHeight);
    }

    private void Start()
    {
        closeButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            AdManager.Instance.OnClickInterstitialAd("");
            Hide();
        });
        changeBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            UIManager.Instance.OpenUI<TxElementTypeSelectPanel>();
        });
        c_Btn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            string str = LanguageManager.Instance.GetText("TxPanel_cBtn");
            UIManager.Instance.OpenUI<GeneralTipsPanel>(string.Format(str, LanguageManager.Instance.GetText_Encrypt("Wh")));
        });
        for (int i = 0; i < amountButtons.Length; i++)
        {
            int taskIndex = i;
            amountButtons[i].onClick.AddListener(() => TxManager.Instance.SelectTask(taskIndex));
        }
        t1.text = LanguageManager.Instance.GetText_Encrypt("CH") + " " + LanguageManager.Instance.GetText_Encrypt("Bl");
        c_Btn_text.text = $"{LanguageManager.Instance.GetText_Encrypt("WD")} {LanguageManager.Instance.GetText_Encrypt("CH")}";
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
        goldLvText.text = $"Lv.{GameManager.Instance.playerInfo.goldLevel}";
        RefreshDisplay();
        RefreshAccountUI();
    }

    public void RefreshAccountUI()
    {
        string str = TxManager.Instance.saveData.AccountStr;
        if(string.IsNullOrEmpty(str))
        {
            str = LanguageManager.Instance.GetText("TxPanel_Account");
        }
        accountText.text = str;
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
            amountButtonTexts2[i].text = $"$ {task.amount}";
            amountButtonTexts[i].gameObject.SetActive(isSelected);
            amountButtonTexts2[i].gameObject.SetActive(!isSelected);

            amountButtonBackgrounds[i].sprite = isSelected ? amountButtonSps[0] : amountButtonSps[1];
            selectedMarks[i].gameObject.SetActive(isSelected);
        }

        TxTaskData selectedTask = TxManager.Instance.Tasks[TxManager.Instance.SelectedTaskIndex];
        TxTaskStage stage = TxManager.Instance.GetTaskStage(selectedTask);
        float currentProgress;
        int targetProgress;
        if (stage == TxTaskStage.Amount)
        {
            currentProgress = GameManager.Instance.playerInfo.Gold;
            targetProgress = selectedTask.amount;
            instructionText.text = string.Format(LanguageManager.Instance.GetText("TxPanel_t1"), LanguageManager.Instance.GetText_Encrypt("Wh"), $"{LanguageManager.Instance.GetText_Encrypt("Special_Diamond__unit")}{targetProgress}");
        }
        else if (stage == TxTaskStage.Win)
        {
            currentProgress = selectedTask.winProgress;
            targetProgress = selectedTask.winTarget;
            instructionText.text = string.Format(LanguageManager.Instance.GetText("TxPanel_t2"), targetProgress);
        }
        else if (stage == TxTaskStage.Login)
        {
            currentProgress = TxManager.Instance.GetLoginDays();
            targetProgress = selectedTask.loginTarget;
            instructionText.text = string.Format(LanguageManager.Instance.GetText("TxPanel_t3"), targetProgress);
        }
        else
        {
            currentProgress = selectedTask.loginTarget;
            targetProgress = selectedTask.loginTarget;
            instructionText.text = LanguageManager.Instance.GetText("TxPanel_t4");
        }

        //progressText.text = $"{currentProgress}/{targetProgress}";
        progressText.text = $"{(int)((currentProgress / targetProgress) * 100)}%";
        progressFill.fillAmount = currentProgress / targetProgress;
    }
}
