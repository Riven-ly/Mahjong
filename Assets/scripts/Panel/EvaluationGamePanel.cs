using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EvaluationGamePanel : UIBase
{
    public Transform bg1;
    public Transform bg2;

    public Button btn1;
    public Button btn2;
    public Button btn3;
    public Button btn4;
    public Button star1;
    public Button star2;
    public Button star3;
    public Button star4;
    public Button star5;

    public GameObject starObj1;
    public GameObject starObj2;
    public GameObject starObj3;
    public GameObject starObj4;
    public GameObject starObj5;

    private int star = 0;

    // Start is called before the first frame update
    void Start()
    {
        btn1.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            Hide();

        });
        btn2.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            bg1.gameObject.SetActive(false);
            bg2.gameObject.SetActive(true);
   
        });
        btn3.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            Hide();
          
        });
        btn4.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            PlayerPrefs.SetInt("EvaluationGameStar", 5);
            callback = () =>
            {
                PingJiaTiaoZhuan();
            };
            Hide();
        });

        star1.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            SetStar(1);
        });
        star2.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            SetStar(2);
        });
        star3.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            SetStar(3);
        });
        star4.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            SetStar(4);
        });
        star5.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            SetStar(5);
        });
    }
    public void SetStar(int _star)
    {
        star = _star;
        starObj1.gameObject.SetActive(_star >= 1);
        starObj2.gameObject.SetActive(_star >= 2);
        starObj3.gameObject.SetActive(_star >= 3);
        starObj4.gameObject.SetActive(_star >= 4);
        starObj5.gameObject.SetActive(_star >= 5);
    }
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        bg1.gameObject.SetActive(false);
        bg2.gameObject.SetActive(true);


        SetStar(0);
        //GameScenePanel.isPause = true;
    }
    public override void Hide()
    {
        //GameScenePanel.isPause = false;
        base.Hide();
        PlayerPrefs.SetString("EvaluationGame", "yes");
    }

    private void PingJiaTiaoZhuan()
    {
#if UNITY_ANDROID
        Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
#elif UNITY_IOS
        return;
        string reviewUrl = $"itms-apps://itunes.apple.com/app/id{"IOSAppId"}?action=write-review";
        Application.OpenURL(reviewUrl);
#endif
    }
}
