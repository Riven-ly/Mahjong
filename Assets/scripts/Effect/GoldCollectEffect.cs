using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldCollectEffect : MonoBehaviour
{
    public static GoldCollectEffect Instance;
    public GameObject glodPrefab;
    public GameObject diamondPrefab;
    public int num;

    private List<GoldFlyControl> golds = new List<GoldFlyControl>();
    private List<GoldFlyControl> diamonds = new List<GoldFlyControl>();

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        for (int i = 0; i < num * 3; i++)
        {
            GameObject go = Instantiate(glodPrefab, transform);
            go.transform.position = gameObject.transform.position;
            GoldFlyControl cc = go.GetComponent<GoldFlyControl>();
            if (cc != null)
            {
                cc.gameObject.SetActive(false);
                golds.Add(cc);
            }
        }

        for (int i = 0; i < num; i++)
        {
            GameObject go = Instantiate(diamondPrefab, transform);
            go.transform.position = gameObject.transform.position;
            GoldFlyControl cc = go.GetComponent<GoldFlyControl>();
            if (cc != null)
            {
                cc.gameObject.SetActive(false);
                diamonds.Add(cc);
            }
        }
    }

    public void StartEffect(ItemType itemType,Vector3 start, Vector3 target)
    {
        List<GoldFlyControl> temList = golds;
        switch (itemType)
        {
            case ItemType.Gold:
                temList = golds;
                break;
            case ItemType.Diamond:
                temList = diamonds;
                break;

        }

        int cntIndex = 0;
        for (int i = 0; i < temList.Count; i++)
        {
            if(!temList[i].gameObject.activeSelf)
            {
                temList[i].FlyGold(start, target, (cntIndex + 1) * 0.1f);
                cntIndex++;
                if (cntIndex == 10)
                {
                    break;
                }
            }
        }

        //ÉùÒôµþ¼ÓÆÆÒô
        int index = 0;
        foreach (var item in golds)
        {
            if (item.gameObject.activeSelf)
            {
                index++;
            }
        }
        foreach (var item in diamonds)
        {
            if (item.gameObject.activeSelf)
            {
                index++;
            }
        }

        float vS = index <= 10 ? 1f : 0.5f;
        foreach (var item in golds)
        {
            item.source.volume = vS;
        }
        foreach (var item in diamonds)
        {
            item.source.volume = vS;
        }
    }

}
