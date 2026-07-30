using System;
using MahjongGame.View;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏场景面板及页面导航入口。
/// </summary>
public class GameScenePanel : UIBase,IEventListener
{
    public MahjongGameplayView gameplayView; // 麻将主玩法视图组件
    public Button SettingBtn; 

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
    }

    private void OnEnable()
    {
        EventManager.Instance.RegisterListener(GameEvent.MahjongGameWon, this);
        EventManager.Instance.RegisterListener(GameEvent.MahjongGameLost, this);
    }

    private void OnDisable()
    {
        EventManager.Instance.UnregisterListener(GameEvent.MahjongGameWon, this);
        EventManager.Instance.UnregisterListener(GameEvent.MahjongGameLost, this);
    }

    /// <summary>
    /// 根据玩家等级读取关卡配置并开始新游戏。
    /// </summary>
    public override void Refresh(object data = null)
    {
        int playerLevel = GameManager.Instance.playerInfo.level;
        gameplayView.StartNewGame(MahjongLevelCatalogLoader.GetLevel(playerLevel, Environment.TickCount));
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

    public void OnEventTriggered(GameEvent eventType, object data = null)
    {
        switch (eventType)
        {
            case GameEvent.MahjongGameWon:
                Debug.Log("游戏胜利");
                break;
            case GameEvent.MahjongGameLost:
                Debug.Log("游戏失败");
                break;
        }
    }
}
