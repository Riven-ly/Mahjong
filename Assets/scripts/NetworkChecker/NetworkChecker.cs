using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ����״̬����������ʾ
/// </summary>
public class NetworkChecker : MonoBehaviour
{
    public static NetworkChecker Instance;

    //��������
    private float checkInterval = 2f;
    [HideInInspector] public bool isNetworkAvailable = true;
    private bool isStart = false;
    [Header("��������")]
    public bool DebugNetwork = true;
    private void Awake()
    {
        Instance = this;
    }
   
    public void StartCheckNetworkStatus()
    {
        isStart = true;
        CheckNetworkStatus();
        StartCoroutine(CheckNetworkCoroutine());
    }

    IEnumerator CheckNetworkCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            CheckNetworkStatus();
        }
    }

    /// <summary>
    /// �������״̬
    /// </summary>
    void CheckNetworkStatus()
    {
        // ����״̬���
        bool currentNetworkState = Application.internetReachability != NetworkReachability.NotReachable;
        //currentNetworkState = DebugNetwork; //todo
        checkInterval = currentNetworkState ? 10f : 2f;
        // ״̬�仯ʱ
        if (currentNetworkState != isNetworkAvailable)
        {
            isNetworkAvailable = currentNetworkState;

            if (!isNetworkAvailable)
            {
                ShowNetworkTip();
            }
        }

        //if(currentNetworkState)
        //{
        //    NetworkCheckerPanel networkCheckerPanel = UIManager.Instance.GetUI<NetworkCheckerPanel>();
        //    if (networkCheckerPanel != null && networkCheckerPanel.gameObject.activeSelf)
        //    {
        //        networkCheckerPanel.Hide();
        //        //0.2�����vpn
        //        DOTween.Sequence().AppendInterval(0.2f).AppendCallback(() =>
        //        {
        //            //Debug.Log("���vpn");
        //        });
        //    }
        //    else
        //    {
        //        //Debug.Log("���vpn");
        //    }
        //}
    }

    /// <summary>
    /// ��ʾ������ʾ
    /// </summary>
    void ShowNetworkTip()
    {
        UIManager.Instance.OpenUI<NetworkCheckerPanel>();
    }

    public bool CheckNetworkManually()
    {
        CheckNetworkStatus();
        return isNetworkAvailable;
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if(!isStart)
        {
            return;
        }
        if (!pauseStatus)
        {
            CheckNetworkStatus();
        }
    }
}