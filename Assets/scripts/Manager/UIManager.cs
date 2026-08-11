using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Transform UIPanelLayer1;
    public Transform UIPanelLayer2;
    public Transform PlayerInfoUI;
    public Transform LobbyScene;
    public Transform GameScene;

    public Transform uiMask;

    private Dictionary<string, UIBase> uiDict = new Dictionary<string, UIBase>();
    private void Awake()
    {
        Instance = this;
    }

    public void OpenUIMask()
    {
        uiMask.gameObject.SetActive(true);
    }

    public void HideUIMask()
    {
        uiMask.gameObject.SetActive(false);
    }

    public void OpenUI<T>(object data = null, Action _callback = null) where T : UIBase
    {
        string className = typeof(T).Name;
        // 已加载，直接显示
        if (uiDict.ContainsKey(className))
        {
            uiDict[className].Open(data, _callback);
            return;
        }

        // 加载预制体并实例化
        string loadPath = $"UI/Panel/{className}";
        GameObject uiPrefab = Resources.Load<GameObject>(loadPath);
        if (uiPrefab == null)
        {
            Debug.LogError($"UI加载失败 → 路径:{loadPath}，请检查预制体/类名是否一致");
            return;
        }

        GameObject uiObj = Instantiate(uiPrefab, UIPanelLayer1);
        uiObj.name = className;
        T uiBase = uiObj.GetComponent<T>();
        if (uiBase != null)
        {
            uiDict.Add(className, uiBase);
            uiBase.SetUIPanelLayer();
            uiBase.Open(data, _callback);
        }
        else
        {
            Debug.LogError($"{className}预制体未挂载对应脚本！");
            Destroy(uiObj);
        }
    }

    public void AddSpecialUI(GameObject uiPrefab)
    {
        if(uiPrefab == null)
        {
            Debug.LogError($"UI添加失败 ，请检查预制体是否为空");
            return;
        }

        GameObject uiObj = Instantiate(uiPrefab, UIPanelLayer1);
        UIBase uiBase = uiObj.GetComponent<UIBase>();
        if (uiBase != null)
        {
            string className = uiBase.GetType().Name;
            uiObj.name = className;
            uiDict.Add(className, uiBase);
            uiBase.SetUIPanelLayer();
            uiBase.RightAwayHide();
        }
        else
        {
            Debug.LogError($"{uiBase.name}预制体未挂载UIBase脚本！");
            Destroy(uiObj);
        }
    }

    public T GetUI<T>() where T : UIBase
    {
        string className = typeof(T).Name;
        if (uiDict.ContainsKey(className))
        {
            return uiDict[className] as T;
        }
        Debug.LogWarning($"获取UI失败 → {className}尚未加载/打开");
        return null;
    }

    public void HideUI<T>() where T : UIBase
    {
        string className = typeof(T).Name;
        if (uiDict.ContainsKey(className))
        {
            uiDict[className].Hide();
        }
    }

    public bool CheckIstheUIopen()
    {
        bool isOpen = false;
        foreach (var ui in uiDict)
        {
            if(ui.Value.isOpen)
            {
                Debug.Log(ui.Key);
                isOpen = true;
                break;
            }
        }
        return isOpen;
    }
}
