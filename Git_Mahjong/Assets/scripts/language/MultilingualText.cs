using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultilingualText : MonoBehaviour
{
    public string key;
    private Text text;
    // Start is called before the first frame update
    void Start()
    {
        text = transform.GetComponent<Text>();
        UpdateText();
    }

    public void UpdateText()
    {
        if (text == null)
        {
            return;
        }
        text.text = LanguageManager.Instance.GetText(key);
    }
}
