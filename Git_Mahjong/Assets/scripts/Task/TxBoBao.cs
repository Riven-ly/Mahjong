using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TxBoBao : MonoBehaviour
{
    public Transform bobaoTrans;
    public Text bobaoText;

    private string bobaoStr = "";
    private string wh;
    private string unit;
    private string ppStr;
    // Start is called before the first frame update
    void Start()
    {
        if (string.IsNullOrEmpty(bobaoStr))
        {
            ppStr = LanguageManager.Instance.GetText_Encrypt("pp");
            bobaoStr = LanguageManager.Instance.GetText("TxPanel_BoBao");
            wh = LanguageManager.Instance.GetText_Encrypt("wh");
            unit = LanguageManager.Instance.GetText_Encrypt("Special_Diamond__unit");
        }
        PlayBoBao();
    }
    private void PlayBoBao()
    {
        bobaoTrans.transform.DOKill();
        string curname = GenerateText();
        int ranV = Random.Range(1000, 10000);
        float targetF = ranV / 100f;

        bobaoText.text = string.Format(bobaoStr, curname, unit + targetF, wh);
        Vector3 curPos = bobaoTrans.transform.localPosition;
        curPos.x = 475f;
        bobaoTrans.transform.localPosition = curPos;
        DOTween.Sequence()
               //.Append(bobaoTrans.transform.DOLocalMoveX(0f, 3f).SetEase(Ease.Linear))
               //.AppendInterval(5f)
               .Append(bobaoTrans.transform.DOLocalMoveX(-2000f, 10f).SetEase(Ease.Linear))
               .AppendInterval(3f)
               .AppendCallback(() =>
               {
                   PlayBoBao();
               })
               .SetTarget(bobaoTrans.transform)
               ;
    }

    public string GenerateText()
    {
        // 1. 生成前缀（2个字母，首字母大写，第二个小写）
        string prefix = GetCapitalizedLetters(2);

        // 2. 固定5个星号
        string stars = "*****";

        // 3. 生成后缀（2个字母，全部小写）
        string suffix = GetLowerLetters(2);

        // 4. 拼接
        return prefix + stars + suffix + $"@{ppStr}.com";
    }
    private string GetCapitalizedLetters(int count)
    {
        char[] letters = new char[count];
        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                // 首字母：大写 A-Z
                letters[i] = (char)Random.Range(65, 91);
            }
            else
            {
                // 其余字母：小写 a-z
                letters[i] = (char)Random.Range(97, 123);
            }
        }
        return new string(letters);
    }
    private string GetLowerLetters(int count)
    {
        char[] letters = new char[count];
        for (int i = 0; i < count; i++)
        {
            letters[i] = (char)Random.Range(97, 123);
        }
        return new string(letters);
    }
}
