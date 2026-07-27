using UnityEngine;
using UnityEngine.UI;

public class GameScenePanel : UIBase
{
    public Button lobbyEnter;

    private void Start()
    {
        lobbyEnter.onClick.AddListener(() =>
        {
            if (AudioManager.Instance.btnMusic != null)
            {
                AudioManager.Instance.PlayBtnMusic();
            }
            GameManager.Instance.gameType = GameType.LobbyScene;
            UIManager.Instance.OpenUI<LobbyScenePanel>();
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
