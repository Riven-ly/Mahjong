using DG.Tweening;
using MahjongGame.View;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏场景面板及页面导航入口。
/// </summary>
public class GameScenePanel : UIBase,IEventListener
{
    public Transform root;
    public MahjongGameplayView gameplayView; // 麻将主玩法视图组件
    public Button SettingBtn;
    public Button taskBtn;
    public Image taskRedImg;
    public Text levelText;

    public GameSceneItem_Exchange gameSceneItem_Exchange;
    public GameSceneItem_Hint gameSceneItem_Hint;
    public GameSceneItem_Return gameSceneItem_Return;
    [SerializeField] private Transform health1; // 第一格生命节点
    [SerializeField] private Transform health2; // 第二格生命节点
    [SerializeField] private Transform health3; // 第三格生命节点

    private readonly CanvasGroup[] healthCanvasGroups = new CanvasGroup[3]; // 三格生命的淡出控制组件

    private const string FirstGameGuideCompletedKey = "FirstGameGuideCompleted"; // 首关新手引导完成标记
    private const string FirstGameStageProgressKey = "FirstGameStageProgress"; // 首关新手引导小关卡进度
    private const int FirstGameLevel = 1; // 首关的玩家等级

    private int firstGameStageIndex; // 首关当前正在玩的小关卡序号，从 1 开始

    private static readonly string[] FirstGameStageTextKeys =
    {
        "GuideMatchSymbols",
        "GuideUncoverHidden",
        "GuideUnlockSide"
    };

    /// <summary>
    /// 判断当前是否处于首关的新手引导小关卡流程。
    /// </summary>
    private bool IsFirstGameTutorial =>
        GameManager.Instance.playerInfo.level == FirstGameLevel && TutorialStageCatalogLoader.StageCount > 0;

