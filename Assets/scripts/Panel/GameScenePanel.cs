using DG.Tweening;
using MahjongGame.Model;
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
    public Transform levlepoint1;
    public Transform levlepoint2;
    public Text majiangNum;

    public GameSceneItem_Exchange gameSceneItem_Exchange;
    public GameSceneItem_Hint gameSceneItem_Hint;
    public GameSceneItem_Return gameSceneItem_Return;
    [SerializeField] private Transform health1; // 第一格生命节点
    [SerializeField] private Transform health2; // 第二格生命节点
    [SerializeField] private Transform health3; // 第三格生命节点

    private readonly CanvasGroup[] healthCanvasGroups = new CanvasGroup[3]; // 三格生命的淡出控制组件

    private const string FirstGameGuideCompletedKey = "FirstGameGuideCompleted"; // 首关新手引导完成标记
    private const string FirstGameStageProgressKey = "FirstGameStageProgress"; // 首关新手引导小关卡进度
    private const string SceneItemsInitialGrantKey = "SceneItemsInitialGrant"; // 第3关首次显示道具时的赠送标记
    private const int FirstGameLevel = 1; // 首关的玩家等级
    private const int SceneItemsUnlockLevel = 3; // 提示和洗牌道具从第3关开始显示
    private const int LevelConfigRedirectStart = 5; // 从玩家第5关开始改读后段关卡配置
    private const int LevelConfigRedirectTarget = 100; // 玩家第5关对应配置第100关

    private int firstGameStageIndex; // 首关当前正在玩的小关卡序号，从 1 开始
    private int levelEntryVersion;
    private UIBase levelEntryPopup;
    public bool IsEnteringLevel { get; private set; }

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
        gameplayView.RemainingPairsChanged += RefreshMajiangNum;
    }

    /// <summary>
    /// 面板禁用时注销玩法结果与提示关闭事件。
    /// </summary>
    private void OnDisable()
    {
        isOpen = false;
        if (IsEnteringLevel)
        {
            CancelLevelEntry();
            gameplayView.StopGameplay();
        }
        EventManager.Instance.UnregisterListener(GameEvent.MahjongGameWon, this);
        EventManager.Instance.UnregisterListener(GameEvent.MahjongGameLost, this);
        EventManager.Instance.UnregisterListener(GameEvent.StopHintAnim, this);
        gameplayView.HealthChanged -= RefreshHealth;
        gameplayView.RemainingPairsChanged -= RefreshMajiangNum;
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
        var onStartupDoorClosed = data as Action;
        BeginLevelEntry(() =>
        {
            PrepareLevel();
            onStartupDoorClosed?.Invoke();
        }, closeFirst: onStartupDoorClosed != null);
    }

    private void PrepareLevel()
    {
        int playerLevel = GameManager.Instance.playerInfo.level;
        int displayLevel = PhaseTaskService.GetLevelDisplayNumber();
        int configLevel = ResolveLevelConfig(playerLevel);
        levelText.text = LanguageManager.Instance.GetText("Level") + $" {displayLevel}";

        RefreshHealth(MahjongViewConfig.InitialHealth);

        bool sceneItemsVisible = playerLevel >= SceneItemsUnlockLevel;
        gameSceneItem_Exchange.gameObject.SetActive(sceneItemsVisible);
        gameSceneItem_Hint.gameObject.SetActive(sceneItemsVisible);
        if (sceneItemsVisible)
        {
            EnsureInitialSceneItems();
            gameSceneItem_Exchange.Refresh();
            gameSceneItem_Hint.Refresh();
        }
        gameSceneItem_Return.Refresh();
        RefreshTaskRedPoint();

        if(GameManager.Instance.IsPhaseOne())
        {
            levelText.transform.SetParent(levlepoint1);
            levelText.transform.localPosition = Vector3.zero;
        }
        else
        {
            levelText.transform.SetParent(levlepoint2);
            levelText.transform.localPosition = Vector3.zero;
        }
        MahjongLevelDefinition level = IsFirstGameTutorial
            ? LoadFirstGameStage(LoadFirstGameStageIndex())
            : MahjongLevelCatalogLoader.GetLevel(configLevel);
        int loadedConfigLevel = level != null ? level.level : -1;
        Debug.Log($"[LevelDebug] 当前数据关卡={playerLevel}，游戏显示关卡={displayLevel}，实际配置关卡={loadedConfigLevel}");
        gameplayView.PrepareNewGame(level);

        OtherSdkManager.Instance.CustomEvent("level_start", "level_id", playerLevel);
    }

    /// <summary>
    /// 仅在运行时映射关卡配置：玩家1-4关保持不变，玩家5关起依次读取配置100、101、102……
    /// </summary>
    private static int ResolveLevelConfig(int playerLevel)
    {
        return playerLevel < LevelConfigRedirectStart
            ? playerLevel
            : LevelConfigRedirectTarget + playerLevel - LevelConfigRedirectStart;
    }

    /// <summary>
    /// 第3关首次解锁提示和洗牌道具时，各保证至少有1个；只执行一次，消耗后不会重复赠送。
    /// </summary>
    private static void EnsureInitialSceneItems()
    {
        if (PlayerPrefs.GetInt(SceneItemsInitialGrantKey, 0) != 0)
        {
            return;
        }

        PlayerInfo playerInfo = GameManager.Instance.playerInfo;
        if (playerInfo.gameSceneItem_Hint <= 0)
        {
            playerInfo.Add_item_hint(1);
        }
        if (playerInfo.gameSceneItem_Exchange <= 0)
        {
            playerInfo.Add_item_exchange(1);
        }

        PlayerPrefs.SetInt(SceneItemsInitialGrantKey, 1);
        GameManager.Instance.SavePlayerInfo();
    }

    private void BeginLevelEntry(Action prepareLevel, bool closeFirst = false, Action afterLevelTip = null)
    {
        if (IsEnteringLevel)
            CancelLevelEntry();

        int entryVersion = ++levelEntryVersion;
        IsEnteringLevel = true;

        void PrepareBoard()
        {
            if (IsCurrentEntry(entryVersion))
                prepareLevel();
        }

        void StartBoard()
        {
            if (IsCurrentEntry(entryVersion))
                gameplayView.PlayEntranceAnimation(() => OnBoardEntranceCompleted(entryVersion, afterLevelTip));
        }

        object doorData = (Action)StartBoard;
        if (closeFirst)
        {
            gameplayView.StopGameplay();
            doorData = new LevelEntrancePanel.CloseThenOpen(PrepareBoard, StartBoard);
        }
        else
        {
            PrepareBoard();
        }

        if (UIManager.Instance.TryOpenUI<LevelEntrancePanel>(doorData))
            return;

        if (closeFirst)
            PrepareBoard();
        StartBoard();
    }

    private void OnBoardEntranceCompleted(int entryVersion, Action afterLevelTip)
    {
        if (!IsCurrentEntry(entryVersion))
            return;

        UIManager.Instance.HideUI<LevelEntrancePanel>();
        Action finish = () => CompleteLevelEntry(entryVersion, afterLevelTip);
        bool needsIntro = !GuideIntro.Shown;
        if (needsIntro && UIManager.Instance.TryGetUI<GuidePop>(out var intro))
        {
            levelEntryPopup = intro;
            intro.Open(new GuidePopData(() =>
            {
                if (IsCurrentEntry(entryVersion))
                    ShowLevelTaskTip(finish);
            }));
        }
        else if (needsIntro || afterLevelTip != null)
        {
            ShowLevelTaskTip(finish);
        }
        else
        {
            finish();
        }
    }

    private void ShowLevelTaskTip(Action onComplete)
    {
        var tipData = new LevelTaskTipData(
            PhaseTaskService.GetLevelDisplayNumber(),
            PhaseTaskService.GetCurrentTaskText(),
            onComplete,
            false);

        if (tipData.CanOpen && UIManager.Instance.TryGetUI<LevelTaskTip>(out var tip))
        {
            levelEntryPopup = tip;
            tip.Open(tipData);
        }
        else
            tipData.Skip();
    }

    private void CompleteLevelEntry(int entryVersion, Action afterLevelTip)
    {
        if (!IsCurrentEntry(entryVersion))
            return;

        IsEnteringLevel = false;
        levelEntryPopup = null;
        gameplayView.CompleteLevelEntry();
        if (IsFirstGameTutorial)
            TryOpenFirstGameGuide();
        afterLevelTip?.Invoke();
    }

    private bool IsCurrentEntry(int entryVersion) => IsEnteringLevel && entryVersion == levelEntryVersion;

    private void CancelLevelEntry()
    {
        IsEnteringLevel = false;
        var popup = levelEntryPopup;
        levelEntryPopup = null;
        if (popup != null && popup.gameObject.activeSelf)
            popup.Hide();
        if (UIManager.Instance != null && UIManager.Instance.TryGetUI<LevelEntrancePanel>(out var entrance))
            entrance.RightAwayHide();
    }

    /// <summary>
    /// 保存首关小关卡进度并加载对应牌面配置。
    /// </summary>
    private MahjongLevelDefinition LoadFirstGameStage(int stageIndex)
    {
        int clampedStage = Mathf.Clamp(stageIndex, 1, TutorialStageCatalogLoader.StageCount);
        firstGameStageIndex = clampedStage;
        SaveFirstGameStageIndex(clampedStage);
        return TutorialStageCatalogLoader.CreateRuntimeStage(clampedStage);
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

        gameplayView.PrepareNewGame(LoadFirstGameStage(firstGameStageIndex + 1), hideBoardForEntrance: false);
        gameplayView.CompleteLevelEntry();
        TryOpenFirstGameGuide();
    }

    /// <summary>
    /// 隐藏主玩法界面并停止当前界面的全部补间动画。
    /// </summary>
    public override void Hide()
    {
        CancelLevelEntry();
        gameplayView.StopGameplay();
        base.Hide();
    }

    /// <summary>
    /// 重置游戏
    /// </summary>
    public void ResetGame(bool closeFirst = false)
    {
        BeginLevelEntry(() =>
        {
            PrepareLevel();
            PhaseTaskService.NotifyProgressRefresh();
        }, closeFirst: closeFirst);
    }

    public void PlayNextLevelTransition(Action afterLevelTip)
    {
        BeginLevelEntry(() =>
        {
            PrepareLevel();
            PhaseTaskService.NotifyProgressRefresh();
        }, closeFirst: true, afterLevelTip: afterLevelTip);
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

    private void RefreshMajiangNum(int remainingPairs)
    {
        majiangNum.text = string.Format(LanguageManager.Instance.GetText("MatchesCount"), remainingPairs);
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
            ResolveFirstGameStageTextKey(firstGameStageIndex),
            firstGameStageIndex));
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
