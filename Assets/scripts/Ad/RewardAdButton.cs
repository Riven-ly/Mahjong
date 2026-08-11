using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RewardAdButton : MonoBehaviour
{
    public Button adsBtn;
    public CanvasGroup adsCanvasGroup;
    public Transform adIcon;

    private bool isGetAdsReward;
    private Action adsCallback;

    private string page_id;
    private bool isContainGold;
    public void Init(Action _adsCallback,string _page_id ,bool _isContainGold)
    {
        adsCallback = _adsCallback;
        page_id = _page_id;
        isContainGold = _isContainGold;
        adsBtn.onClick.RemoveAllListeners();
        adsBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            AdsOnClick();
        });

        UpdateAdsBtnState(true);
        isGetAdsReward = false;

        adIcon.gameObject.SetActive(AdManager.ShowAdIcon);
    }

    private void AdsOnClick()
    {
        UIManager.Instance.OpenUIMask();
        UpdateAdsBtnState(false);
        Debug.Log("播放激励广告 page_id :" + page_id);
        //播放广告
        if(isContainGold)
        {
            AdManager.Instance.ShowRewardedAd(
                 page_id,
                 AdsCallback,
                 AdsPlayError
           );
        }
        else
        {
            AdManager.Instance.ShowRewardedAd(
                 page_id,
                 AdsCallback,
                 AdsPlayError
               );
        }
    }
    private void AdsCallback()
    {
        UIManager.Instance.HideUIMask();
        UpdateAdsBtnState(false);
        //获得奖励
        isGetAdsReward = true;
        adsCallback?.Invoke();
        adsCallback = null;
    }
    private void AdsPlayError()
    {
        UIManager.Instance.HideUIMask();
        if (!isGetAdsReward)
        {
            UpdateAdsBtnState(true);
        }
    }
    public void UpdateAdsBtnState(bool _bool)
    {
        adsBtn.interactable = _bool;     
        if(adsCanvasGroup != null)
        {
            adsCanvasGroup.alpha = _bool ? 1 : 0.5f;
        }
    }
}
