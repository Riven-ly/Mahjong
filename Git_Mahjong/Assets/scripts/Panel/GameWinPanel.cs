using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏胜利结算面板。
/// </summary>
public class GameWinPanel : UIBase
{
    public Button nextLevelButton;

    /// <summary>
    /// 注册进入下一关按钮事件。
    /// </summary>
    private void Start()
    {
        nextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
    }

    public override void Refresh(object data = null)
    {
        base.Refresh(data);
    }

    public override void Hide()
    {
        base.Hide();
    }

    /// <summary>
    /// 处理进入下一关操作。
    /// </summary>
    private void OnNextLevelButtonClick()
    {
        AudioManager.Instance.PlayBtnMusic();
        GameManager.Instance.playerInfo.level++;

        callback = () =>
        {
            GameManager.Instance.SavePlayerInfo();
            UIManager.Instance.GetUI<GameScenePanel>().ResetGame();
        };
        Hide();
    }
}
