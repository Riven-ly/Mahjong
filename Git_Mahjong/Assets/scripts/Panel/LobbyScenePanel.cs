using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScenePanel : UIBase
{
    public Button gameEnter;

    private void Start()
    {
        gameEnter.onClick.AddListener(() =>
        {
            if (AudioManager.Instance.btnMusic != null)
            {
                AudioManager.Instance.PlayBtnMusic();
            }
            GameManager.Instance.gameType = GameType.MainGame;
            UIManager.Instance.OpenUI<GameScenePanel>();
            Hide();
        });
    }
    public override void Refresh(object data = null)
    {

    }

    public override void Hide()
    {
        base.Hide();
    }
}
