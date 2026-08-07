using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoUI : UIBase
{
    [Header("Gold")]
    public Transform goldTrans;
    public Image goldIcon;
    public Text goldCnt;
    public Canvas goldCanvas;
    public Transform txTrans;
    [Header("Diamond")]
    public Transform diamondTrans;
    public Image diamondIcon;
    public Text diamondCnt;
    public Canvas diamondCanvas;

    private void Awake()
    {
        RectTransform rect = GetComponent<RectTransform>();
        float topBlockHeight = Screen.height - Screen.safeArea.yMax;
        rect.offsetMax = new Vector2(0, -topBlockHeight);
    }
    private void OnEnable()
    {
        isOpen = false;
    }
    private void OnDisable()
    {
        isOpen = false;
    }
    private void Start()
    {
        GameManager.Instance.UpdateAppATTToDiamond(goldIcon);

        //if (TxElementMananger.Instance != null)
        //{
        //    var obj = Instantiate(TxElementMananger.Instance.TxProgressPrefab, txTrans);
        //    obj.transform.localPosition = Vector3.zero;
        //}
    }

    public override void Refresh(object data = null)
    {
        base.Refresh(data);

        RefreshGoldUI();
        RefreshDiamondUI();
    }
    public override void Hide()
    {
        base.Hide();
    }


    public void RefreshGoldUI()
    {
        goldCnt.text = GameManager.Instance.playerInfo.Gold.ToString();
    }

    public void RefreshDiamondUI()
    {
        diamondCnt.text = GameManager.Instance.playerInfo.Diamond.ToString();
    }

    //------------------------------------------
    public void GoldFlyAnim(Vector3 start)
    {
        GoldCollectEffect.Instance.StartEffect(ItemType.Gold, start, goldIcon.transform.position);
        DOTween.Sequence().AppendInterval(0.8f).AppendCallback(() =>
        {
            StartGoldAnim();
        });
    }
    public void GoldCanvasTop()
    {
        goldCanvas.sortingOrder = 510;
    }
    public void GoldCanvasRecover()
    {
        goldCanvas.sortingOrder = 410;
    }

    public void StartGoldAnim()
    {
        goldTrans.DOKill();
        float _currentValue = float.Parse(goldCnt.text);
        float targetGold = GameManager.Instance.playerInfo.Gold;
        bool hasDecimal1 = targetGold != Mathf.RoundToInt(targetGold); // true（有小数）
        int unit = hasDecimal1 ? 2 : 0;

        DOTween.To(
          () => _currentValue,
          x =>
          {
              _currentValue = (float)Math.Round(x, unit);
              goldCnt.text = _currentValue.ToString();
          },
          targetGold, // 目标值
          1f // 时长
        ).SetTarget(goldTrans)
        .OnComplete(() =>
        {
            goldCnt.text = GameManager.Instance.playerInfo.Gold.ToString();
        });
    }

    //-----------------------------------------------------

    public void DiamondFlyAnim(Vector3 start)
    {
        GoldCollectEffect.Instance.StartEffect(ItemType.Diamond, start, diamondIcon.transform.position);
        DOTween.Sequence().AppendInterval(0.8f).AppendCallback(() =>
        {
            StartDiamondAnim();
        });
    }
    public void DiamondCanvasTop()
    {
        diamondCanvas.sortingOrder = 510;
    }
    public void DiamondCanvasRecover()
    {
        diamondCanvas.sortingOrder = 410;
    }

    public void StartDiamondAnim()
    {
        diamondTrans.DOKill();
        float _currentValue = float.Parse(diamondCnt.text);
        float targetDiamond = GameManager.Instance.playerInfo.Diamond;
        bool hasDecimal1 = targetDiamond != Mathf.RoundToInt(targetDiamond); // true（有小数）
        int unit = hasDecimal1 ? 2 : 0;

        DOTween.To(
          () => _currentValue,
          x =>
          {
              _currentValue = (float)Math.Round(x, unit);
              diamondCnt.text = _currentValue.ToString();
          },
          targetDiamond, // 目标值
          1f // 时长
        ).SetTarget(diamondTrans)
        .OnComplete(() =>
        {
            diamondCnt.text = GameManager.Instance.playerInfo.Diamond.ToString();
        });
    }
}
