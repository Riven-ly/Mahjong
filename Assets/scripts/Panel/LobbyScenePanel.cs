using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 麻将大厅界面及进入主玩法的导航入口。
/// </summary>
public class LobbyScenePanel : UIBase
{
    public Button gameEnter;

    /// <summary>
    /// 注册进入主玩法按钮事件。
    /// </summary>
    private void Awake()
    {
        gameEnter.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            GameManager.Instance.gameType = GameType.MainGame;
            UIManager.Instance.OpenUI<GameScenePanel>();
            Hide();
        });
    }
    /// <summary>
    /// 刷新大厅界面。当前界面暂无动态数据。
    /// </summary>
    public override void Refresh(object data = null)
    {

    }

    /// <summary>
    /// 隐藏大厅界面并执行基础面板关闭流程。
    /// </summary>
    public override void Hide()
    {
        base.Hide();
    }
}
