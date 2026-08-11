using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : UIBase
{

    public static bool IsVibrateEnabled = true;

    public Button hideBtn;
    public Button privacyPolicy;
    public Button termsOfServic;
    //--
    public Button musicBtn;
    public Button nomusicBtn;
    public Button soundBtn;
    public Button nosoundBtn;
    public Button vibrationBtn;
    public Button novibrationBtn;
    //
    public Transform btnAll;
    public Transform panel1;
    public Transform panel1_btnRoot;
    public Transform panel2;
    public Transform panel2_btnRoot;
    //
    public Button continueBtn;
    public Button resetGameBtn;

    private string page_id = "SettingPanel";

    private void OnEnable()
    {
        isOpen = true;
    }
    private void OnDisable()
    {
        isOpen = false;
    }
    void Start()
    {
        hideBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            Hide();
        });
        privacyPolicy.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            OpenPrivacyPolicy();
        });
        termsOfServic.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            OpenTermsOfServic();
        });

        musicBtn.onClick.AddListener(() => {
            AudioManager.Instance.PlayBtnMusic();
            OnMusicClick(false);
        });
        nomusicBtn.onClick.AddListener(() => {
            AudioManager.Instance.PlayBtnMusic();
            OnMusicClick(true);
        });

        soundBtn.onClick.AddListener(() => {
            AudioManager.Instance.PlayBtnMusic();
            OnSoundClick(false);
        });
        nosoundBtn.onClick.AddListener(() => {
            AudioManager.Instance.PlayBtnMusic();
            OnSoundClick(true);
        });

        vibrationBtn.onClick.AddListener(() => {
            AudioManager.Instance.PlayBtnMusic();
            OnVibrationClick(false);
        });
        novibrationBtn.onClick.AddListener(() => {
            AudioManager.Instance.PlayBtnMusic();
            OnVibrationClick(true);
        });

        continueBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            Hide();
        });

        resetGameBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            callback = () =>
            {
                UIManager.Instance.GetUI<GameScenePanel>().ResetGame();
            };
            Hide();
        });

        OnMusicClick(true);
        OnSoundClick(true);
        OnVibrationClick(true);

    }
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        bool isBasePanel = GameManager.Instance.gameType == GameType.LobbyScene;
        panel1.gameObject.SetActive(isBasePanel);
        panel2.gameObject.SetActive(!isBasePanel);
        Transform targetTran = isBasePanel ? panel1_btnRoot : panel2_btnRoot;
        btnAll.SetParent(targetTran);
        btnAll.localPosition = Vector3.zero;
    }
    public override void Hide()
    {
        base.Hide();
    }

    private void OnMusicClick(bool isOpen)
    {
        AudioManager.Instance.MusicState(isOpen);
        musicBtn.gameObject.SetActive(isOpen);
        nomusicBtn.gameObject.SetActive(!isOpen);
    }

    private void OnSoundClick(bool isOpen)
    {
        AudioManager.Instance.SoundState(isOpen);
        soundBtn.gameObject.SetActive(isOpen);
        nosoundBtn.gameObject.SetActive(!isOpen);
    }

    private void OnVibrationClick(bool isOpen)
    {
        SettingPanel.IsVibrateEnabled = isOpen;
        vibrationBtn.gameObject.SetActive(isOpen);
        novibrationBtn.gameObject.SetActive(!isOpen);
    }
    
    public static void OpenPrivacyPolicy()
    {
        Application.OpenURL("https://sites.google.com/view/happy-mahjong-rich-adventure");
    }
    public static void OpenTermsOfServic()
    {
        Application.OpenURL("https://sites.google.com/view/happy-mahjong-rich-adventure");
    }
}