    /// <summary>
    /// 从本地进度读取首关新手引导的当前小关卡序号。
    /// </summary>
    private static int LoadFirstGameStageIndex()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(FirstGameStageProgressKey, 1), 1, Mathf.Max(1, TutorialStageCatalogLoader.StageCount));
    }

    /// <summary>
    /// 记录首关新手引导的当前小关卡序号。
    /// </summary>
    private static void SaveFirstGameStageIndex(int stageIndex)
    {
        PlayerPrefs.SetInt(FirstGameStageProgressKey, stageIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 初始化安全区与生命显示引用。
    /// </summary>
    private void Awake()
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        float topBlockHeight = Screen.height - Screen.safeArea.yMax;
        rect.offsetMax = new Vector2(0, -topBlockHeight);
        InitializeHealthCanvasGroups();
    }
    /// <summary>
    /// 注册设置按钮事件。
    /// </summary>
    private void Start()
    {
        SettingBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            UIManager.Instance.OpenUI<SettingPanel>();
        });
        taskBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            UIManager.Instance.OpenUI<TaskPanel>();
        });

        TaskManager.Instance.TasksChanged += RefreshTaskRedPoint;
    }

    /// <summary>
    /// 面板启用时注册玩法结果、提示关闭与任务刷新事件。
    /// </summary>
    private void OnEnable()
    {
        isOpen = false;
        EventManager.Instance.RegisterListener(GameEvent.MahjongGameWon, this);
        EventManager.Instance.RegisterListener(GameEvent.MahjongGameLost, this);
        EventManager.Instance.RegisterListener(GameEvent.StopHintAnim, this);
        gameplayView.HealthChanged += RefreshHealth;
    }

    /// <summary>
    /// 面板禁用时注销玩法结果与提示关闭事件。
    /// </summary>
    private void OnDisable()
    {
        isOpen = false;
        EventManager.Instance.UnregisterListener(GameEvent.MahjongGameWon, this);
        EventManager.Instance.UnregisterListener(GameEvent.MahjongGameLost, this);
        EventManager.Instance.UnregisterListener(GameEvent.StopHintAnim, this);
        gameplayView.HealthChanged -= RefreshHealth;
    }

    /// <summary>
    /// 刷新任务按钮上的可领取任务红点。
    /// </summary>
    private void RefreshTaskRedPoint()
    {
        if (taskRedImg == null)
        {
            return;
        }

        taskRedImg.gameObject.SetActive(TaskManager.Instance != null && TaskManager.Instance.HasClaimableTasks());
    }

    /// <summary>
    /// 根据玩家等级读取关卡配置并开始新游戏。
    /// </summary>
    public override void Refresh(object data = null)
    {
        int playerLevel = GameManager.Instance.playerInfo.level;
        levelText.text = LanguageManager.Instance.GetText("Level") + $" {PhaseTaskService.GetLevelDisplayNumber()}";

        if (IsFirstGameTutorial)
        {
            StartFirstGameStage(LoadFirstGameStageIndex());
        }
        else
        {
            gameplayView.StartNewGame(MahjongLevelCatalogLoader.GetLevel(playerLevel));
        }

        RefreshHealth(MahjongViewConfig.InitialHealth);

        gameSceneItem_Exchange.Refresh();
        gameSceneItem_Hint.Refresh();
        gameSceneItem_Return.Refresh();
        RefreshTaskRedPoint();

        OtherSdkManager.Instance.CustomEvent("level_start", "level_id", GameManager.Instance.playerInfo.level);

        ShowGuideIfNeeded();
    }
    void ShowGuideIfNeeded()
    {
        if (GuideIntro.Shown)
            return;

        DOTween.Sequence().AppendInterval(0.4f).AppendCallback(() =>
        {
            UIManager.Instance.OpenUI<GuidePop>(new GuidePopData(OnGuidePopCompleted));
        });
    }
    /// <summary>
    /// 新手引导关闭后补充第一关的关卡任务提示。
    /// </summary>
    void OnGuidePopCompleted()
    {
        GuideIntro.MarkShown();

        LevelTaskTipData tipData = new LevelTaskTipData(
            PhaseTaskService.GetLevelDisplayNumber(),
            PhaseTaskService.GetCurrentTaskText(),
            null,
            false);

        if (tipData.CanOpen)
            UIManager.Instance.OpenUI<LevelTaskTip>(tipData, () =>
            {
                UIManager.Instance.GetUI<GameScenePanel>().TryOpenFirstGameGuide();
            });
    }

    /// <summary>
    /// 加载首关指定序号的新手引导小关卡。
    /// 第 1 小关的手势引导由 LevelTaskTip 关闭后的回调触发，此处不重复调用；
    /// 第 2、3 小关无前置 LevelTaskTip，直接打开引导。
    /// </summary>
    private void StartFirstGameStage(int stageIndex)
    {
        int clampedStage = Mathf.Clamp(stageIndex, 1, TutorialStageCatalogLoader.StageCount);
        firstGameStageIndex = clampedStage;
        SaveFirstGameStageIndex(clampedStage);
        gameplayView.StartNewGame(TutorialStageCatalogLoader.CreateRuntimeStage(clampedStage));

        if (clampedStage > 1)
        {
            TryOpenFirstGameGuide();
        }
    }

    /// <summary>
    /// 首关当前小关卡完成，推进至下一个小关卡或结束引导流程。
    /// </summary>
    private void OnFirstGameStageCleared()
    {
        if (!IsFirstGameTutorial)
        {
            return;
        }

        if (firstGameStageIndex >= TutorialStageCatalogLoader.StageCount)
        {
            firstGameStageIndex = 0;
            PlayerPrefs.SetInt(FirstGameStageProgressKey, 1);
            PlayerPrefs.Save();
            UIManager.Instance.OpenUI<GameWinPanel>();
            return;
        }

        StartFirstGameStage(firstGameStageIndex + 1);
    }

    /// <summary>
    /// 隐藏主玩法界面并停止当前界面的全部补间动画。
    /// </summary>
    public override void Hide()
    {
        gameplayView.StopGameplay();
        base.Hide();
    }

    /// <summary>
    /// 重置游戏
    /// </summary>
    public void ResetGame()
    {
        Refresh();
        PhaseTaskService.NotifyProgressRefresh();
    }

    /// <summary>
    /// 缓存三格生命的 CanvasGroup 引用。
    /// </summary>
    private void InitializeHealthCanvasGroups()
    {
        healthCanvasGroups[0] = health1.GetComponent<CanvasGroup>();
        healthCanvasGroups[1] = health2.GetComponent<CanvasGroup>();
        healthCanvasGroups[2] = health3.GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 刷新三格生命显示；扣血时将失去的生命淡出。
    /// </summary>
    private void RefreshHealth(int remainingHealth)
    {
        for (int i = 0; i < healthCanvasGroups.Length; i++)
        {
            CanvasGroup healthCanvasGroup = healthCanvasGroups[i];
            bool active = i < remainingHealth;
            healthCanvasGroup.DOKill();
            if (active)
            {
                healthCanvasGroup.gameObject.SetActive(true);
                healthCanvasGroup.alpha = 1f;
            }
            else if (healthCanvasGroup.gameObject.activeSelf)
            {
                healthCanvasGroup.DOFade(0f, MahjongViewConfig.HealthFadeDuration)
                    .OnComplete(() => healthCanvasGroup.gameObject.SetActive(false));
            }
        }
    }

    /// <summary>
    /// 打开当前小关卡指向首组可消除牌的手势引导；该小关引导已完成时跳过。
    /// </summary>
    public void TryOpenFirstGameGuide()
    {
        if (HasShownFirstGameGuide(firstGameStageIndex) ||
            !gameplayView.TryShowFirstGameGuide(
                out int firstTargetCardId,
                out RectTransform firstTargetRectTransform,
                out int secondTargetCardId,
                out RectTransform secondTargetRectTransform))
        {
            return;
        }

        MarkFirstGameGuideShown(firstGameStageIndex);
        UIManager.Instance.OpenUI<GuidePanel_firstGame>(new FirstGameGuideData(
            gameplayView,
            firstTargetCardId,
            firstTargetRectTransform,
            secondTargetCardId,
            secondTargetRectTransform,
            ResolveFirstGameStageTextKey(firstGameStageIndex)));
    }

    /// <summary>
    /// 判断首关指定小关卡的手势引导是否已展示过。
    /// </summary>
    private static bool HasShownFirstGameGuide(int stageIndex)
    {
        if (stageIndex <= 1)
        {
            return PlayerPrefs.GetInt(FirstGameGuideCompletedKey, 0) != 0;
        }

        return PlayerPrefs.GetInt(FirstGameGuideCompletedKey + stageIndex, 0) != 0;
    }

    /// <summary>
    /// 记录首关指定小关卡的手势引导已展示。
    /// </summary>
    private static void MarkFirstGameGuideShown(int stageIndex)
    {
        string key = stageIndex <= 1 ? FirstGameGuideCompletedKey : FirstGameGuideCompletedKey + stageIndex;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 获取首关指定小关卡对应的引导文案键。
    /// </summary>
    private static string ResolveFirstGameStageTextKey(int stageIndex)
    {
        int index = Mathf.Clamp(stageIndex - 1, 0, FirstGameStageTextKeys.Length - 1);
        return FirstGameStageTextKeys[index];
    }

    /// <summary>
    /// 尝试启动返回道具的五组自动消除。
    /// </summary>
    public bool TryAutoEliminateMahjongPairs()
    {
        return gameplayView.TryAutoEliminate(5);
    }

    /// <summary>
    /// 尝试随机交换游戏区域内卡牌的位置。
    /// </summary>
    public bool TryShuffleMahjongCards()
    {
        return gameplayView.TryShuffle();
    }

    /// <summary>
    /// 尝试显示一组可消除牌面的提示特效。
    /// </summary>
    public bool TryShowMahjongHint()
    {
        return gameplayView.TryShowHint();
    }

    /// <summary>
    /// 关闭全部麻将牌的提示特效。
    /// </summary>
    public void StopMahjongHint()
    {
        gameplayView.StopHint();
    }

    /// <summary>
    /// 响应玩法结果与提示关闭事件。
    /// </summary>
    public void OnEventTriggered(GameEvent eventType, object data = null)
    {
        switch (eventType)
        {
            case GameEvent.MahjongGameWon:
                UIManager.Instance.OpenUIMask();
                DOTween.Sequence().AppendInterval(1f).AppendCallback(() =>
                {
                    UIManager.Instance.HideUIMask();
                    if (IsFirstGameTutorial && firstGameStageIndex < TutorialStageCatalogLoader.StageCount)
                    {
                        OnFirstGameStageCleared();
                        return;
                    }

                    UIManager.Instance.OpenUI<GameWinPanel>();
                });
                break;
            case GameEvent.MahjongGameLost:
                UIManager.Instance.OpenUI<GameLostPanel>();
                break;
            case GameEvent.StopHintAnim:
                StopMahjongHint();
                break;
        }
    }
}
