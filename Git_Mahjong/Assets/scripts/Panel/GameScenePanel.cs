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
    public Transform bobao;

    public GameSceneItem_Exchange gameSceneItem_Exchange;
    public GameSceneItem_Hint gameSceneItem_Hint;
    public GameSceneItem_Return gameSceneItem_Return;
    [SerializeField] private Transform health1; // 第一格生命节点
    [SerializeField] private Transform health2; // 第二格生命节点
    [SerializeField] private Transform health3; // 第三格生命节点

    private readonly CanvasGroup[] healthCanvasGroups = new CanvasGroup[3]; // 三格生命的淡出控制组件

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
        levelText.text = LanguageManager.Instance.GetText("Level") + $" {playerLevel}";
        gameplayView.StartNewGame(MahjongLevelCatalogLoader.GetLevel(playerLevel));
        RefreshHealth(MahjongViewConfig.InitialHealth);

        gameSceneItem_Exchange.Refresh();
        gameSceneItem_Hint.Refresh();
        gameSceneItem_Return.Refresh();
        RefreshTaskRedPoint();
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
