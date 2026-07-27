using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwitchAorBSprite : MonoBehaviour
{
    public Sprite aSp;
    public Sprite bSp;
    public Image img;
    private void OnEnable()
    {
        img.sprite = GameManager.appATTtype == 1 ? bSp : aSp;
        img.SetNativeSize();
    }
}
