using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLoadingPanel : UIBase
{
    public Slider slider;
    public Text loadingText;
    public Text progressText;
    [SerializeField] private Text quoteText;
    [SerializeField] private Text quoteAuthorText;

    private const string LastQuoteIndexKey = "GameLoadingPanel.LastQuoteIndex";

    private const int QuoteCount = 20;

    private bool hasStarted;

    public static bool isCheckRegister = true;//��¼��⣺��8��/�й�
    public static bool isOpenStatic = false;

    // Start is called before the first frame update
    private void OnEnable()
    {
        isOpen = true;
        isOpenStatic = true;
        if (hasStarted)
            ShowRandomQuote();
    }

    private void ShowRandomQuote()
    {
        if (quoteText == null || quoteAuthorText == null)
            return;

        int lastIndex = PlayerPrefs.GetInt(LastQuoteIndexKey, -1);
        int index = lastIndex >= 0 && lastIndex < QuoteCount
            ? (lastIndex + Random.Range(1, QuoteCount)) % QuoteCount
            : Random.Range(0, QuoteCount);

        string key = $"LoadingQuote{index + 1}";
        quoteText.text = LanguageManager.Instance.GetText(key);
        quoteAuthorText.text = LanguageManager.Instance.GetText(key + "Author");
        PlayerPrefs.SetInt(LastQuoteIndexKey, index);
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        isOpen = false;
        isOpenStatic = false;
        this.DOKill();
    }

    // Start is called before the first frame update
    void Start()
    {
        hasStarted = true;
        ShowRandomQuote();

        slider.value = 0f;
        loadingText.text = $"{LanguageManager.Instance.GetText("Loading")}...";
        progressText.text = "0%";

        string str = PlayerPrefs.GetString("LoginAgreementPanel");
        if(string.IsNullOrEmpty(str))
        {
            //UIManager.Instance.OpenUI<LoginAgreementPanel>(null, () =>
            //{
            //    StartCoroutine(NetworkIE());
            //    LoadingUI();
            //});

            StartCoroutine(CheckNetworkIE());
            LoadingUI();
        }
        else
        {
            StartCoroutine(CheckNetworkIE());
            LoadingUI();
        }
    }

    private void LoadingUI()
    {
        string str = LanguageManager.Instance.GetText("Loading");
        DOTween.Sequence()
             .AppendCallback(() =>
             {
                 loadingText.text = $"{str}.";
             })
            .AppendInterval(0.3f)
            .AppendCallback(() =>
            {
                loadingText.text = $"{str}..";
            })
            .AppendInterval(0.3f)
            .AppendCallback(() =>
            {
                loadingText.text = $"{str}...";
            })
            .AppendInterval(0.3f)
            .SetLoops(-1, LoopType.Restart)
            .SetTarget(this)
            ;

        slider.value = 0f;
    }
    void DoTextNumberAnim(int from, int to, float duration)
    {
        // Kill ��ֹ��β��ŵ���
        DOTween.Kill(progressText);

        DOTween.To(() => from, x =>
        {
            // x �Ƕ�����ֵ���м���ֵ��ת���ַ�����ֵ��text
            progressText.text = x.ToString() + "%";
        }, to, duration)
            .SetEase(Ease.Linear);
    }

    IEnumerator CheckNetworkIE()
    {
        //1.������
        NetworkChecker.Instance.StartCheckNetworkStatus();
        while (!NetworkChecker.Instance.isNetworkAvailable)
        {
            yield return null;
        }

        //2.����SDK��ʼ��
        GameManager.appATTtype = 1;
        GameManager.Instance.UpdateAppATT();

        //3.loading������������
        slider.DOValue(0.9f, 1.8f).SetEase(Ease.Linear);
        DoTextNumberAnim(0, 90, 1.8f);

        yield return new WaitForSeconds(1.8f);

        OtherSdkManager.Instance.CustomEvent("load_page_open", "load_page_open", "");

        //4.��Ϸ��Դ׼��
        while (!GameManager.LoadABAsyncOK)
        {
            //Debug.Log("�ȴ��첽����");
            yield return null;
        }

        if (isCheckRegister)
        {
            slider.DOValue(1f, 0.2f).SetEase(Ease.Linear);
            DoTextNumberAnim(90, 100, 0.2f);
            yield return new WaitForSeconds(0.3f);
            AddCallback(() =>
            {
                if (ABResManager.Instance != null)
                {
                    ABResManager.Instance.Init();
                }
            });
            GameManager.Instance.Init(Hide);
        }
    }

    public override void Refresh(object data = null)
    {
        base.Refresh(data);

    }
    public override void Hide()
    {
        OtherSdkManager.Instance.CustomEvent("load_page_close", "load_page_open", "");
        base.Hide();
    }
}
