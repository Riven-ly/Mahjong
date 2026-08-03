using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏失败结算面板。
/// </summary>
public class GameLostPanel : UIBase
{
    public Button restartButton;

    /// <summary>
    /// 注册重新开局按钮事件。
    /// </summary>
    private void Start()
    {
        restartButton.onClick.AddListener(OnRestartButtonClick);
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
    /// 处理重新开局操作。
    /// </summary>
    private void OnRestartButtonClick()
    {
        AudioManager.Instance.PlayBtnMusic();
        callback = () =>
        {
            UIManager.Instance.GetUI<GameScenePanel>().ResetGame();
        };
        Hide();
    }
}
