using System;
using MahjongGame.View;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏场景面板及页面导航入口。
/// </summary>
public class GameScenePanel : UIBase
{
    [SerializeField] private MahjongGameplayView gameplayView; // 麻将主玩法视图组件
    [SerializeField] private Button lobbyEnter; // 返回大厅按钮

    public event Action LobbyEnterRequested; // 返回大厅请求事件

    /// <summary>
    /// 初始化主玩法逻辑并注册返回大厅按钮事件。
    /// </summary>
    private void Awake()
    {
        lobbyEnter.onClick.AddListener(HandleLobbyEnterClicked);
    }

    /// <summary>
    /// 根据玩家等级读取关卡配置并开始新游戏。data 可传入 int 随机种子，用于缺失等级时随机选择关卡。
    /// </summary>
    public override void Refresh(object data = null)
    {
        int randomSeed = data is int seed ? seed : Environment.TickCount;
        int playerLevel = GameManager.Instance.playerInfo.level;
        gameplayView.StartNewGame(MahjongLevelCatalogLoader.GetLevel(playerLevel, randomSeed));
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
    /// 处理返回大厅按钮点击，派发请求并切换现有 UI 面板。
    /// </summary>
    private void HandleLobbyEnterClicked()
    {
        LobbyEnterRequested?.Invoke();
        GameManager.Instance.gameType = GameType.LobbyScene;
        UIManager.Instance.OpenUI<LobbyScenePanel>();
        Hide();
    }

}
